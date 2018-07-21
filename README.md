# StratumServer
.NET stratum server and client for pools and other mining related software

## Usage
### Fire and forget
```cs
int anyRandomPort = 500;
var serv = new StratumServer(IPAddress.Any, anyRandomPort);
serv.Start(); // Remember to start server, creating object instance is not equivalent to starting a server
serv.ListenAsync(); // Listen for new clients, no need to await
serv.ClientConnected += (s1, clientArgs) =>
{
    Console.WriteLine("Client connected");
    clientArgs.Client.ListenAsync(); // Listen for messages from client, no need to await
    clientArgs.Client.MessageReceived += (s2, messageArgs) =>
    {
        Console.WriteLine($"Message received: {messageArgs.Message}");
        clientArgs.Client.SendAsync(messageArgs.Message); // Send any message to client
    };
    clientArgs.Client.Disconnected += (s3, disconnectedArgs) => // Due to TCP nature you won't be notified if you are not listening
    {
        Console.WriteLine($"Client disonnected: {disconnectedArgs.Client.IP}:{disconnectedArgs.Client.Port}");
    };
};
```

### Other properties and functions
```cs
// Stop listening for new clients
serv.StopListening();

// Stop server
serv.Stop();

// Get collection of all active clients
IEnumerable clients = serv.Clients;

// Send message to all active clients
await serv.Broadcast("Some message");

// Accept client
StratumClient client = await serv.AcceptClientAsync();

// Get client unique id
int id = client.Id;

// Get client IP
string ip = client.IP;

// Get client port
int port = client.Port;

// Check if client is connected
bool isConnected = client.Connected;

// Stop listening for new messages from client
client.StopListening();

// Disconnect with client
client.Disconnect();

// Send message to client
await client.SendAsync("Message");

// Read message from client
string message = await client.ReadAsync();

// Time span from last message from clinet
TimeSpan timeSpan = client.TimeFromLastMsg;
```
