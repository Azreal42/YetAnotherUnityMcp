# CLAUDE.md - Development Guidelines

## Project Overview
YetAnotherUnityMcp is a system that bridges the Unity game engine with AI-driven tools using the Model Context Protocol (MCP):

- **Unity MCP Plugin (Server)**: A C# plugin integrated into the Unity Editor that hosts a TCP server
- **FastMCP Python Client**: A Python application that connects to Unity and implements the MCP interface
- **MCP Client (AI or External)**: The external entity (such as an AI assistant or testing script) that sends MCP requests

This architecture cleanly separates the game engine concerns from the AI logic, improving scalability and maintainability. The goal is to allow AI agents to inspect and control a running Unity scene in a structured, safe manner.

## Important Rules for Claude
1. **Don't run code directly**: The Python venv is Windows-based while Claude runs on WSL - running tests directly will fail.
2. **Update documentation**: Every time a new feature is implemented, update the relevant documentation in this file, README.md, and TECH_DETAILS.md to reflect the changes.
3. **Keep code and documentation in sync**: Ensure schema definitions, function signatures, and documentation stay consistent.

## Project Architecture
The project follows a server-client architecture with Unity hosting a TCP server and Python acting as the TCP client. The Python client then provides MCP protocol functionality for AI integration.

## File Structure
The code is organized with a clean separation of components:

### Unity Plugin (TCP Server)

#### Core TCP Server
- `plugin/Scripts/Editor/Net/TcpServer.cs`: Core TCP server implementation with low-level socket handling
- `plugin/Scripts/Editor/MCPTcpServer.cs`: High-level MCP-specific server with command routing
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
- `plugin/Scripts/Editor/Commands/GetSchemaCommand.cs`: Retrieve schema information

#### Message Models
- `plugin/Scripts/Editor/Net/TcpMessages.cs`: Message types and serialization for TCP communication
- `plugin/Scripts/Editor/Models/MCPModels.cs`: Data models for MCP protocol

### Python MCP Client

#### Server and Client Core
- `server/mcp_server.py`: Main MCP server entry point that initializes FastMCP with connection lifecycle management
- `server/unity_tcp_client.py`: High-level Unity client for direct communication with Unity
- `server/websocket_client.py`: Low-level TCP client implementation (legacy name)

#### Utility and Connection Components
- `server/unity_client_util.py`: Utility functions for Unity TCP operations with error handling
- `server/dynamic_tool_invoker.py`: Dynamic tool invocation system for FastMCP
- `server/dynamic_tools.py`: Tool and resource manager for FastMCP integration
- `server/connection_manager.py`: Connection lifecycle management utilities

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

## MCP Implementation
Our MCP implementation consists of several key components:

### Server (Unity)
- **TCP Server**: Core communication layer for receiving commands
- **Command Processing**: Converting incoming messages to Unity actions
- **Response Serialization**: Converting Unity objects to serializable responses
- **Auto-Start**: Automatic TCP server initialization on Unity Editor start
- **Performance Monitoring**: Tracking command execution times and metrics

### Client (Python)
- **TCP Client**: Connection management to Unity TCP server
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

## Commands
```bash
# Server (Python)
cd server
python -m pip install -e ".[dev]"     # Install with dev dependencies
python -m uvicorn main:app --reload   # Run the traditional server with auto-reload
python -m pytest                      # Run all tests
python -m pytest test_file.py         # Run a specific test file
python -m pytest test_file.py::test_func # Run a single test
python -m black .                     # Format Python code
python -m flake8                      # Lint Python code
python -m mypy .                      # Type-check Python code

# MCP Integration 
source $HOME/.local/bin/env           # Set up uv environment
python -m server.mcp_server           # Run MCP server from command line
python -m server.fastmcp_example      # Run simplified FastMCP example

# MCP Development
fastmcp dev server/mcp_server.py           # Run with MCP Inspector UI
fastmcp dev server/fastmcp_example.py      # Test simplified example

# Claude Desktop Installation
fastmcp install server/mcp_server.py       # Install in Claude Desktop
fastmcp install server/fastmcp_example.py --name "Unity Simple" # Install simplified version
fastmcp install server/mcp_server.py -e UNITY_PATH=/path/to/unity # With environment variables

# Plugin (C#)
# Build and install Unity plugin via Unity Editor
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

### Communication Flow

1. AI (or user) makes a request to the Python MCP client
2. Python client translates this to a TCP message with framing protocol
3. Unity server receives the message and executes the requested command
4. Unity sends the result back to the Python client via TCP
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