using ForumApi.Models;

public record AuthResponse
(
    string UserId,
    string Token,
    string UserName,
    UserRole Role
);
