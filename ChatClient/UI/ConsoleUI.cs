
using ChatClient.Models;

namespace ChatClient.UI;

public class ConsoleUI
{
    public void ShowWelcome()
    {
        Console.WriteLine("=== TCP CHAT ===");
        Console.WriteLine("Welcome to the chat!");
        Console.WriteLine();
    }

    public void ShowMessage(ChatMessage message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"↓ {message.Sender}: {message.Content}");
        Console.ResetColor();
    }

    public void ShowSentMessage(ChatMessage message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"↑ {message.Sender}: {message.Content}");
        Console.ResetColor();
    }

    public void ShowInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void ShowError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }

    public string? ReadInput()
    {
        return Console.ReadLine();
    }
}
