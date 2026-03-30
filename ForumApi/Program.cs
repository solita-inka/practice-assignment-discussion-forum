using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

builder.Services.AddDbContext<ForumContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


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
    if (app.Environment.IsDevelopment())
    {
        db.Database.EnsureDeleted();
    }
    db.Database.EnsureCreated();
}


// Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication(); // must come before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
