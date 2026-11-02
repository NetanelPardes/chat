using System.Collections.Concurrent;
using System.Net.Sockets;
using ChatServer.Models;

namespace ChatServer.Services;

//Responsible for managing all users connected to our TCP server
public class ClientService
{
    //A list of connected users
    //this allows multiple users to use it simultaneously.
    private readonly ConcurrentDictionary<string, ConnectedClient> _clients = new();

    public ConnectedClient CreateClient(string username,TcpClient tcpClient,StreamReader reader,StreamWriter writer)
    {
        //We create a new object from the ConnectedClient class and insert the data we received into it.
        return new ConnectedClient
        {
            Username = username,
            TcpClient = tcpClient,
            Reader = reader,
            Writer = writer
        };
    }

    //Adding a user
    public bool AddClient(ConnectedClient client)
    {
        return _clients.TryAdd(client.Username, client);
    }

    //Finding a logged-in user
    public ConnectedClient? GetClient(string username)
    {
        _clients.TryGetValue(username, out var client);
        return client;
    }

    //Remove User
    //Checks whether both the key and the value are equal.
    public bool RemoveClient(ConnectedClient client)
    {
        return ((ICollection<KeyValuePair<string, ConnectedClient>>)_clients)
            .Remove(new KeyValuePair<string, ConnectedClient>(
                client.Username, client));
    }

    //List of connected users
    public List<string> GetOnlineUsers()
    {
        return _clients.Keys.OrderBy(name => name).ToList();
    }

    //Sending a message to the user
    public async Task SendAsync(ConnectedClient client, string message)
    {
        //Holding the semaphore
        await client.SendLock.WaitAsync();
        try
        {
            //Sends the message via the user's StreamWriter
            await client.Writer.WriteLineAsync(message);
        }
        finally
        {
            //Releases the semaphore even if the write operation failed.
            client.SendLock.Release();
        }
    }

    //Receiving a message from a user
    public async Task<string?> ReceiveAsync(ConnectedClient client)
    {
        return await client.Reader.ReadLineAsync();
    }

    //Closing a connection, closes the read object, closes the write object, and closes the TCP connection.
    public void CloseClient(ConnectedClient client)
    {
        client.Reader.Dispose();
        client.Writer.Dispose();
        client.TcpClient.Dispose();
    }
}