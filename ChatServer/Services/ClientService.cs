using System.Collections.Concurrent;
using System.Net.Sockets;
using ChatServer.Models;

namespace ChatServer.Services;

public class ClientService
{
    private readonly ConcurrentDictionary<string, ConnectedClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public ConnectedClient CreateClient(string username,TcpClient tcpClient,StreamReader reader,StreamWriter writer)
    {
        return new ConnectedClient
        {
            Username = username,
            TcpClient = tcpClient,
            Reader = reader,
            Writer = writer
        };
    }

    public bool AddClient(ConnectedClient client)
    {
        return _clients.TryAdd(client.Username, client);
    }

    public ConnectedClient? GetClient(string username)
    {
        _clients.TryGetValue(username, out var client);
        return client;
    }

    public bool RemoveClient(ConnectedClient client)
    {
        return ((ICollection<KeyValuePair<string, ConnectedClient>>)_clients)
            .Remove(new KeyValuePair<string, ConnectedClient>(
                client.Username, client));
    }

    public List<string> GetOnlineUsers()
    {
        return _clients.Keys.OrderBy(name => name).ToList();
    }

    public async Task SendAsync(ConnectedClient client, string message)
    {
        await client.SendLock.WaitAsync();

        try
        {
            await client.Writer.WriteLineAsync(message);
        }
        finally
        {
            client.SendLock.Release();
        }
    }

    public async Task<string?> ReceiveAsync(ConnectedClient client)
    {
        return await client.Reader.ReadLineAsync();
    }

    public void CloseClient(ConnectedClient client)
    {
        client.Reader.Dispose();
        client.Writer.Dispose();
        client.TcpClient.Dispose();
    }
}