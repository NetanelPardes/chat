using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatServer.Models;

public class User
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Username { get; set; } = "";

    public bool IsOnline { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastSeenAtUtc { get; set; }
}