using System.Net.Sockets;
using System.Text;
using ChatServer.Models;
using ChatServer.Services;

namespace ChatServer.Networking;

public class ClientHandler
{
    private readonly ClientService _clientService;
    private readonly MessageService _messageService;

    public ClientHandler(ClientService clientService,MessageService messageService)
    {
        _clientService = clientService;
        _messageService = messageService;
    }

    public async Task HandleClientAsync(TcpClient tcpClient)
    {
        ConnectedClient? client = null;

        try
        {
            using (tcpClient)
            using (NetworkStream stream = tcpClient.GetStream())
            using (StreamReader reader = new(stream,Encoding.UTF8,detectEncodingFromByteOrderMarks: true,bufferSize: 1024,leaveOpen: true))
            using (StreamWriter writer = new(stream,new UTF8Encoding(false),bufferSize: 1024,leaveOpen: true) { AutoFlush = true })
            {
                await writer.WriteLineAsync("Enter username:");

                string? username = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(username))
                    return;

                username = username.Trim();

                if (username.Length > 30 || username.Contains(':') || username.StartsWith('/'))
                {
                    await writer.WriteLineAsync("Invalid username");
                    return;
                }

                client = _clientService.CreateClient(username,tcpClient,reader,writer);

                if (!_clientService.AddClient(client))
                {
                    await writer.WriteLineAsync("Username already connected");
                    client = null;
                    return;
                }

                Console.WriteLine($"{username} connected");

                await _clientService.SendAsync(client,$"Welcome {username}!");

                while (true)
                {
                    string? message = await _clientService.ReceiveAsync(client);

                    if (message == null || message == "/exit")
                        break;

                    if (message == "/users")
                    {
                        List<string> users = _clientService.GetOnlineUsers();

                        await _clientService.SendAsync(client,"Online: " + string.Join(", ", users));

                        continue;
                    }

                    int separatorIndex = message.IndexOf(':');

                    if (separatorIndex <= 0)
                    {
                        await _clientService.SendAsync(client,"Use format: Recipient:Message");

                        continue;
                    }

                    string receiverName = message[..separatorIndex].Trim();
                    string content = message[(separatorIndex + 1)..];

                    await _messageService.SendPrivateMessageAsync(client,receiverName,content);
                }
            }
        }
        catch (IOException)
        {
            Console.WriteLine("Client connection lost");
        }
        catch (SocketException)
        {
            Console.WriteLine("Client socket error");
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("Client connection closed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
        finally
        {
            if (client != null)
            {
                _clientService.RemoveClient(client);
                Console.WriteLine($"{client.Username} disconnected");
            }

            tcpClient.Dispose();
        }
    }
}