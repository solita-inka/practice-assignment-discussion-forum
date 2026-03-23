using ForumApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using   System.IdentityModel.Tokens.Jwt;

public class AuthService
{
    private readonly IConfiguration _configuration;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private readonly List<User> _users = new()
    {
        new User { Id="d4f5a9b1-2c3e-4f8a-b7d6-1a2b3c4d5e6f", Username = "user123", Role = UserRole.Admin },
        new User { Id="e8c7b6a5-3d2f-4e1a-9c8b-7f6e5d4c3b2a", Username = "sports_lover", Role = UserRole.User },
        new User { Id="a1b2c3d4-5e6f-7a8b-9c0d-e1f2a3b4c5d6", Username = "guest", Role = UserRole.User }
    };

    public async Task<AuthResponse?> LoginAsync(string username, string password)
    {
        var user = _users.FirstOrDefault(u => u.Username == username);
        if (user == null)
            return null;

        var token = GenerateToken(user);
        return new AuthResponse (user.Id, token, user.Username, user.Role);
    }

    public async Task<AuthResponse?> RegisterAsync(string username, string password)
    {
        if (_users.Any(u => u.Username == username))
            return null;

        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Role = UserRole.User
        };
        _users.Add(newUser);

        var token = GenerateToken(newUser);
        return new AuthResponse (newUser.Id, token, newUser.Username, newUser.Role);
    }

    public string? GetUsernameById(string userId)
    {
        return _users.FirstOrDefault(u => u.Id == userId)?.Username ?? "";
    }

    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}