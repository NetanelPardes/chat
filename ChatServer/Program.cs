//Main Executable File

using ChatServer.Networking;
using ChatServer.Services;

//Creating a list of connected users
ClientService clientService = new ClientService();

//Creating a service responsible for messages
MessageService messageService = new MessageService(clientService);

//Creating a TCP server that listens for and sends messages
TcpServer server = new TcpServer("127.0.0.1",5000,clientService,messageService);

//Synchronous server synchronization
await server.StartAsync();