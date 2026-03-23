namespace ForumApi.Models;

public enum UserRole
{
    User,
    Admin
}

public class User
{
    public required string Id { get; set; }
    public required string Username { get; set; }    
    public UserRole Role { get; set; } = UserRole.User;
}
