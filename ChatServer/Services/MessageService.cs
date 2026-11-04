using ChatServer.Data;
using ChatServer.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatServer.Services;

//Responsible for facilitating private messages between users within our chat system.
public class MessageService
{
    //Holds a reference to the ClientService object
    private readonly ClientService _clientService;

    // Provides access to messages stored in MongoDB
    private readonly MongoDbContext _mongoDb;

    //Constructor
    public MessageService(ClientService clientService,MongoDbContext mongoDb)
    {
        _clientService = clientService;
        _mongoDb = mongoDb;
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

        // Searching for the recipient in MongoDB, even when offline
        User? receiverUser;

        try
        {
            receiverUser = await _mongoDb.Users.Find(u => u.Username == receiverName).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            Console.WriteLine($"Failed to find recipient: {ex.Message}");
            await _clientService.SendAsync(sender, "Could not find recipient");
            return;
        }

        // The recipient must have registered at least once
        if (receiverUser == null)
        {
            await _clientService.SendAsync(sender, "User not found");
            return;
        }

        // Creating the message document
        ChatMessage message = new ChatMessage
        {
            SenderId = sender.UserId,
            ReceiverId = receiverUser.Id,
            Content = content,
            Status = "STORED"
        };

        try
        {
            // Saving the message before attempting delivery
            await _mongoDb.Messages.InsertOneAsync(message);
        }
        catch (MongoException ex)
        {
            Console.WriteLine($"Failed to save message: {ex.Message}");
            await _clientService.SendAsync(sender, "Message could not be saved");
            return;
        }

        // Check the active connection after saving the message
        ConnectedClient? receiver = _clientService.GetClient(receiverUser.Username);

        if (receiver == null || receiver.UserId != receiverUser.Id)
        {
            // Leave the message STORED until the recipient connects
            await _clientService.SendAsync(sender,"Message saved for offline delivery");

            return;
        }

        // Sending the saved message to the connected recipient
        bool sent = await SendStoredMessageAsync(receiver,message,sender.Username);

        if (!sent)
        {
            await _clientService.SendAsync(sender,"Message saved for delivery on next connection");
        }
    }
    // Sending an existing message without creating another document
    private async Task<bool> SendStoredMessageAsync(ConnectedClient receiver,ChatMessage message,string senderName)
    {
        string status = "SEND_FAILED";
        bool sent = false;

        try
        {
            await _clientService.SendAsync(receiver,$"MSG:{message.Id}:{senderName}:{message.Content}");

            status = "SENT";
            sent = true;

            Console.WriteLine($"Message \"{message.Content}\" sent from " +
                $"{senderName} to {receiver.Username}");
        }
        catch (IOException)
        {
            Console.WriteLine($"Message {message.Id}: connection write failed");
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine($"Message {message.Id}: connection already closed");
        }

        try
        {
            // Do not overwrite DELIVERED if the confirmation arrived first
            var filter = Builders<ChatMessage>.Filter
                .Eq(m => m.Id, message.Id)
                & Builders<ChatMessage>.Filter
                    .Ne(m => m.Status, "DELIVERED");

            var update = Builders<ChatMessage>.Update
                .Set(m => m.Status, status);

            await _mongoDb.Messages.UpdateOneAsync(filter, update);
        }
        catch (MongoException ex)
        {
            Console.WriteLine($"Failed to update message {message.Id}: {ex.Message}");
        }

        return sent;
    }
    // Sending messages that the recipient has not confirmed yet
    public async Task SendPendingMessagesAsync(ConnectedClient receiver)
    {
        try
        {
            var filter = Builders<ChatMessage>.Filter
                .Eq(m => m.ReceiverId, receiver.UserId) & Builders<ChatMessage>.Filter
                    .Ne(m => m.Status, "DELIVERED");

            // Oldest messages first, with the ID as a tie-breaker
            var sort = Builders<ChatMessage>.Sort
                .Ascending(m => m.SentAt)
                .Ascending(m => m.Id);

            using var cursor = await _mongoDb.Messages
                .Find(filter)
                .Sort(sort)
                .ToCursorAsync();

            while (await cursor.MoveNextAsync())
            {
                foreach (ChatMessage message in cursor.Current)
                {
                    // Finding the original sender's name
                    User? sender = await _mongoDb.Users
                        .Find(u => u.Id == message.SenderId)
                        .FirstOrDefaultAsync();

                    string senderName = sender?.Username ?? "Unknown";

                    bool sent = await SendStoredMessageAsync(receiver,message,senderName);

                    // Stop if this connection can no longer receive messages
                    if (!sent)
                        return;
                }
            }
        }
        catch (MongoException ex)
        {
            // Keep the connection alive if loading pending messages fails
            Console.WriteLine($"Failed to load pending messages: {ex.Message}");
        }
    }

    // Updating the message only when its actual receiver confirms receipt
    public async Task ConfirmDeliveryAsync(ConnectedClient receiver,string messageId)
    {
        // Reject invalid message IDs
        if (!ObjectId.TryParse(messageId, out ObjectId id))
            return;

        var filter = Builders<ChatMessage>.Filter
            .Eq(m => m.Id, id)
            & Builders<ChatMessage>.Filter
                .Eq(m => m.ReceiverId, receiver.UserId)
            & Builders<ChatMessage>.Filter
                .Ne(m => m.Status, "DELIVERED");

        var update = Builders<ChatMessage>.Update
            .Set(m => m.Status, "DELIVERED");

        try
        {
            await _mongoDb.Messages.UpdateOneAsync(filter, update);
        }
        catch (MongoException ex)
        {
            // Keep the client connected even if the status update fails
            Console.WriteLine($"Failed to confirm delivery for {id}: {ex.Message}");
        }
    }
}