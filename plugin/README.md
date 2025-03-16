# Unity MCP Plugin

This Unity plugin allows communication between Unity and the Model Context Protocol (MCP) server, enabling AI agents to control and interact with Unity.

## Installation

1. Import the plugin into your Unity project
2. Add the MCPClient component to a GameObject in your scene (or use the provided prefab)
3. Configure the server URL and other settings in the inspector

## Usage

### Editor Window

The plugin includes an editor window for testing and debugging. Open it from the menu:

```
Window > MCP Client
```

### Runtime API

You can use the MCPClient API in your own scripts:

```csharp
// Get the MCP client
MCPClient client = MCPClient.Instance;

// Execute code
string result = await client.ExecuteCode("Debug.Log(\"Hello from MCP!\");");

// Take a screenshot
StartCoroutine(client.TakeScreenshot("Assets/screenshot.png"));

// Modify an object
string result = await client.ModifyObject("MainCamera", "position.x", 10f);

// Get logs
string logs = await client.GetLogs();

// Get Unity info
string info = await client.GetUnityInfo();
```

## Features

- Execute C# code in the Unity Editor
- Take screenshots of the Unity Editor
- Modify GameObject properties
- Access logs and Unity information
- Editor window for testing and debugging

## Requirements

- Unity 2020.3 or later
- .NET 4.x Scripting Runtime
- MCP server running (see the server/ directory in the main project)

## License

This project is licensed under the MIT License - see the LICENSE file for details.