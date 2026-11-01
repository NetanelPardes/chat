using System.Net;
using System.Net.Sockets;
using ChatServer.Services;

namespace ChatServer.Networking;

//class responsible for creating a listening server and handling connected users.
public class TcpServer
{
    //object that listens for TCP
    private readonly TcpListener _server;
    
    //object that handles a connected client
    private readonly ClientHandler _clientHandler;

    //Constructor
    public TcpServer(string ip,int port,ClientService clientService,MessageService messageService)
    {
        //create TcpListener with ip and port 
        _server = new TcpListener(IPAddress.Parse(ip), port);

        //create ClientHandler and get evry concted list
        _clientHandler = new ClientHandler(clientService,messageService);
    }

    //method that activates the server
    public async Task StartAsync()
    {
        //activates the server
        _server.Start();

        Console.WriteLine("TCP Server started!");
        Console.WriteLine($"Listening on: {_server.LocalEndpoint}");

        //The Infinite Loop
        //Waiting for new connections
        while (true)
        {
            //Accepting a new client
            //Waiting asynchronously
            TcpClient tcpClient = await _server.AcceptTcpClientAsync();

            Console.WriteLine("New client connected!");

            //Passes the connection to HandleClientAsync for processing.
            _ = _clientHandler.HandleClientAsync(tcpClient);
        }
    }
}