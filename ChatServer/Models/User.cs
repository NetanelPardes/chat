using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatServer.Models;

public class User
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Username { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}