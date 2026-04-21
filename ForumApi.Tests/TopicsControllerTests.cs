using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ForumApi.DTOs.Pagination;
using ForumApi.DTOs.Topics;

[assembly: CollectionBehavior(DisableTestParallelization = true)]


namespace ForumApi.Tests;

public class TopicsControllerTests : IClassFixture<ForumApiFactory>
{
    private readonly ForumApiFactory _factory;

    public TopicsControllerTests(ForumApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient() => _factory.CreateAuthenticatedClient("1", "alice", "Admin");
    private HttpClient CreateUserClient() => _factory.CreateAuthenticatedClient("2", "bob", "User");

    [Fact]
    public async Task GetTopics_ReturnsOk()
    {
        var client = CreateUserClient();

        var response = await client.GetAsync("/api/topics?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task CreateTopic_ReturnsCreated()
    {
        var client = CreateAdminClient();
        var request = new TopicRequest("New Test Topic");

        var response = await client.PostAsJsonAsync("/api/topics", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdTopic = await response.Content.ReadFromJsonAsync<TopicSummaryDto>();
        Assert.NotNull(createdTopic);
        Assert.Equal(request.Title, createdTopic.Title);
    }

    [Fact]
    public async Task CreateTopic_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        var request = new TopicRequest("");  // Invalid title

        var response = await client.PostAsJsonAsync("/api/topics", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_ReturnsNoContent()
    {
        var client = CreateAdminClient();
        var createRequest = new TopicRequest("Topic to Delete");
        var createResponse = await client.PostAsJsonAsync("/api/topics", createRequest);
        var createdTopic = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var deleteResponse = await client.DeleteAsync($"/api/topics/{createdTopic!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_ReturnsNotFound()
    {
        var client = CreateAdminClient();

        var response = await client.DeleteAsync("/api/topics/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    async Task ModifyTopic_ReturnsNoContent()
    {
        var client = CreateAdminClient();
        var createRequest = new TopicRequest("Topic to Modify");
        var createResponse = await client.PostAsJsonAsync("/api/topics", createRequest);
        var createdTopic = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var modifyRequest = new TopicRequest("Modified Title");
        var modifyResponse = await client.PutAsJsonAsync($"/api/topics/{createdTopic!.Id}", modifyRequest);

        Assert.Equal(HttpStatusCode.NoContent, modifyResponse.StatusCode);
    }

    [Fact]
    async Task ModifyTopic_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        var modifyRequest = new TopicRequest("Modified Title");
        var modifyResponse = await client.PutAsJsonAsync("/api/topics/9999", modifyRequest);

        Assert.Equal(HttpStatusCode.NotFound, modifyResponse.StatusCode);
    }

    [Fact]
    async Task GetTopics_ReturnsOk_ForUnauthenticatedUser()
    {
        var client = _factory.CreateClient(); // No authentication

        var response = await client.GetAsync("/api/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    async Task CreateTopic_ReturnsCreated_ForNonAdminUser()
    {
        var client = CreateUserClient();
        var request = new TopicRequest("User Created Topic");

        var response = await client.PostAsJsonAsync("/api/topics", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    async Task DeleteTopic_ReturnsUnauthorized_ForNonAdminUser()
    {
        var client = CreateUserClient();

        var response = await client.DeleteAsync("/api/topics/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    async Task ModifyTopic_ReturnsUnauthorized_ForNonAdminUser()
    {
        var client = CreateUserClient();
        var modifyRequest = new TopicRequest("Modified Title");

        var response = await client.PutAsJsonAsync("/api/topics/1", modifyRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_ActuallyPersistsInDb()
    {
        var client = CreateAdminClient();
        var request = new TopicRequest("New DB Topic");

        var response = await client.PostAsJsonAsync("/api/topics", request);
        response.EnsureSuccessStatusCode();

        var db = _factory.GetDbContext();
        var topicInDb = await db.Topics.FirstOrDefaultAsync(t => t.Title == "New DB Topic");

        Assert.NotNull(topicInDb);
        Assert.Equal(request.Title, topicInDb.Title);
    }

    [Fact] async Task ModifyTopic_ActuallyUpdatesDb()
    {
        var client = CreateAdminClient();
        var createRequest = new TopicRequest("Topic to Modify in DB");
        var createResponse = await client.PostAsJsonAsync("/api/topics", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdTopic = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var modifyRequest = new TopicRequest("Modified Title in DB");
        var modifyResponse = await client.PutAsJsonAsync($"/api/topics/{createdTopic!.Id}", modifyRequest);
        modifyResponse.EnsureSuccessStatusCode();

        var db = _factory.GetDbContext();
        var topicInDb = await db.Topics.FirstOrDefaultAsync(t => t.Id == createdTopic.Id);

        Assert.NotNull(topicInDb);
        Assert.Equal("Modified Title in DB", topicInDb.Title);
    }

    [Fact] async Task DeleteTopic_ActuallyRemovesFromDb()
    {
        var client = CreateAdminClient();
        var createRequest = new TopicRequest("Topic to Delete from DB");
        var createResponse = await client.PostAsJsonAsync("/api/topics", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdTopic = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var deleteResponse = await client.DeleteAsync($"/api/topics/{createdTopic!.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        var db = _factory.GetDbContext();
        var topicInDb = await db.Topics.FirstOrDefaultAsync(t => t.Id == createdTopic.Id);

        Assert.Null(topicInDb);
    }

    [Fact]
    async Task GetTopics_ReturnsPaginatedResponse()
    {
        var client = CreateAdminClient();
        // Create 3 topics
        for (int i = 1; i <= 3; i++)
        {
            await client.PostAsJsonAsync("/api/topics", new TopicRequest($"Pagination Topic {i}"));
        }

        var response = await client.GetAsync("/api/topics?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();
        Assert.NotNull(result);
        Assert.True(result!.Items.Count() <= 2);
        Assert.True(result.TotalCount >= 3);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    async Task GetTopics_ArchivedTopicsNotReturnedByDefault()
    {
        var client = CreateAdminClient();
        var createResponse = await client.PostAsJsonAsync("/api/topics", new TopicRequest("Archived Topic Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();
        await client.PatchAsJsonAsync($"/api/topics/{created!.Id}/archive", true);

        var response = await client.GetAsync("/api/topics?page=1&pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();

        Assert.DoesNotContain(result!.Items, t => t.Id == created.Id);
    }

    [Fact]
    async Task GetTopics_ArchivedTrue_ReturnsOnlyArchivedTopics()
    {
        var client = CreateAdminClient();
        // Create and archive a topic
        var createResponse = await client.PostAsJsonAsync("/api/topics", new TopicRequest("Archived Only Topic"));
        var created = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();
        await client.PatchAsJsonAsync($"/api/topics/{created!.Id}/archive", true);

        var response = await client.GetAsync("/api/topics?page=1&pageSize=100&archived=true");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();

        Assert.Contains(result!.Items, t => t.Id == created.Id);
        Assert.All(result.Items, t => Assert.True(t.IsArchived));
    }

    [Fact]
    async Task ArchiveTopic_SetsIsArchived()
    {
        var client = CreateAdminClient();
        var createResponse = await client.PostAsJsonAsync("/api/topics", new TopicRequest("Topic To Archive"));
        var created = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var archiveResponse = await client.PatchAsJsonAsync($"/api/topics/{created!.Id}/archive", true);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        var db = _factory.GetDbContext();
        var topicInDb = await db.Topics.FirstOrDefaultAsync(t => t.Id == created.Id);
        Assert.True(topicInDb!.IsArchived);
    }

    [Fact]
    async Task ArchiveTopic_NonAdmin_ReturnsForbidden()
    {
        var client = CreateUserClient();

        var response = await client.PatchAsJsonAsync("/api/topics/1/archive", true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    async Task GetTopics_ReturnsTopicsOrderedByNewestMessage()
    {
        var admin = CreateAdminClient();
        var user = CreateUserClient();

        // Create two topics
        var topicOld = await (await admin.PostAsJsonAsync("/api/topics", new TopicRequest("Topic with Older Messages"))).Content.ReadFromJsonAsync<TopicSummaryDto>();
        var topicNew = await (await admin.PostAsJsonAsync("/api/topics", new TopicRequest("Topic with Newer Messages"))).Content.ReadFromJsonAsync<TopicSummaryDto>();

        await user.PostAsJsonAsync($"/api/topics/{topicOld!.Id}/messages", new { Content = "Old message" });

        await user.PostAsJsonAsync($"/api/topics/{topicNew!.Id}/messages", new { Content = "New message" });

        var response = await admin.GetAsync("/api/topics?page=1&pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();

        var topics = result!.Items.ToList();
        var oldIndex = topics.FindIndex(t => t.Id == topicOld.Id);
        var newIndex = topics.FindIndex(t => t.Id == topicNew.Id);

        Assert.True(newIndex < oldIndex, "Topic with newest message should appear before topic with older message");
    }

    [Fact]
    async Task GetTopics_DefaultsPagination_WhenNoParamsProvided()
    {
        var client = CreateUserClient();

        var response = await client.GetAsync("/api/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    async Task GetTopics_ClampsNegativePage()
    {
        var client = CreateUserClient();

        var response = await client.GetAsync("/api/topics?page=-5&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
    }

    [Fact]
    async Task GetTopics_ClampsExcessivePageSize()
    {
        var client = CreateUserClient();

        var response = await client.GetAsync("/api/topics?page=1&pageSize=5000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();
        Assert.NotNull(result);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    async Task GetTopics_ClampsZeroPageSize()
    {
        var client = CreateUserClient();

        var response = await client.GetAsync("/api/topics?page=1&pageSize=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TopicSummaryDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageSize);
    }
}