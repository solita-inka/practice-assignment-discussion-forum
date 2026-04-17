using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ForumApi.DTOs.Topics;

namespace ForumApi.Tests;

public class UpVotesControllerTests : IClassFixture<ForumApiFactory>
{
    private readonly ForumApiFactory _factory;

    public UpVotesControllerTests(ForumApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int TopicId, int MessageId)> CreateTopicAndMessage(HttpClient client)
    {
        var topicRequest = new TopicRequest("Topic for Upvote Test");
        var topicResponse = await client.PostAsJsonAsync("/api/topics", topicRequest);
        var createdTopic = await topicResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();

        var messageRequest = new { Content = "Message for upvote test" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{createdTopic!.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<dynamic>();
        var messageId = (int)createdMessage!.GetProperty("id").GetInt32();

        return (createdTopic.Id, messageId);
    }

    [Fact]
    public async Task Upvote_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient("1", "alice", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client);

        var response = await client.PostAsync($"/api/messages/{messageId}/upvotes", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Upvote_ReturnsBadRequest_WhenAlreadyUpvoted()
    {
        var client = _factory.CreateAuthenticatedClient("1", "alice", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client);

        await client.PostAsync($"/api/messages/{messageId}/upvotes", null);
        var response = await client.PostAsync($"/api/messages/{messageId}/upvotes", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUpvote_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient("1", "alice", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client);

        await client.PostAsync($"/api/messages/{messageId}/upvotes", null);
        var response = await client.DeleteAsync($"/api/messages/{messageId}/upvotes");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUpvote_ReturnsNotFound_WhenNotUpvoted()
    {
        var client = _factory.CreateAuthenticatedClient("1", "alice", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client);

        var response = await client.DeleteAsync($"/api/messages/{messageId}/upvotes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upvote_ReturnsUnauthorized_ForUnauthenticatedUser()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/messages/1/upvotes", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upvote_ActuallyPersistsInDb()
    {
        var client = _factory.CreateAuthenticatedClient("10", "upvoteUser", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client);

        var response = await client.PostAsync($"/api/messages/{messageId}/upvotes", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var db = _factory.GetDbContext();
        var upvoteInDb = await db.MessageUpVotes.FirstOrDefaultAsync(u => u.MessageId == messageId && u.CreatedByUserId == "10");

        Assert.NotNull(upvoteInDb);
    }

    [Fact]
    public async Task DeleteUpvote_ActuallyRemovesFromDb()
    {
        var client = _factory.CreateAuthenticatedClient("11", "deleteUpvoteUser", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client);

        await client.PostAsync($"/api/messages/{messageId}/upvotes", null);
        var deleteResponse = await client.DeleteAsync($"/api/messages/{messageId}/upvotes");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var db = _factory.GetDbContext();
        var upvoteInDb = await db.MessageUpVotes.FirstOrDefaultAsync(u => u.MessageId == messageId && u.CreatedByUserId == "11");

        Assert.Null(upvoteInDb);
    }

    [Fact]
    public async Task DeleteUpvote_ReturnsNotFound_WhenDifferentUser()
    {
        var client1 = _factory.CreateAuthenticatedClient("20", "userA", "Admin");
        var (_, messageId) = await CreateTopicAndMessage(client1);

        // User A upvotes
        await client1.PostAsync($"/api/messages/{messageId}/upvotes", null);

        // User B tries to delete User A's upvote
        var client2 = _factory.CreateAuthenticatedClient("21", "userB", "Admin");
        var response = await client2.DeleteAsync($"/api/messages/{messageId}/upvotes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upvote_ReturnsBadRequest_WhenMessageDoesNotExist()
    {
        var client = _factory.CreateAuthenticatedClient("1", "alice", "Admin");

        var response = await client.PostAsync("/api/messages/9999/upvotes", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
