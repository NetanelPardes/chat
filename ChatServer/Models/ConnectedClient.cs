using System.Net.Sockets;
using MongoDB.Bson;

namespace ChatServer.Models;

public class ConnectedClient
{
    public ObjectId UserId { get; set; }
    public string Username { get; set; } = "";

    public TcpClient TcpClient { get; set; } = null!;

    public StreamReader Reader { get; set; } = null!;

    public StreamWriter Writer { get; set; } = null!;

    public SemaphoreSlim SendLock { get; set; } = new(1, 1);

}