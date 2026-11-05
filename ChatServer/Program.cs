using ChatServer.Data;
using ChatServer.Networking;
using ChatServer.Services;
using Microsoft.Extensions.Configuration;

// Loading settings
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Reading the connection string
string connectionString =
    configuration["MongoDB:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDB connection string is missing");

string databaseName =
    configuration["MongoDB:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDB database name is missing");

// Creating access to MongoDB collections
MongoDbContext mongoDb = new MongoDbContext(connectionString,databaseName);

// Creating the user service and preparing the database
UserService userService = new UserService(mongoDb);

await userService.InitializeAsync();

// Creating the chat services
ClientService clientService = new ClientService();

MessageService messageService = new MessageService(clientService, mongoDb);

// Reading server settings
string ip = configuration["Server:IpAddress"] ?? "127.0.0.1";

int port = configuration.GetValue<int>("Server:Port", 5000);

// Starting the server
TcpServer server = new TcpServer(ip,port,clientService,messageService,userService);

await server.StartAsync();