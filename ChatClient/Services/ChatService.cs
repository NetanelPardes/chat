using ChatClient.Models;
using ChatClient.Networking;

namespace ChatClient.Services;

//Responsible for managing chat operations on the client side
public class ChatService
{
    //We keep a reference to the TcpChatClient object so that we can use its methods from within ChatService.
    private readonly TcpChatClient _tcpClient;

    //Constructor
    public ChatService(TcpChatClient tcpClient)
    {
        _tcpClient = tcpClient;
    }

    //Connecting to the chat system
    public async Task ConnectAsync(string ip, int port, string username)
    {
        //Establishing a TCP connection
        await _tcpClient.ConnectAsync(ip, port);

        //Receiving a username request
        string? request = await _tcpClient.ReceiveAsync();

        //Checking the server response
        if (request != "Enter username:")
            throw new InvalidOperationException("Invalid server response");

        //Sending the username
        await _tcpClient.SendAsync(username);

        //Receiving the connection response
        string? response = await _tcpClient.ReceiveAsync();

        //Checking connection success
        if (response != $"Welcome {username}!")
            throw new InvalidOperationException(
                response ?? "Connection closed by server");
    }

    //Sending a private message
    public async Task SendMessageAsync(ChatMessage message)
    {
        //Recipient Check
        if (string.IsNullOrWhiteSpace(message.Receiver))
            throw new ArgumentException("Receiver cannot be empty");

        //Checking the message content
        if (string.IsNullOrWhiteSpace(message.Content))
            throw new ArgumentException("Message cannot be empty");

        //Drafting the text to be sent
        string text = $"{message.Receiver}:{message.Content}";

        //Sending the message to the server
        await _tcpClient.SendAsync(text);
    }

    //Requesting the list of users
    public async Task RequestOnlineUsersAsync()
    {
        await _tcpClient.SendAsync("/users");
    }

    //Receiving a message from the server
    public async Task<string?> ReceiveMessageAsync()
    {
        return await _tcpClient.ReceiveAsync();
    }

    // Parsing MSG:Id:Sender:Content into a ChatMessage
    public ChatMessage? ParseReceivedMessage(
        string incoming,
        string username)
    {
        if (!incoming.StartsWith("MSG:", StringComparison.Ordinal))
            return null;

        // Split only three times, preserving colons inside the content
        string[] parts = incoming.Split(':', 4);

        if (parts.Length != 4
            || string.IsNullOrWhiteSpace(parts[1])
            || string.IsNullOrWhiteSpace(parts[2]))
        {
            return null;
        }

        //Constructing a ChatMessage object
        return new ChatMessage
        {
            Id = parts[1],
            Sender = parts[2],
            Receiver = username,
            Content = parts[3].Trim()
        };
    }

    // Automatically confirming receipt of a specific message
    public async Task ConfirmDeliveryAsync(string messageId)
    {
        await _tcpClient.SendAsync($"/ack {messageId}");
    }

    //Disconnect from the chat
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