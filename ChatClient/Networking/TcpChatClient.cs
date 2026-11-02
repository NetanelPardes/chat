using System.Net.Sockets;
using System.Text;

namespace ChatClient.Networking;

//Responsible for four core operations: connecting to the server, sending messages, receiving messages, and disconnecting.
public class TcpChatClient
{
    //Represents the TCP connection
    private TcpClient? _tcpClient;
    //Allows us to read the message as a string
    private StreamReader? _reader;
    //Allows writing text to the server
    private StreamWriter? _writer;

    //Connecting to server
    public async Task ConnectAsync(string ip, int port)
    {
        _tcpClient = new TcpClient();

        //Attempting to connect to the server
        await _tcpClient.ConnectAsync(ip, port);

        //Returns a NetworkStream object that represents the communication stream of the connection.
        NetworkStream stream = _tcpClient.GetStream();

        //This is the creation of a StreamReader object.
        _reader = new StreamReader(stream,Encoding.UTF8,detectEncodingFromByteOrderMarks: true,bufferSize: 1024,leaveOpen: true);

        //This object is responsible for sending text to the server.
        _writer = new StreamWriter(stream,new UTF8Encoding(false),bufferSize: 1024,leaveOpen: true)
        {
            AutoFlush = true
        };
    }

    //Sending a message
    public async Task SendAsync(string message)
    {
        if (_writer == null)
            throw new InvalidOperationException("Not connected to server");

        await _writer.WriteLineAsync(message);
    }

    //Receiving a message
    public async Task<string?> ReceiveAsync()
    {
        if (_reader == null)
            throw new InvalidOperationException("Not connected to server");

        return await _reader.ReadLineAsync();
    }

    //Closing the connection
    public void Disconnect()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _tcpClient?.Dispose();

        _reader = null;
        _writer = null;
        _tcpClient = null;
    }
}
