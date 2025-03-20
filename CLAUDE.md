# CLAUDE.md - Development Guidelines

## Project Overview
YetAnotherUnityMcp is a system that bridges the Unity game engine with AI-driven tools using the Model Context Protocol (MCP):

- **Unity MCP Plugin (Server)**: A C# plugin integrated into the Unity Editor that hosts a WebSocket server
- **FastMCP Python Client**: A Python application that connects to Unity and implements the MCP interface
- **MCP Client (AI or External)**: The external entity (such as an AI assistant or testing script) that sends MCP requests

This architecture cleanly separates the game engine concerns from the AI logic, improving scalability and maintainability. The goal is to allow AI agents to inspect and control a running Unity scene in a structured, safe manner.

## Project Architecture
The project follows a server-client architecture with Unity hosting a WebSocket server and Python acting as the WebSocket client. The Python client then provides MCP protocol functionality for AI integration.

## File Structure
The code is organized with a clean separation of components:

### Unity Plugin (WebSocket Server)

#### Core WebSocket Server
- `plugin/Scripts/Editor/WebSocket/WebSocketServer.cs`: Core WebSocket server implementation with low-level socket handling
- `plugin/Scripts/Editor/MCPWebSocketServer.cs`: High-level MCP-specific server with command routing
- `plugin/Scripts/Editor/MCPWindow.cs`: Editor UI for controlling the server and executing local commands
- `plugin/Scripts/Editor/MCPMenu.cs`: Menu items for Unity Editor integration

#### Initialization and Configuration
- `plugin/Scripts/Editor/CommandExecutionMonitor.cs`: Performance monitoring for command execution
- `plugin/Scripts/Editor/Commands/MCPLocalCommandExecutor.cs`: Local command execution without WebSockets

#### Command Implementations
- `plugin/Scripts/Editor/Commands/ExecuteCodeCommand.cs`: Execute C# code in Unity
- `plugin/Scripts/Editor/Commands/TakeScreenshotCommand.cs`: Capture screenshots of the Unity scene
- `plugin/Scripts/Editor/Commands/ModifyObjectCommand.cs`: Modify Unity GameObject properties
- `plugin/Scripts/Editor/Commands/GetLogsCommand.cs`: Retrieve Unity console logs
- `plugin/Scripts/Editor/Commands/GetUnityInfoCommand.cs`: Get Unity environment information

#### Message Models
- `plugin/Scripts/Editor/WebSocket/WebSocketMessages.cs`: Message types and serialization for WebSocket communication
- `plugin/Scripts/Editor/Models/MCPModels.cs`: Data models for MCP protocol

### Python MCP Client

#### Server and Client Core
- `server/mcp_server.py`: Main MCP server entry point that initializes FastMCP with connection lifecycle management
- `server/unity_websocket_client.py`: High-level Unity client that provides direct access to Unity operations
- `server/websocket_client.py`: Low-level WebSocket client implementation

#### Utility Components
- `server/mcp/unity_client_util.py`: Utility functions for Unity WebSocket operations with error handling

#### MCP Tools (Actions AI Can Take)
- `server/mcp/tools/__init__.py`: Tool initialization and registration
- `server/mcp/tools/execute_code.py`: Execute C# code in Unity
- `server/mcp/tools/take_screenshot.py`: Capture screenshots and return as Image objects
- `server/mcp/tools/modify_object.py`: Change GameObject properties at runtime
- `server/mcp/tools/get_logs.py`: Get Unity console logs
- `server/mcp/tools/get_unity_info.py`: Get Unity environment information

#### MCP Resources (Data Sources for AI)
- `server/mcp/resources/__init__.py`: Resource initialization and registration
- `server/mcp/resources/unity_info.py`: Retrieves Unity environment information
- `server/mcp/resources/unity_logs.py`: Retrieves console logs
- `server/mcp/resources/unity_object.py`: Accesses GameObject information
- `server/mcp/resources/unity_scene.py`: Provides scene hierarchy information

#### Testing
- `tests/test_api.py`: API tests for the MCP functionality
- `tests/test_mcp_server.py`: Tests for the MCP server
- `tests/test_async_utils.py`: Tests for async utilities

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
Our MCP implementation consists of several key components:

### Server (Unity)
- **WebSocket Server**: Core communication layer for receiving commands
- **Command Processing**: Converting incoming messages to Unity actions
- **Response Serialization**: Converting Unity objects to serializable responses
- **Auto-Start**: Automatic WebSocket server initialization on Unity Editor start
- **Performance Monitoring**: Tracking command execution times and metrics

