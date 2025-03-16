# YetAnotherUnityMcp

A Unity Master Control Protocol (MCP) implementation that allows AI agents to control and interact with Unity.

## Overview

**YetAnotherUnityMcp** is a system that bridges the Unity game engine with AI-driven tools using the **Model Context Protocol (MCP)**. It consists of a Unity **.NET/C# plugin** acting as the MCP client, and a **Python MCP server** (built with FastMCP) that handles requests from AI agents. Communication between Unity and the server is done via **WebSockets**, enabling real-time, bidirectional exchange of JSON messages and image data.

This architecture cleanly separates the game engine concerns from the AI logic, improving scalability and maintainability. The goal is to allow AI agents (e.g. an LLM-based assistant) to **inspect and control a running Unity scene** in a structured, safe manner.

Key components include:

1. **Unity MCP Plugin (Client)** – A C# plugin integrated into the Unity Editor that connects to the MCP server via WebSockets
2. **FastMCP Python Server** – A Python application that implements the MCP interface for Unity
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
- Fallback to local command execution when server is unavailable

## Getting Started

### Server Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/YetAnotherUnityMcp.git
cd YetAnotherUnityMcp

# Create and activate a virtual environment using uv
uv venv -p 3.11
source .venv/bin/activate  # On Windows: .venv\Scripts\activate

# Install the server with development dependencies
uv pip install -e ".[dev]"

# Run the MCP server for AI integration
python -m server.mcp_server

# Alternatively, run the WebSocket-based server
python -m server.websocket_mcp_server
```

### MCP Integration

```bash
# Install FastMCP and tools
uv pip install fastmcp

# Run the server with MCP inspector for debugging
fastmcp dev server/mcp_server.py

# Install in Claude Desktop
fastmcp install server/mcp_server.py --name "Unity Controller"
```

### Unity Plugin Setup

1. Open your Unity project (2020.3 or later)
2. Import the YetAnotherUnityMcp plugin using one of these methods:
   - Copy the `plugin/Scripts` folder to your Unity project's Assets directory
   - Create a Unity package and import it
   - Create a symbolic link for development (Windows PowerShell example):
     ```powershell
     New-Item -ItemType SymbolicLink -Target "D:\Dev\YetAnotherUnityMcp\plugin" -Path "C:\Users\azrea\My project\Assets\Plugins\YetAnotherUnityMcp"
     ```
3. Add the MCPClient to your scene
   - Create an empty GameObject
   - Add the MCPClient component
   - Configure the server URL (WebSocket URL, e.g., ws://localhost:8000/ws)
4. Use the MCP Editor Window
   - Open: Window > MCP Client (standard HTTP-based client)
   - Or: Window > WebSocket MCP Client (WebSocket-based client)
   - Connect to the server
   - Test the various MCP features

## Project Structure

```
YetAnotherUnityMcp/
├── server/                      # Python MCP server
│   ├── api/                     # API endpoints
│   ├── models/                  # Data models
│   ├── main.py                  # Traditional API server
│   ├── mcp_server.py            # MCP server implementation
│   ├── websocket_mcp_server.py  # WebSocket-based MCP server
│   └── fastmcp_example.py       # Simple FastMCP example
├── plugin/                      # Unity C# plugin
│   ├── Scripts/                 # Plugin source code
│   │   ├── Editor/              # Editor extensions
│   │   │   ├── Commands/        # Editor command implementations
│   │   │   ├── Models/          # Data models for Editor
│   │   │   └── WebSocket/       # WebSocket implementation
│   │   └── YetAnotherUnityMcp.asmdef  # Assembly definition
│   └── README.md                # Plugin documentation
└── tests/                       # Test suite
```

## MCP Resources and Tools

### Resources

- `unity://info` - Basic information about the Unity environment
- `unity://logs` - Editor logs for debugging
- `unity://scene/{scene_name}` - Information about a specific scene
- `unity://object/{object_id}` - Details about a specific GameObject

### Tools

- `execute_code` - Run C# code in the Unity Editor
- `screen_shot_editor` - Take screenshots of the Unity Editor
- `modify_object` - Change properties of Unity GameObjects

### Prompts

- `create_object` - Template for creating new GameObjects
- `debug_error` - Template for diagnosing Unity errors

## Communication Protocol

All communication between the Unity plugin and the Python server uses a **WebSocket** connection, which allows persistent, low-latency bidirectional messaging. The connection is initiated by the Unity client to the server's WebSocket endpoint (e.g. `ws://localhost:8000/ws`).

Every message is a JSON object containing at least a **message type**, a **unique ID** (to pair requests with responses), and a **payload**. For more details on the communication protocol, see the [Technical Details](TECH_DETAILS.md) document.

## Development

```bash
# Server development
python -m pytest                    # Run tests
python -m black .                   # Format code
python -m flake8                    # Lint code
python -m mypy .                    # Type check
python -m pytest tests/test_mcp_server.py  # Test MCP integration

# MCP Development
fastmcp dev server/mcp_server.py           # Run with MCP Inspector UI
fastmcp dev server/websocket_mcp_server.py # Run WebSocket server with Inspector

# Plugin development
# The Unity plugin can be developed in Unity Editor
# Use the editor window for testing
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

For more details on architecture, implementation, and extensibility, see the [Technical Details](TECH_DETAILS.md) document.
