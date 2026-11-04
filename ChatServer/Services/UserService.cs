using ChatServer.Data;
using ChatServer.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatServer.Services;

//Responsible for maintaining users and updating their connection status in MongoDB.
public class UserService
{
    private readonly MongoDbContext _mongoDb;

    public UserService(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    // Creating a unique username index and resetting statuses upon server startup
    public async Task InitializeAsync()
    {
        var index = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true });

        await _mongoDb.Users.Indexes.CreateOneAsync(index);

        var update = Builders<User>.Update.Set(u => u.IsOnline, false);

        await _mongoDb.Users.UpdateManyAsync(Builders<User>.Filter.Empty,update);
    }

    // Searching for or creating the user, and marking them as logged in
    public async Task<User> MarkOnlineAsync(string username)
    {
        //Searching for a user by name
        var filter = Builders<User>.Filter
            .Eq(u => u.Username, username);

        //Creating a user if they do not yet exist in the database
        var update = Builders<User>.Update
            .Set(u => u.IsOnline, true)
            .SetOnInsert(u => u.Id, ObjectId.GenerateNewId())
            .SetOnInsert(u => u.Username, username)
            .SetOnInsert(u => u.CreatedAtUtc, DateTime.UtcNow);

        // Insert if missing, and return the user after the update
        var options = new FindOneAndUpdateOptions<User>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        return await _mongoDb.Users.FindOneAndUpdateAsync(filter,update,options);
    }

    // Marking the user as disconnected and saving the disconnection time
    public async Task MarkOfflineAsync(ObjectId userId)
    {
        var filter = Builders<User>.Filter
            .Eq(u => u.Id, userId);

        var update = Builders<User>.Update
            .Set(u => u.IsOnline, false)
            .Set(u => u.LastSeenAtUtc, (DateTime?)DateTime.UtcNow);

        await _mongoDb.Users.UpdateOneAsync(filter, update);
    }
}