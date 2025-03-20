# CLAUDE.md - Development Guidelines

## Project Overview
YetAnotherUnityMcp is a system that bridges the Unity game engine with AI-driven tools using the Model Context Protocol (MCP):

- **Unity MCP Plugin (Server)**: A C# plugin integrated into the Unity Editor that hosts a WebSocket server
- **FastMCP Python Client**: A Python application that connects to Unity and implements the MCP interface
- **MCP Client (AI or External)**: The external entity (such as an AI assistant or testing script) that sends MCP requests

This architecture cleanly separates the game engine concerns from the AI logic, improving scalability and maintainability. The goal is to allow AI agents to inspect and control a running Unity scene in a structured, safe manner.

## Code Structure
The code is organized with a clean separation of components:

- **Unity (Server)**:
  - `plugin/Scripts/Editor/WebSocket/WebSocketServer.cs`: Core WebSocket server implementation
  - `plugin/Scripts/Editor/WebSocket/MCPWebSocketServer.cs`: MCP-specific server functionality
  - `plugin/Scripts/Editor/WebSocket/WebSocketServerMCPWindow.cs`: Server management UI
  - `plugin/Scripts/Editor/Commands/`: Command implementations (ExecuteCode, TakeScreenshot, etc.)
  - `plugin/Scripts/Editor/WebSocket/MCPLocalCommandExecutor.cs`: Local command execution without WebSockets

- **Python (Client)**:
  - `server/mcp_server.py`: Main client entry point that initializes FastMCP
  - `server/mcp_client.py`: High-level Unity client
  - `server/websocket_client.py`: Low-level WebSocket client
  - `server/mcp/tools/`: MCP tool implementations
  - `server/mcp/resources/`: MCP resource implementations

## Model Context Protocol (MCP)
We use FastMCP for MCP integration, which provides:
- Simple decorator-based API for creating MCP servers
- Support for Resources, Tools, and Prompts
- Standard MCP primitives for AI interaction with Unity
- Integration with Claude Desktop via `fastmcp` CLI tool

## Commands
```bash
# Client (Python)
cd server
python -m pip install -e ".[dev]"     # Install with dev dependencies
python -m server.mcp_server           # Run the MCP client with STDIO transport
python -m pytest                      # Run all tests
python -m pytest test_file.py         # Run a specific test file
python -m pytest test_file.py::test_func # Run a single test
python -m black .                     # Format Python code
python -m flake8                      # Lint Python code
python -m mypy .                      # Type-check Python code

# MCP Integration 
source $HOME/.local/bin/env           # Set up uv environment
python -m server.mcp_server           # Run MCP client from command line

# MCP Development
fastmcp dev server/mcp_server.py      # Run with MCP Inspector UI

# Claude Desktop Installation
fastmcp install server/mcp_server.py       # Install in Claude Desktop
fastmcp install server/mcp_server.py --name "Unity Controller" # Install with custom name
fastmcp install server/mcp_server.py -e UNITY_URL=ws://localhost:8080/ # With environment variables

# Plugin (C#)
# Build and install Unity plugin via Unity Editor
# Start the WebSocket server from MCP > WebSocket Server > Start Server
```

## MCP Implementation
Our client implements the MCP specification with:
- Tools: `execute_code_in_unity`, `unity_screenshot`, etc. as functions AI can call
- Resources: Scene hierarchy, GameObject properties as data sources
- Prompts: Pre-defined templates for common Unity operations
- Context: Passing context information through the Context object

## FastMCP Examples
```python
# Create an MCP server with connection to Unity
from fastmcp import FastMCP, Context, Image
from server.mcp_client import get_client

mcp = FastMCP("Unity MCP", dependencies=["pillow", "websockets"])
unity_client = get_client("ws://localhost:8080/")

# Add a tool that AI can call
@mcp.tool()
async def execute_code_in_unity(code: str, ctx: Context) -> str:
    """Execute C# code in Unity"""
    # Connect to Unity if not connected
    if not unity_client.connected:
        await unity_client.connect()
    # Execute the code through the client
    result = await unity_client.execute_code(code)
    return result

# Add a resource AI can access
@mcp.resource("unity://scene/{scene_name}")
async def get_scene(scene_name: str) -> str:
    """Get information about a specific Unity scene"""
    # Connect to Unity if needed
    if not unity_client.connected:
        await unity_client.connect()
    # Use custom C# code to get scene info
    code = f"""
        // C# code to get scene info
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("{scene_name}");
        var result = new Dictionary<string, object>();
        // Fill result dictionary with scene info
        return JsonConvert.SerializeObject(result);
    """
    result = await unity_client.execute_code(code)
    return result
```

## Running the System

1. **Start the Unity WebSocket Server**:
   - Open the Unity Editor with the plugin imported
   - Start the server from MCP > WebSocket Server > Start Server
   - You can also use Window > WebSocket MCP Server to start the server

2. **Run the Python MCP Client**:
   - Start the client with `python -m server.mcp_server`
   - Or use FastMCP: `fastmcp dev server/mcp_server.py`

3. **Use from Claude Desktop**:
   - Install in Claude Desktop with `fastmcp install server/mcp_server.py`
   - Use from Claude: `/mcp add unity-mcp`

## Code Style Guidelines
- **Python**:
  - Use PEP 8 style guide with Black formatter (line length 88)
  - Type hints required for all function parameters and returns
  - Use snake_case for variables and functions
  - Import order: standard library, third-party, local modules
  - Error handling: use specific exceptions, log errors properly
  - All MCP handlers must be properly typed and documented

- **C# (Unity)**:
  - Follow Unity's C# style guide
  - Use PascalCase for classes, methods, and properties
  - Use camelCase for variables and private fields
  - Implement proper serialization for MCP communication
  - Use descriptive naming and add comments for complex logic