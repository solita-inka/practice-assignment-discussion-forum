using ForumApi.Models;
using Microsoft.EntityFrameworkCore;

public interface IUserRepository
{
    Task UpsertAsync(string userId, string username);
}

public class UserRepository : IUserRepository
{
    private readonly ForumContext _context;

    public UserRepository(ForumContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(string userId, string username)
    {
        var existingUser = await _context.Users.FindAsync(userId);
        if (existingUser == null)
        {
            // First time this user hits the API → insert
            _context.Users.Add(new User { Id = userId, Username = username });
        }
        else if (existingUser.Username != username)
        {
            // Username changed in Entra ID → update
            existingUser.Username = username;
        }
        await _context.SaveChangesAsync();
    }
}
