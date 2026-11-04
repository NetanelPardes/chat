namespace ChatClient.Models;

public class ChatMessage
{
    // The MongoDB message ID received from the server
    public string Id { get; set; } = "";

    public string Sender { get; set; } = "";
    public string Receiver { get; set; } = "";
    public string Content { get; set; } = "";
}