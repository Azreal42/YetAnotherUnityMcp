# Python MCP Client for Unity

This Python package implements a WebSocket client that connects to Unity's WebSocket server for Model Context Protocol (MCP) communication.

## Architecture

In this architecture, Unity hosts a WebSocket server that receives commands from Python clients. The Python code acts as a WebSocket client that sends commands to Unity and processes the responses.

## Core Components

- `websocket_client.py`: Low-level WebSocket client for communicating with Unity
- `mcp_client.py`: High-level MCP client abstraction
- `mcp_server.py`: FastMCP server implementation that uses the client
- `mcp/tools/`: Implementations of MCP tools that communicate with Unity
- `mcp/resources/`: Implementations of MCP resources that fetch data from Unity

## Supported Commands

The client can send the following commands to Unity:

```python
commands = [
    "execute_code",  # code -> result
    "take_screenshot",  # output_path, width, height -> result
    "modify_object",  # object_id, property_path, property_value -> result
    "get_logs",  # max_logs -> list[logs]
    "get_unity_info",  # void -> { "unity_version", etc. }
]
```

## MCP Tools and Resources

### Tools
- `execute_code_in_unity`: Execute C# code in Unity
- `unity_screenshot`: Take screenshots in Unity
- `unity_modify_object`: Modify GameObject properties
- `unity_logs`: Get logs from Unity

### Resources
- `unity://info`: Get Unity environment information
- `unity://logs`: Get Unity logs
- `unity://scene/{scene_name}`: Get scene information
- `unity://object/{object_id}`: Get GameObject information

## Usage

### Direct Client Usage

```python
from server.mcp_client import get_client

# Get the MCP client
client = get_client("ws://localhost:8080/")

# Connect to Unity
await client.connect()

# Execute code
result = await client.execute_code("Debug.Log('Hello from Python!');")

# Take a screenshot
result = await client.take_screenshot("screenshot.png", 1920, 1080)

# Disconnect
await client.disconnect()
```

### FastMCP Usage

```python
# Run the MCP server with STDIO transport
python -m server.mcp_server

# Run with FastMCP's development tool
fastmcp dev server/mcp_server.py

# Install in Claude Desktop
fastmcp install server/mcp_server.py --name "Unity Controller"
```

## Communication Protocol

All messages use JSON format:

### Command Request
```json
{
  "id": "req_101",
  "command": "execute_code",
  "parameters": {
    "code": "Debug.Log(\"Hello from MCP!\");"
  },
  "client_timestamp": 1710956378123
}
```

### Command Response
```json
{
  "id": "req_101",
  "type": "response",
  "status": "success",
  "result": {
    "output": "Hello from MCP!",
    "logs": ["Hello from MCP!"],
    "returnValue": null
  },
  "server_timestamp": 1710956378456,
  "client_timestamp": 1710956378123
}
```