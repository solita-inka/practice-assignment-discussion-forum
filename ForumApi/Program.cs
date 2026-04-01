using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ForumApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUpVoteService, UpVoteService>();
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUpVoteRepository, UpVoteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
}

builder.Services.AddDbContext<ForumContext>(options =>
    options.UseSqlServer(connectionString));


// ✅ AUTH CONFIG GOES HERE (before build)
if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = MockAuthHandler.SchemeName;
            options.DefaultChallengeScheme = MockAuthHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>(
            MockAuthHandler.SchemeName, null);
}
else
{
    // 🔄 Replace YOUR_TENANT_ID and YOUR_API_CLIENT_ID with real values from Entra ID
    var azureAdConfig = builder.Configuration.GetSection("AzureAd");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"{azureAdConfig["Instance"]}{azureAdConfig["TenantId"]}/v2.0";
            options.Audience = azureAdConfig["ClientId"];
        });
}

builder.Services.AddAuthorization();


// ✅ Build app AFTER configuring services
var app = builder.Build();


// ✅ DB init AFTER app is built
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForumContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            db.Database.EnsureDeleted();
        }
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize database");
    }
}


// Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/error");
}

// Health check endpoint - no auth required
app.MapGet("/", () => Results.Ok(new
{
    status = "running",
    environment = app.Environment.EnvironmentName,
    hasConnectionString = !string.IsNullOrEmpty(connectionString)
}));

// Temporary debug endpoint - REMOVE after debugging
app.MapGet("/debug", (ForumContext db) =>
{
    try
    {
        var canConnect = db.Database.CanConnect();
        return Results.Ok(new
        {
            connectionStringSource = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"))
                ? "appsettings" : "environment variable",
            connectionStringLength = connectionString?.Length ?? 0,
            canConnectToDb = canConnect
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            error = ex.Message,
            innerError = ex.InnerException?.Message,
            connectionStringLength = connectionString?.Length ?? 0
        });
    }
});

app.MapGet("/error", () => Results.Problem("An internal error occurred"));

app.UseAuthentication(); // must come before Authorization
app.UseMiddleware<UserUpsertMiddleware>(); // upsert user from JWT
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
