using System.Net.Sockets;
using System.Text;
using ChatServer.Models;
using ChatServer.Services;
using MongoDB.Bson;

namespace ChatServer.Networking;

//The class responsible for handling each client connects to our central server.
public class ClientHandler
{
    //Allows managing connected users
    private readonly ClientService _clientService;

    //Allows managing messages between users
    private readonly MessageService _messageService;

    private readonly UserService _userService;

    //Constructor
    public ClientHandler(ClientService clientService,MessageService messageService,UserService userService)
    {
        _clientService = clientService;
        _messageService = messageService;

        // The service that updates the user record in MongoDB
        _userService = userService;
    }

    public async Task HandleClientAsync(TcpClient tcpClient)
    {
        ConnectedClient? client = null;

        try
        {
            //If I exit the loop, the connection is disconnected.
            using (tcpClient)

            //Returns an object that allows reading and writing bytes over the TCP connection.
            using (NetworkStream stream = tcpClient.GetStream())

            //Allows us to read text from the connection, instead of working directly with arrays of bytes.
            using (StreamReader reader = new(stream,Encoding.UTF8,detectEncodingFromByteOrderMarks: true,bufferSize: 1024,leaveOpen: true))

            //Receives text, encodes it into bytes, and sends them.
            using (StreamWriter writer = new(stream,new UTF8Encoding(false),bufferSize: 1024,leaveOpen: true){ AutoFlush = true })
            {
                //Username Request
                await writer.WriteLineAsync("Enter username:");

                string? username = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(username))
                    return;

                username = username.Trim();

                if (username.Length < 2 || username.Length > 30 || username.Contains(':') || username.Contains(' '))
                {
                    await writer.WriteLineAsync("Invalid username");
                    return;
                }

                //Creating a ConnectedClient
                client = _clientService.CreateClient(username, tcpClient, reader, writer);

                //If such a username already exists
                if (!_clientService.AddClient(client))
                {
                    await writer.WriteLineAsync("Username already connected");
                    client = null;
                    return;
                }

                // Saving the user to the database and updating the status
                var user = await _userService.MarkOnlineAsync(username);

                // Storing the user ID within the active connection
                client.UserId = user.Id;

                //Successful login
                Console.WriteLine($"{username} connected");

                //Sends a message to the user's window via TCP
                await _clientService.SendAsync(client, $"Welcome {username}!");

                // Delivering messages saved while the user was offline
                await _messageService.SendPendingMessagesAsync(client);

                //Infinite Loop
                while (true)
                {
                    //Waiting to read a line of text from the connection
                    string? message = await _clientService.ReceiveAsync(client);

                    //Exiting the loop
                    if (message == null || message == "/exit")
                        break;

                    // Handling an automatic receipt confirmation
                    if (message.StartsWith("/ack ", StringComparison.Ordinal))
                    {
                        string messageId = message[5..].Trim();

                        await _messageService.ConfirmDeliveryAsync(client, messageId);

                        continue;
                    }

                    //Send a list of users and continue in a loop.
                    if (message == "/users")
                    {
                        //Returns the list of connected users
                        List<string> users = _clientService.GetOnlineUsers();

                        //Sending the list to the user
                        await _clientService.SendAsync(client, "Online: " + string.Join(", ", users));

                        continue;
                    }

                    //The part that handles messages

                    //Finding the colons in the message
                    int separatorIndex = message.IndexOf(':');

                    //Checking the message format
                    if (separatorIndex <= 0)
                    {
                        await _clientService.SendAsync(client, "Use format: Recipient:Message");

                        continue;
                    }

                    //Extracting the recipient's name
                    string receiverName = message[..separatorIndex].Trim();

                    //Extracting message content
                    string content = message[(separatorIndex + 1)..];

                    //Sending the message via MessageService
                    await _messageService.SendPrivateMessageAsync(client, receiverName, content);
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
            try
            {
                if (client != null)
                {
                    try
                    {
                        // If the user is saved successfully, mark them as disconnected
                        if (client.UserId != ObjectId.Empty)
                        {
                            await _userService.MarkOfflineAsync(client.UserId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to update user status: {ex.Message}");
                    }
                    finally
                    {
                        // Remove the connection even if the database update fails
                        _clientService.RemoveClient(client);

                        Console.WriteLine($"{client.Username} disconnected");
                    }
                }
            }
            finally
            {
                tcpClient.Dispose();
            }
        }
    }
}