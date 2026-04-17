using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ForumApi.Data;

namespace ForumApi.Tests;

public class ForumApiFactory : WebApplicationFactory<Program>
{
    private readonly List<IServiceScope> _scopes = new();

    public ForumContext GetDbContext()
    {
        var scope = Services.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<ForumContext>();
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        base.Dispose(disposing);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure "Development" environment is used so MockAuth is active
        builder.UseEnvironment("Development");

        // Load test project's own config so tests don't depend on the main app's dev config
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testProjectDir = Path.GetDirectoryName(typeof(ForumApiFactory).Assembly.Location)!;
            config.AddJsonFile(Path.Combine(testProjectDir, "appsettings.Development.json"), optional: false);
        });

        builder.ConfigureServices((context, services) =>
        {
            // Reset database for clean state after all services are registered
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ForumContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Creates a client preconfigured with mock authentication headers
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId, string username, string role)
    {
        var client = this.CreateClient();

        client.DefaultRequestHeaders.Add("X-Mock-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Mock-Username", username);
        client.DefaultRequestHeaders.Add("X-Mock-Role", role);

        return client;
    }
}