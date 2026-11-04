using ChatServer.Data;
using ChatServer.Networking;
using ChatServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Loading settings
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Reading the connection string
string connectionString =
    configuration.GetConnectionString("MySql")
    ?? throw new InvalidOperationException(
        "MySQL connection string is missing");

// Configuring EF Core to work with MySQL
var options = new DbContextOptionsBuilder<MySqlDbContext>()
    .UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 4, 0)))
    .Options;

// Creating the user service and preparing the database
UserService userService = new UserService(options);

await userService.InitializeAsync();

// Creating the chat services
ClientService clientService = new ClientService();

MessageService messageService = new MessageService(clientService);

// Reading server settings
string ip = configuration["Server:IpAddress"] ?? "127.0.0.1";

int port = configuration.GetValue<int>("Server:Port", 5000);

// Starting the server
TcpServer server = new TcpServer(
    ip,
    port,
    clientService,
    messageService,
    userService);

await server.StartAsync();