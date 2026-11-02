//Runs the chat software and manages two operations simultaneously: receiving messages from the server and sending messages typed by the user.
using ChatClient.Models;
using ChatClient.Networking;
using ChatClient.Services;
using ChatClient.UI;

//Creating the Objects
TcpChatClient tcpClient = new TcpChatClient();
ChatService chatService = new ChatService(tcpClient);
ConsoleUI ui = new ConsoleUI();

ui.ShowWelcome();

//Username Request
Console.Write("Enter username : ");
string? username = ui.ReadInput();

//Username check
while(string.IsNullOrWhiteSpace(username))
{
    ui.ShowError("Username cannot be empty");
    Console.Write("Enter username : ");
    username = ui.ReadInput();
}

username = username.Trim();

try
{
    await chatService.ConnectAsync("127.0.0.1", 5000, username);

    ui.ShowInfo($"Connected as {username}");
    ui.ShowInfo("Use Recipient:Message, /users or /exit");

    // Receive messages independently of user input
    Task receiveTask = Task.Run(async () =>
    {
        try
        {
            while (true)
            {
                string? incoming = await chatService.ReceiveMessageAsync();

                if (incoming == null)
                    break;

                if (incoming.Contains(':') && !incoming.StartsWith("Online:"))
                {
                    int separatorIndex = incoming.IndexOf(':');

                    ChatMessage message = new ChatMessage
                    {
                        Sender = incoming[..separatorIndex].Trim(),
                        Receiver = username,
                        Content = incoming[(separatorIndex + 1)..].Trim()
                    };

                    ui.ShowMessage(message);
                }
                else
                {
                    ui.ShowInfo(incoming);
                }
                            }
        }
        catch (IOException)
        {
            ui.ShowError("Connection lost");
        }
        catch (ObjectDisposedException)
        {
            // Connection closed during shutdown
        }
    });

    // Read and send user input
    while (true)
    {
        string? input = ui.ReadInput();

        if (input == null || input == "/exit")
            break;

        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input == "/users")
        {
            await chatService.RequestOnlineUsersAsync();
            continue;
        }

        int separatorIndex = input.IndexOf(':');

        if (separatorIndex <= 0)
        {
            ui.ShowError("Use format: Recipient:Message");
            continue;
        }

        ChatMessage message = new ChatMessage
        {
            Sender = username,
            Receiver = input[..separatorIndex].Trim(),
            Content = input[(separatorIndex + 1)..]
        };

        await chatService.SendMessageAsync(message);

        ui.ShowSentMessage(message);
    }
}
catch (Exception ex)
{
    ui.ShowError(ex.Message);
}
finally
{
    tcpClient.Disconnect();
}
