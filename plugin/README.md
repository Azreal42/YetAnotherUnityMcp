# Unity MCP Plugin

This Unity plugin implements a WebSocket server that allows AI agents to control and interact with Unity using the Model Context Protocol (MCP).

## Installation

1. Import the plugin into your Unity project
2. The plugin will automatically register itself in the Unity Editor menu
3. By default, the server will automatically start when Unity launches (configurable)
4. Alternatively, open the server window from the menu: `Window > MCP Server`
5. Start the server using the "Start Server" button if not using auto-start
6. Connect to the server from the Python MCP client

## Usage

### MCP Server Window

The plugin includes a server window for managing and monitoring WebSocket connections. Open it from the menu:

```
Window > MCP Server
```

This window allows you to:
- Start and stop the WebSocket server
- View connected clients
- Monitor message traffic
- Execute local commands for testing
- Configure server options (host, port)
- Enable/disable auto-start on Unity launch
- View performance statistics

### Local Command Testing

For testing without connecting to the Python client, you can use the MCP Server window's local command testing section:

```
Window > MCP Server > Local Command Testing
```

This section allows you to execute commands directly within Unity:
- Execute C# code with syntax highlighting
- Take screenshots with configurable resolution 
- Modify GameObject properties with type conversion
- Get Unity information and logs
- View command results in real time

### Server API

You can use the MCPWebSocketServer API in your own scripts:

```csharp
// Get the server instance
using YetAnotherUnityMcp.Editor.WebSocket;
MCPWebSocketServer server = MCPWebSocketServer.Instance;

// Start the server (returns true on success)
bool success = await server.StartAsync("localhost", 8080);

// Get server status
bool isRunning = server.IsRunning;
string serverUrl = server.ServerUrl;
int clientCount = server.ClientCount;

// Send a message to a specific client
await server.SendMessageAsync(clientId, messageObject);

// Broadcast a message to all clients
await server.BroadcastMessageAsync(messageObject);

// Stop the server
await server.StopAsync();

// Subscribe to events
server.OnServerStarted += HandleServerStarted;
server.OnServerStopped += HandleServerStopped;
server.OnClientConnected += HandleClientConnected;
server.OnClientDisconnected += HandleClientDisconnected;
server.OnMessageReceived += HandleMessageReceived;
server.OnError += HandleError;

// Control auto-start behavior
MCPServerInitializer.AutoStartEnabled = true; // Enable auto-start
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
- Automatic server startup on Unity Editor launch (configurable)
- Thread-safe message processing on Unity's main thread
- Comprehensive event system for server and client events
- Performance monitoring with detailed metrics
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