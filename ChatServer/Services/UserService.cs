using ChatServer.Data;
using ChatServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatServer.Services;

//Responsible for maintaining users and updating their connection status in MySQL.
public class UserService
{
    private readonly DbContextOptions<MySqlDbContext> _options;

    public UserService(DbContextOptions<MySqlDbContext> options)
    {
        _options = options;
    }

    // Creating tables and resetting statuses upon server startup
    public async Task InitializeAsync()
    {
        await using var db = new MySqlDbContext(_options);

        await db.Database.EnsureCreatedAsync();

        await db.Users.ExecuteUpdateAsync(update =>
            update.SetProperty(u => u.IsOnline, false));
    }

    // Searching for or creating the user, and marking them as logged in
    public async Task<User> MarkOnlineAsync(string username)
    {
        await using var db = new MySqlDbContext(_options);

        //Searching for a user by name
        User? user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            //Creating a user if they do not yet exist in the database
            user = new User
            {
                Username = username,
                IsOnline = true
            };
            // Marking the record for addition to the database
            db.Users.Add(user);
        }
        else
        {
            user.IsOnline = true;
        }

        await db.SaveChangesAsync();

        return user;
    }

   // Marking the user as disconnected and saving the disconnection time
    public async Task MarkOfflineAsync(int userId)
    {
        await using var db = new MySqlDbContext(_options);

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(u => u.IsOnline, false)
                .SetProperty(
                    u => u.LastSeenAtUtc,
                    (DateTime?)DateTime.UtcNow));
    }
}