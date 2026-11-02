
using ChatClient.Models;
using ChatClient.Networking;

namespace ChatClient.Services;

public class ChatService
{
    private readonly TcpChatClient _tcpClient;

    public ChatService(TcpChatClient tcpClient)
    {
        _tcpClient = tcpClient;
    }

    public async Task ConnectAsync(string ip, int port, string username)
    {
        await _tcpClient.ConnectAsync(ip, port);

        string? request = await _tcpClient.ReceiveAsync();

        if (request != "Enter username:")
            throw new InvalidOperationException("Invalid server response");

        await _tcpClient.SendAsync(username);

        string? response = await _tcpClient.ReceiveAsync();

        if (response != $"Welcome {username}!")
            throw new InvalidOperationException(
                response ?? "Connection closed by server"
            );
    }

    public async Task SendMessageAsync(ChatMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Receiver))
            throw new ArgumentException("Receiver cannot be empty");

        if (string.IsNullOrWhiteSpace(message.Content))
            throw new ArgumentException("Message cannot be empty");

        string text = $"{message.Receiver}:{message.Content}";

        await _tcpClient.SendAsync(text);
    }

    public async Task RequestOnlineUsersAsync()
    {
        await _tcpClient.SendAsync("/users");
    }

    public async Task<string?> ReceiveMessageAsync()
    {
        return await _tcpClient.ReceiveAsync();
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _tcpClient.SendAsync("/exit");
        }
        finally
        {
            _tcpClient.Disconnect();
        }
    }
}
