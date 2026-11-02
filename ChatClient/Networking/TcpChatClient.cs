
using System.Net.Sockets;
using System.Text;

namespace ChatClient.Networking;

public class TcpChatClient
{
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public async Task ConnectAsync(string ip, int port)
    {
        _tcpClient = new TcpClient();

        await _tcpClient.ConnectAsync(ip, port);

        NetworkStream stream = _tcpClient.GetStream();

        _reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true
        );

        _writer = new StreamWriter(
            stream,
            new UTF8Encoding(false),
            bufferSize: 1024,
            leaveOpen: true
        )
        {
            AutoFlush = true
        };
    }

    public async Task SendAsync(string message)
    {
        if (_writer == null)
            throw new InvalidOperationException("Not connected to server");

        await _writer.WriteLineAsync(message);
    }

    public async Task<string?> ReceiveAsync()
    {
        if (_reader == null)
            throw new InvalidOperationException("Not connected to server");

        return await _reader.ReadLineAsync();
    }

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
