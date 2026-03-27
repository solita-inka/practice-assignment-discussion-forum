using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
public class ForumApiFactory : WebApplicationFactory<Program>
{
    public ForumContext GetDbContext() => Services.CreateScope().ServiceProvider.GetRequiredService<ForumContext>();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure "Development" environment is used so MockAuth is active
        builder.UseEnvironment("Development");

        builder.ConfigureServices((context, services) =>
        {
            // Get connection string from config
            var configuration = context.Configuration;
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ForumContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext pointing to Docker SQL Server or SQLite
            services.AddDbContext<ForumContext>(options =>
                options.UseSqlServer(connectionString)
                // For SQLite in-memory (optional):
                // options.UseSqlite("DataSource=:memory:")
            );

            // Build provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ForumContext>();
                db.Database.EnsureDeleted();  // Reset DB for clean state
                db.Database.EnsureCreated();
            }
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