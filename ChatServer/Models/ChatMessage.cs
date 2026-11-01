using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatServer.Models;

public class ChatMessage
{
    [BsonId]
    public ObjectId Id { get; set; }

    public ObjectId SenderId { get; set; }

    public ObjectId ReceiverId { get; set; }

    public string Content { get; set; } = "";

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "SENT";
}