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
while (string.IsNullOrWhiteSpace(username))
{
    ui.ShowError("Username cannot be empty");
    Console.Write("Enter username : ");
    username = ui.ReadInput();
}

username = username.Trim();

//Message reception loop
try
{
    //Connecting to server
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
                //Waiting for the next message from the server
                string? incoming = await chatService.ReceiveMessageAsync();

                //Returning null indicates that we have reached the end.
                if (incoming == null)
                    break;

                // Parsing a private message, including its server-generated ID
                ChatMessage? message = chatService.ParseReceivedMessage(
                    incoming, username);

                if (message != null)
                {
                    //Passes the object to a function that displays an incoming message on the screen.
                    ui.ShowMessage(message);

                    // Automatically confirming receipt after displaying the message
                    await chatService.ConfirmDeliveryAsync(message.Id);
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

    // The Typing and Sending Loop
    while (true)
    {
        //Reads input from the user (the message to be sent)
        string? input = ui.ReadInput();

        if (input == null || input == "/exit")
            break;

        //Skip empty input
        if (string.IsNullOrWhiteSpace(input))
            continue;

        //Handling the /users command
        if (input == "/users")
        {
            await chatService.RequestOnlineUsersAsync();
            continue;
        }

        //Checking the format of the message to be sent
        int separatorIndex = input.IndexOf(':');

        if (separatorIndex <= 0)
        {
            ui.ShowError("Use format: Recipient:Message");
            continue;
        }

        //Creating the outgoing message
        ChatMessage message = new ChatMessage
        {
            Sender = username,
            Receiver = input[..separatorIndex].Trim(),
            Content = input[(separatorIndex + 1)..]
        };

        //Sending and Displaying on Screen
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