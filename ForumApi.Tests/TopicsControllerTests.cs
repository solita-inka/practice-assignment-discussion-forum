using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
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

        var response = await client.GetAsync("/api/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var topics = await response.Content.ReadFromJsonAsync<List<TopicSummaryDto>>();
        Assert.NotNull(topics);
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

        var deleteResponse = await client.DeleteAsync($"/api/topics/{createdTopic.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_ReturnsNotFound()
    {
        var client = CreateAdminClient();
    }

    [Fact]
    async Task ModifyTopic_ReturnsNoContent()
    {
        var client = CreateAdminClient();
        var createRequest = new TopicRequest("Topic to Modify");
        var createResponse = await client.PostAsJsonAsync("/api/topics", createRequest);
        var createdTopic = await createResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var modifyRequest = new TopicRequest("Modified Title");
        var modifyResponse = await client.PutAsJsonAsync($"/api/topics/{createdTopic.Id}", modifyRequest);

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
        var modifyResponse = await client.PutAsJsonAsync($"/api/topics/{createdTopic.Id}", modifyRequest);
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

        var deleteResponse = await client.DeleteAsync($"/api/topics/{createdTopic.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        var db = _factory.GetDbContext();
        var topicInDb = await db.Topics.FirstOrDefaultAsync(t => t.Id == createdTopic.Id);

        Assert.Null(topicInDb);
    }   
}