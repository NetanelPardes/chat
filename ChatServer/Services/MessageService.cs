using ChatServer.Models;

namespace ChatServer.Services;

//Responsible for facilitating private messages between users within our chat system.
public class MessageService
{
    //Holds a reference to the ClientService object
    private readonly ClientService _clientService;

    //Constructor
    public MessageService(ClientService clientService)
    {
        _clientService = clientService;
    }

    //A function that handles sending the message
    public async Task SendPrivateMessageAsync(ConnectedClient sender,string receiverName,string content)
    {
        //Check that the message is not empty
        if (string.IsNullOrWhiteSpace(content))
        {
            await _clientService.SendAsync(sender, "Message cannot be empty");
            return;
        }

        //Searching for the Recipient
        ConnectedClient? receiver = _clientService.GetClient(receiverName);

        //Checking if the recipient is connected
        if (receiver == null)
        {
            await _clientService.SendAsync(sender, "User not connected");
            return;
        }

        try
        {
            //Sending the message
            await _clientService.SendAsync(receiver,$"{sender.Username}: {content}");

            //Sending a confirmation to the sender
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