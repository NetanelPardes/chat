using ChatServer.Models;
using MongoDB.Driver;

namespace ChatServer.Data;

public class MongoDbContext
{
    public IMongoCollection<User> Users { get; }

    public IMongoCollection<ChatMessage> Messages { get; }

    // Provides access to the database and its collections.
    public MongoDbContext(string connectionString, string databaseName)
    {
        MongoClient client = new MongoClient(connectionString);

        IMongoDatabase database = client.GetDatabase(databaseName);

        Users = database.GetCollection<User>("users");
        Messages = database.GetCollection<ChatMessage>("messages");
    }
}