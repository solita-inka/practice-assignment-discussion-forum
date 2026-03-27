using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ForumApi.DTOs.Topics;
using ForumApi.DTOs.Messages;

namespace ForumApi.Tests;

public class MessagesControllerTests : IClassFixture<ForumApiFactory>
{
    private readonly ForumApiFactory _factory;

    public MessagesControllerTests(ForumApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient() => _factory.CreateAuthenticatedClient("1", "alice", "Admin");
    private HttpClient CreateUserClient() => _factory.CreateAuthenticatedClient("2", "bob", "User");

    private async Task<TopicSummaryDto> CreateTopicAsync(HttpClient client, string title = "Test Topic")
    {
        var topicRequest = new TopicRequest(title);
        var topicResponse = await client.PostAsJsonAsync("/api/topics", topicRequest);
        topicResponse.EnsureSuccessStatusCode();
        var createdTopic = await topicResponse.Content.ReadFromJsonAsync<TopicSummaryDto>();
        return createdTopic!;
    }

    [Fact]
    public async Task CreateMessage_ReturnsCreated()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Message Test");

        var client = CreateUserClient();
        var messageRequest = new { Content = "Hello from the test!" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        messageResponse.EnsureSuccessStatusCode();
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.NotNull(createdMessage);
        Assert.Equal("Hello from the test!", createdMessage.Content);
    }

    [Fact]
    public async Task CreateMessage_ActuallyPersistsInDb()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for DB Message Test");

        var client = CreateUserClient();
        var messageRequest = new { Content = "Hello from the DB test!" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        messageResponse.EnsureSuccessStatusCode();

        var db = _factory.GetDbContext();
        var messageInDb = await db.Messages.FirstOrDefaultAsync(m => m.Content == "Hello from the DB test!");

        Assert.NotNull(messageInDb);
        Assert.Equal("Hello from the DB test!", messageInDb.Content);
        Assert.Equal(topic.Id, messageInDb.TopicId);
    }

    [Fact]
    public async Task ModifyMessage_ReturnsNoContent()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Modify Test");

        var client = CreateUserClient();
        var messageRequest = new { Content = "Original content" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();

        var modifyRequest = new { Content = "Modified content" };
        var modifyResponse = await client.PutAsJsonAsync($"/api/topics/{topic.Id}/messages/{createdMessage!.Id}", modifyRequest);

        Assert.Equal(HttpStatusCode.NoContent, modifyResponse.StatusCode);

        var db = _factory.GetDbContext();
        var modifiedMessageInDb = await db.Messages.FindAsync(createdMessage.Id);

        Assert.NotNull(modifiedMessageInDb);
        Assert.Equal("Modified content", modifiedMessageInDb.Content);
    }

    [Fact]
    public async Task ModifyMessage_ReturnsForbidden_WhenDifferentUser()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Forbidden Modify Test");

        // Bob creates a message
        var bob = CreateUserClient();
        var messageRequest = new { Content = "Bob's message" };
        var messageResponse = await bob.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();

        // Alice (admin) tries to modify Bob's message
        var modifyRequest = new { Content = "Alice modified this" };
        var modifyResponse = await admin.PutAsJsonAsync($"/api/topics/{topic.Id}/messages/{createdMessage!.Id}", modifyRequest);

        Assert.Equal(HttpStatusCode.Forbidden, modifyResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_ReturnsForbidden_WhenDifferentUser()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Forbidden Delete Test");

        // Bob creates a message
        var bob = CreateUserClient();
        var messageRequest = new { Content = "Bob's message to not delete" };
        var messageResponse = await bob.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();

        // Alice (admin) tries to delete Bob's message
        var deleteResponse = await admin.DeleteAsync($"/api/topics/{topic.Id}/messages/{createdMessage!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact] async Task ModifyMessage_actuallyUpdatesDb()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Modify DB Test");

        var client = CreateUserClient();
        var messageRequest = new { Content = "Before update" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();

        var modifyRequest = new { Content = "After update" };
        await client.PutAsJsonAsync($"/api/topics/{topic.Id}/messages/{createdMessage!.Id}", modifyRequest);

        var db = _factory.GetDbContext();
        var messageInDb = await db.Messages.FindAsync(createdMessage.Id);

        Assert.NotNull(messageInDb);
        Assert.Equal("After update", messageInDb.Content);
    }

    [Fact]
    public async Task DeleteMessage_ReturnsNoContent()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Delete Message Test");

        var client = CreateUserClient();
        var messageRequest = new { Content = "Message to delete" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/topics/{topic.Id}/messages/{createdMessage!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_ReturnsNotFound_WhenMessageDoesNotExist()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for NotFound Delete Test");

        var client = CreateUserClient();
        var deleteResponse = await client.DeleteAsync($"/api/topics/{topic.Id}/messages/9999");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_ActuallyRemovesFromDb()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Delete DB Test");

        var client = CreateUserClient();
        var messageRequest = new { Content = "Message to delete from DB" };
        var messageResponse = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);
        var createdMessage = await messageResponse.Content.ReadFromJsonAsync<MessageResponse>();

        await client.DeleteAsync($"/api/topics/{topic.Id}/messages/{createdMessage!.Id}");

        var db = _factory.GetDbContext();
        var messageInDb = await db.Messages.FindAsync(createdMessage.Id);

        Assert.Null(messageInDb);
    }

    [Fact]
    public async Task CreateMessage_ReturnsUnauthorized_ForUnauthenticatedUser()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for Unauth Message Test");

        var client = _factory.CreateClient();
        var messageRequest = new { Content = "Should not work" };
        var response = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/messages", messageRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ModifyMessage_ReturnsNotFound_WhenMessageDoesNotExist()
    {
        var admin = CreateAdminClient();
        var topic = await CreateTopicAsync(admin, "Topic for NotFound Modify Test");

        var client = CreateUserClient();
        var modifyRequest = new { Content = "Does not matter" };
        var response = await client.PutAsJsonAsync($"/api/topics/{topic.Id}/messages/9999", modifyRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMessage_ReturnsBadRequest_WhenTopicDoesNotExist()
    {
        var client = CreateUserClient();
        var messageRequest = new { Content = "Message for non-existent topic" };
        var response = await client.PostAsJsonAsync("/api/topics/9999/messages", messageRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
