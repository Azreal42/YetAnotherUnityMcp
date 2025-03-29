# YetAnotherUnityMcp

DO NOT USE THIS. THIS IS A TOY PROJECT TO SEE WHAT I CAN DO WITH CLAUDE CODE.
I'M TRYING TO ASSESS IF A DEV CAN CORRECTLY WORK WITH JUST VIBE CODING
SO FAR, IT SEEMS NOT TO BE THE CASE !


A Unity Master Control Protocol (MCP) implementation that allows AI agents to control and interact with Unity.

## Overview

**YetAnotherUnityMcp** is a system that bridges the Unity game engine with AI-driven tools using the **Model Context Protocol (MCP)**. It consists of a Unity **.NET/C# plugin** acting as the MCP TCP server, and a **Python MCP client** (built with FastMCP) that handles requests from AI agents. Communication between Unity and the client is done via a **custom TCP protocol**, enabling real-time, bidirectional exchange of JSON messages and image data.

This architecture cleanly separates the game engine concerns from the AI logic, improving scalability and maintainability. The goal is to allow AI agents (e.g. an LLM-based assistant) to **inspect and control a running Unity scene** in a structured, safe manner. The container-based approach for organizing resources and tools further improves code organization and reduces boilerplate.

Key components include:

1. **Unity MCP Plugin (Server)** – A C# plugin integrated into the Unity Editor that hosts a TCP server
2. **FastMCP Python Client** – A Python application that implements the MCP interface for Unity
3. **MCP Client (AI or External)** – The external entity (such as an AI assistant or testing script) that sends MCP requests

## What is MCP?

The [Model Context Protocol (MCP)](https://modelcontextprotocol.io) is a standardized way for AI models to interact with applications. It separates the concerns of providing context from the LLM interaction itself, allowing for:

- **Resources**: Providing data to LLMs (like Unity scene hierarchies)
- **Tools**: Allowing LLMs to take actions (like executing code in Unity)
- **Prompts**: Defining interaction templates (like how to create GameObjects)

YetAnotherUnityMcp implements the official MCP specification with full compliance, including:
- Content-array based responses
- URI-based resource descriptors
- Required parameter arrays at schema level
- MIME type specifications for resources

## Features

- Execute C# code in Unity from AI agents
- Query Unity Editor state through MCP resources with dynamic parameter handling
- Organize MCP resources and tools in logical containers for better organization
- Capture screenshots with AI-driven parameters
- Get logs and debug information from Unity with real-time monitoring and incremental retrieval
- Modify GameObject properties with AI assistance
- List and navigate GameObject hierarchies
- Provide contextual templates through MCP prompts
- Real-time communication via TCP sockets
- TCP server hosted directly in Unity
- Fast, efficient JSON serialization
- Dynamic resource invocation with type-safe parameter mapping
- Schema-based input validation for tools and resources

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
3. Start the TCP server:
   - From the menu: MCP > TCP Server > Start Server
   - Or: Window > MCP Server > Start Server
4. Note the TCP server address (default: localhost:8080)

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
│   ├── unity_client_util.py     # Unity client utility functions
│   ├── unity_tcp_client.py      # High-level Unity TCP client
│   ├── mcp_server.py            # MCP server implementation
│   ├── dynamic_tool_invoker.py  # Dynamic tool invocation system
│   ├── dynamic_tools.py         # Dynamic tool manager
│   ├── connection_manager.py    # Connection lifecycle management
│   └── websocket_client.py      # Low-level TCP client (legacy name)
├── plugin/                      # Unity C# plugin
│   ├── Scripts/                 # Plugin source code
│   │   ├── Editor/              # Editor extensions
│   │   │   ├── Commands/        # Editor command implementations
│   │   │   ├── MCPWindow.cs     # Server control window
│   │   │   ├── MCPMenu.cs       # Unity menu integration
│   │   │   ├── MCPTcpServer.cs  # Primary TCP server implementation
│   │   │   ├── CommandExecutionMonitor.cs # Performance monitoring
│   │   │   ├── Models/          # Data models for Editor
│   │   │   └── Net/             # TCP communication implementation
│   │   └── YetAnotherUnityMcp.asmdef  # Assembly definition
│   └── README.md                # Plugin documentation
└── tests/                       # Test suite
```

## Architecture

### Unity TCP Server

The Unity plugin hosts a TCP server that listens for connections from MCP clients. This server:

- Manages client connections and message routing with a simple framing protocol
- Supports handshake and ping/pong for connection health monitoring
- Uses a dynamic registry of tools and resources with reflection-based attribute discovery
- Provides invokers for dynamically accessing resources and tools by name
- Supports container-based organization of tools and resources
- Executes commands sent by clients (e.g., running C# code, taking screenshots)
- Returns results back to clients
- Provides a UI for monitoring connections and debugging

For detailed information about the container-based approach, see the [MCP Container Documentation](plugin/MCP_CONTAINER_README.md).

### Python MCP Client

The Python client connects to the Unity TCP server and provides an MCP interface for AI tools. It:

- Translates MCP requests into framed TCP messages for Unity
- Handles connection retries and keep-alive pings
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
- `unity_logs` - Get logs from Unity with option to retrieve only new logs

## Communication Protocol

All communication between the Unity server and the Python client uses a **TCP socket** connection with a simple framing protocol, which allows persistent, low-latency bidirectional messaging. The connection is initiated by the Python client to the Unity server's TCP endpoint (e.g. `localhost:8080`).

The protocol uses a simple framing mechanism:
- Start marker (STX, 0x02)
- 4-byte length prefix
- JSON message content
- End marker (ETX, 0x03)

Every message is a JSON object containing at least a **command or response type**, a **unique ID** (to pair requests with responses), and a **parameters or result object**. The connection is maintained with periodic ping/pong messages. For more details on the communication protocol, see the [Technical Details](TECH_DETAILS.md) document.

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
# Use the MCP Server window in Unity for debugging
# Monitor connections and messages in real-time
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

For more details on architecture, implementation, and extensibility, see the [Technical Details](TECH_DETAILS.md) document.