using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ForumApi.Data;

namespace ForumApi.Tests;

public class UserUpsertMiddlewareTests : IClassFixture<ForumApiFactory>
{
    private readonly ForumApiFactory _factory;

    public UserUpsertMiddlewareTests(ForumApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticatedRequest_CreatesUserInDatabase()
    {
        var client = _factory.CreateAuthenticatedClient("100", "testmiddleware", "User");

        await client.GetAsync("/api/topics");

        var db = _factory.GetDbContext();
        var user = await db.Users.FindAsync("100");
        Assert.NotNull(user);
        Assert.Equal("testmiddleware", user.Username);
    }

    [Fact]
    public async Task AuthenticatedRequest_UpdatesUsernameInDatabase()
    {
        var client1 = _factory.CreateAuthenticatedClient("101", "original_name", "User");
        await client1.GetAsync("/api/topics");

        var db1 = _factory.GetDbContext();
        var userBefore = await db1.Users.FindAsync("101");
        Assert.NotNull(userBefore);
        Assert.Equal("original_name", userBefore.Username);

        var client2 = _factory.CreateAuthenticatedClient("101", "updated_name", "User");
        await client2.GetAsync("/api/topics");

        var db2 = _factory.GetDbContext();
        var userAfter = await db2.Users.FindAsync("101");
        Assert.NotNull(userAfter);
        Assert.Equal("updated_name", userAfter.Username);
    }

    [Fact]
    public async Task UnauthenticatedRequest_DoesNotCreateUser()
    {
        var db = _factory.GetDbContext();
        var userCountBefore = await db.Users.CountAsync();

        var client = _factory.CreateClient();
        await client.GetAsync("/api/topics");

        var userCountAfter = await db.Users.CountAsync();
        Assert.Equal(userCountBefore, userCountAfter);
    }
}