### Client (Python)
- **WebSocket Client**: Connection management to Unity WebSocket server
- **Tools**: Functions AI can call like `execute_code_in_unity`, `take_screenshot`
- **Resources**: Data sources like `unity://info`, `unity://scene` for AI to access
- **Connection Lifecycle**: Automatic connection management via FastMCP lifespan
- **Error Handling**: Standardized error handling across all operations
- **Context**: Passing context information through the FastMCP Context object

### Communication Protocol
- **Request-Response Pattern**: Each command has a unique ID for tracking
- **JSON Serialization**: All messages are serialized as JSON
- **Command Structure**: Commands include parameters and metadata
- **Response Structure**: Responses include results, errors, and timing information

## FastMCP Examples
```python
# Create an MCP server with connection to Unity
from fastmcp import FastMCP, Context, Image
from contextlib import asynccontextmanager
from server.unity_websocket_client import get_client

# Setup lifespan management for connection lifecycle
@asynccontextmanager
async def server_lifespan(server):
    # Initialize Unity client
    unity_client = get_client("ws://localhost:8080/")
    try:
        # Connect when server starts
        await unity_client.connect()
        yield {}  # Server runs during this time
    finally:
        # Disconnect when server stops
        await unity_client.disconnect()

# Create FastMCP instance with lifespan manager
mcp = FastMCP(
    "Unity MCP", 
    dependencies=["pillow", "websockets"],
    lifespan=server_lifespan
)

# Utility for standardized client access and error handling
async def execute_unity_operation(operation_name, operation, ctx):
    client = get_client()
    if not client.connected:
        await client.connect()
    try:
        ctx.info(f"Executing {operation_name}...")
        return await operation(client)
    except Exception as e:
        ctx.error(f"Error: {str(e)}")
        raise

# Add a tool that AI can call
@mcp.tool()
async def execute_code_in_unity(code: str, ctx: Context) -> str:
    """Execute C# code in Unity"""
    try:
        result = await execute_unity_operation(
            "code execution",
            lambda client: client.execute_code(code),
            ctx
        )
        return str(result)
    except Exception as e:
        return f"Error: {str(e)}"

# Add a resource AI can access
@mcp.resource("unity://scene/{scene_name}")
async def get_scene(scene_name: str, ctx: Context) -> dict:
    """Get information about a specific Unity scene"""
    # Use custom C# code to get scene info
    code = f"""
        // C# code to get scene info
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("{scene_name}");
        var result = new Dictionary<string, object>();
        // Fill result dictionary with scene info
        return JsonConvert.SerializeObject(result);
    """
    try:
        result = await execute_unity_operation(
            "scene retrieval",
            lambda client: client.execute_code(code),
            ctx
        )
        # Parse JSON result into dictionary
        import json
        return json.loads(result) if isinstance(result, str) else result
    except Exception as e:
        return {"error": str(e)}
```

## Running the System

### Setup and Installation

1. **Unity Plugin Installation**:
   - Copy the `plugin` directory into your Unity project's `Assets` directory
   - Wait for Unity to import all assets and compile scripts
   - The plugin will automatically register itself in the Unity Editor menu

2. **Python Client Installation**:
   ```bash
   cd server
   python -m pip install -e ".[dev]"
   ```

3. **Configure Claude Desktop (Optional)**:
   ```bash
   fastmcp install server/mcp_server.py --name "Unity Controller"
   ```

### Starting the System

1. **Start the Unity WebSocket Server**:
   - Open the Unity Editor with the plugin imported
   - The server can auto-start if enabled (default behavior)
   - Manual start: MCP > Server > Start Server
   - UI control panel: Window > MCP Server

2. **Run the Python MCP Client**:
   - Start the client with `python -m server.mcp_server`
   - For development: `fastmcp dev server/mcp_server.py`
   - The client will automatically attempt to connect to Unity

3. **Use from Claude Desktop**:
   - In Claude: `/mcp add unity-mcp`
   - Then use tools like: 
     ```
     /mcp run unity-mcp execute_code_in_unity --code "Debug.Log(\"Hello from Claude!\");"
     ```
   - Or get resources:
     ```
     /mcp get unity-mcp unity://info
     ```

### Communication Flow

1. AI (or user) makes a request to the Python MCP client
2. Python client translates this to a WebSocket message
3. Unity server receives the message and executes the requested command
4. Unity sends the result back to the Python client
5. Python client formats the result and returns it to the AI

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