# YetAnotherUnityMcp

DO NOT USE THIS. THIS IS A TOY PROJECT TO SEE WHAT I CAN DO WITH CLAUDE CODE.
I'M TRYING TO ASSESS IF A DEV CAN CORRECTLY WORK WITH JUST VIBE CODING
SO FAR, IT SEEMS NOT TO BE THE CASE !


A Unity Master Control Protocol (MCP) implementation that allows AI agents to control and interact with Unity.

## Overview

**YetAnotherUnityMcp** is a system that bridges the Unity game engine with AI-driven tools using the **Model Context Protocol (MCP)**. It consists of a Unity **.NET/C# plugin** acting as the MCP WebSocket server, and a **Python MCP client** (built with FastMCP) that handles requests from AI agents. Communication between Unity and the client is done via **WebSockets**, enabling real-time, bidirectional exchange of JSON messages and image data.

This architecture cleanly separates the game engine concerns from the AI logic, improving scalability and maintainability. The goal is to allow AI agents (e.g. an LLM-based assistant) to **inspect and control a running Unity scene** in a structured, safe manner.

Key components include:

1. **Unity MCP Plugin (Server)** – A C# plugin integrated into the Unity Editor that hosts a WebSocket server
2. **FastMCP Python Client** – A Python application that implements the MCP interface for Unity
3. **MCP Client (AI or External)** – The external entity (such as an AI assistant or testing script) that sends MCP requests

## What is MCP?

The [Model Context Protocol (MCP)](https://modelcontextprotocol.io) is a standardized way for AI models to interact with applications. It separates the concerns of providing context from the LLM interaction itself, allowing for:

- **Resources**: Providing data to LLMs (like Unity scene hierarchies)
- **Tools**: Allowing LLMs to take actions (like executing code in Unity)
- **Prompts**: Defining interaction templates (like how to create GameObjects)

## Features

- Execute C# code in Unity from AI agents
- Query Unity Editor state through MCP resources
- Capture screenshots with AI-driven parameters
- Get logs and debug information from Unity
- Modify GameObject properties with AI assistance
- List and navigate GameObject hierarchies
- Provide contextual templates through MCP prompts
- Real-time communication via WebSockets
- WebSocket server hosted directly in Unity
- Fast, efficient JSON serialization

## Getting Started

### Unity Server Setup

1. Open your Unity project (2020.3 or later)
2. Import the YetAnotherUnityMcp plugin using one of these methods:
   - Copy the `plugin/Scripts` folder to your Unity project's Assets directory
   - Create a Unity package and import it
   - Create a symbolic link for development (Windows PowerShell example):
     ```powershell
     New-Item -ItemType SymbolicLink -Target "D:\Dev\YetAnotherUnityMcp\plugin" -Path "C:\Users\azrea\My project\Assets\Plugins\YetAnotherUnityMcp"
     ```
3. Start the WebSocket server:
   - From the menu: MCP > WebSocket Server > Start Server
   - Or: Window > WebSocket MCP Server > Start Server
4. Note the WebSocket URL (default: ws://localhost:8080/)

### Python Client Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/YetAnotherUnityMcp.git
cd YetAnotherUnityMcp

# Create and activate a virtual environment using uv
uv venv -p 3.11
source .venv/bin/activate  # On Windows: .venv\Scripts\activate

# Install the server with development dependencies
uv pip install -e ".[dev]"

# Run the MCP client
python -m server.mcp_server
```

### MCP Integration

```bash
# Install FastMCP and tools
uv pip install fastmcp

# Run the client with MCP inspector for debugging
fastmcp dev server/mcp_server.py

# Install in Claude Desktop
fastmcp install server/mcp_server.py --name "Unity Controller"
```

## Project Structure

```
YetAnotherUnityMcp/
├── server/                      # Python MCP client
│   ├── mcp/                     # MCP tool and resource implementations
│   │   ├── tools/               # Tool implementations
│   │   ├── resources/           # Resource implementations
│   │   └── unity_client_util.py # Unity client utility functions
│   ├── unity_websocket_client.py # High-level Unity WebSocket client
│   ├── mcp_server.py            # MCP server implementation
│   └── websocket_client.py      # Low-level WebSocket client implementation
├── plugin/                      # Unity C# plugin
│   ├── Scripts/                 # Plugin source code
│   │   ├── Editor/              # Editor extensions
│   │   │   ├── Commands/        # Editor command implementations
│   │   │   ├── MCPWindow.cs     # Server control window
│   │   │   ├── MCPMenu.cs       # Unity menu integration
│   │   │   ├── MCPWebSocketServer.cs # High-level server implementation
│   │   │   ├── CommandExecutionMonitor.cs # Performance monitoring
│   │   │   ├── Models/          # Data models for Editor
│   │   │   └── WebSocket/       # WebSocket server implementation
│   │   └── YetAnotherUnityMcp.asmdef  # Assembly definition
│   └── README.md                # Plugin documentation
└── tests/                       # Test suite
```

## Architecture

### Unity WebSocket Server

The Unity plugin hosts a WebSocket server that listens for connections from MCP clients. This server:

- Manages client connections and message routing
- Executes commands sent by clients (e.g., running C# code, taking screenshots)
- Returns results back to clients
- Provides a UI for monitoring connections and debugging

### Python MCP Client

The Python client connects to the Unity WebSocket server and provides an MCP interface for AI tools. It:

- Translates MCP requests into WebSocket messages for Unity
- Converts Unity responses into MCP resource data
- Uses FastMCP's lifespan management for connection lifecycle
- Provides standardized error handling and reconnection logic
- Implements a unified execution pattern for all operations
- Provides tools and resources through the FastMCP framework

## MCP Resources and Tools

### Resources

- `unity://info` - Basic information about the Unity environment
- `unity://logs` - Editor logs for debugging
- `unity://scene/{scene_name}` - Information about a specific scene
- `unity://object/{object_id}` - Details about a specific GameObject

### Tools

- `execute_code_in_unity` - Run C# code in the Unity Editor
- `unity_screenshot` - Take screenshots of the Unity Editor
- `unity_modify_object` - Change properties of Unity GameObjects
- `unity_logs` - Get logs from Unity

## Communication Protocol

All communication between the Unity server and the Python client uses a **WebSocket** connection, which allows persistent, low-latency bidirectional messaging. The connection is initiated by the Python client to the Unity server's WebSocket endpoint (e.g. `ws://localhost:8080/`).

Every message is a JSON object containing at least a **command or response type**, a **unique ID** (to pair requests with responses), and a **parameters or result object**. For more details on the communication protocol, see the [Technical Details](TECH_DETAILS.md) document.

## Development

```bash
# Python client development
python -m pytest                    # Run tests
python -m black .                   # Format code
python -m flake8                    # Lint code
python -m mypy .                    # Type check

# MCP Development
fastmcp dev server/mcp_server.py    # Run with MCP Inspector UI

# Unity server development
# Use the WebSocket Server window in Unity for debugging
# Monitor connections and messages in real-time
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

For more details on architecture, implementation, and extensibility, see the [Technical Details](TECH_DETAILS.md) document.