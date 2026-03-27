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
    // 🔄 Later: replace with Entra ID (JWT)
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0";
            options.Audience = "YOUR_API_CLIENT_ID";
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
