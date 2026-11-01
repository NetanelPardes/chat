using ChatServer.Models;

namespace ChatServer.Services;

public class MessageService
{
    private readonly ClientService _clientService;

    public MessageService(ClientService clientService)
    {
        _clientService = clientService;
    }

    public async Task SendPrivateMessageAsync(
        ConnectedClient sender,
        string receiverName,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            await _clientService.SendAsync(sender, "Message cannot be empty");
            return;
        }

        ConnectedClient? receiver = _clientService.GetClient(receiverName);

        if (receiver == null)
        {
            await _clientService.SendAsync(sender, "User not connected");
            return;
        }

        try
        {
            await _clientService.SendAsync(
                receiver,
                $"{sender.Username}: {content}"
            );

            await _clientService.SendAsync(sender, "Message sent!");
        }
        catch (IOException)
        {
            await _clientService.SendAsync(sender, "Message delivery failed");
        }
        catch (ObjectDisposedException)
        {
            await _clientService.SendAsync(sender, "Message delivery failed");
        }
    }
}