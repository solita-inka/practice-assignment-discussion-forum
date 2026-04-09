using System.Security.Claims;

public class UserUpsertMiddleware
{
    private readonly RequestDelegate _next;

    public UserUpsertMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        // Only run if the user is authenticated (has a valid JWT)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");
            var username = context.User.FindFirstValue(ClaimTypes.Name)
                       ?? context.User.FindFirstValue("name")
                       ?? context.User.FindFirstValue("preferred_username");

            if (userId != null && username != null)
            {
                await userRepository.UpsertAsync(userId, username);
            }
        }

        // Pass the request to the next middleware / controller
        await _next(context);
    }
}
