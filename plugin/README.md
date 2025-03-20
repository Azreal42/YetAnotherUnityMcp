# Unity MCP Plugin

This Unity plugin implements a WebSocket server that allows AI agents to control and interact with Unity using the Model Context Protocol (MCP).

## Installation

1. Import the plugin into your Unity project
2. Open the WebSocket Server window from the menu: `Window > WebSocket MCP Server`
3. Start the server using the "Start Server" button
4. Connect to the server from the Python MCP client

## Usage

### WebSocket Server Window

The plugin includes a server window for managing and monitoring WebSocket connections. Open it from the menu:

```
Window > WebSocket MCP Server
```

This window allows you to:
- Start and stop the WebSocket server
- View connected clients
- Monitor message traffic
- Configure server options (host, port)

### Local Client Window

For testing without connecting to the Python client, you can use the local client window:

```
Window > MCP Client
```

This window allows you to execute commands directly within Unity:
- Execute C# code
- Take screenshots
- Modify GameObject properties
- Get Unity information and logs

### Server API

You can use the MCPWebSocketServer API in your own scripts:

```csharp
// Get the server instance
MCPWebSocketServer server = MCPWebSocketServer.Instance;

// Start the server
await server.StartAsync("localhost", 8080);

// Send a message to a specific client
await server.SendMessageAsync(clientId, messageObject);

// Broadcast a message to all clients
await server.BroadcastMessageAsync(messageObject);

// Stop the server
await server.StopAsync();

// Subscribe to events
server.OnServerStarted += HandleServerStarted;
server.OnClientConnected += HandleClientConnected;
server.OnMessageReceived += HandleMessageReceived;
```

### Local Execution API

You can use the MCPLocalCommandExecutor for direct command execution without WebSockets:

```csharp
// Get the local command executor
MCPLocalCommandExecutor executor = MCPLocalCommandExecutor.Instance;

// Execute a command
string result = executor.ExecuteCommand("execute_code", new Dictionary<string, object>
{
    { "code", "Debug.Log(\"Hello from MCP!\");" }
});
```

## Features

- WebSocket server for handling connections from MCP clients
- Local command execution for testing without a client
- Execute C# code in the Unity Editor
- Take screenshots of the Unity Editor
- Modify GameObject properties
- Access logs and Unity information
- Server monitoring and management window

## Requirements

- Unity 2020.3 or later
- .NET 4.x Scripting Runtime

## License

This project is licensed under the MIT License - see the LICENSE file for details.