# CLAUDE.md - Development Guidelines

## Project Overview
YetAnotherUnityMcp implements the Model Context Protocol (MCP) for Unity:
- Server: Python-based API that communicates with AI models and Unity
- Plugin: C#-based Unity integration that allows AI to control and inspect Unity

## Model Context Protocol (MCP)
We use FastMCP for MCP integration, which provides:
- Simple decorator-based API for creating MCP servers
- Support for Resources, Tools, and Prompts
- Standard MCP primitives for AI interaction with Unity
- Integration with Claude Desktop via `fastmcp` CLI tool

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

## MCP Implementation
Our server implements the MCP specification with:
- Tools: `execute_code`, `modify_object`, etc. as functions exposed to AI
- Resources: Scene hierarchy, GameObject properties as data sources
- Prompts: Pre-defined templates for common Unity operations
- Context: Passing context information through the Context object

## FastMCP Examples
```python
# Create an MCP server
from fastmcp import FastMCP, Context, Image
mcp = FastMCP("Unity MCP", dependencies=["pillow", "numpy"])

# Add a tool that AI can call
@mcp.tool()
def execute_code(code: str, ctx: Context) -> str:
    """Execute C# code in Unity"""
    ctx.info(f"Executing code in Unity...")
    # Implementation here
    return result

# Add a resource AI can access
@mcp.resource("unity://scene/{scene_name}")
def get_scene(scene_name: str) -> str:
    """Get information about a specific Unity scene"""
    # Implementation here
    return scene_data

# Return images from tools
@mcp.tool()
def take_screenshot(width: int = 1920, height: int = 1080) -> Image:
    """Take a screenshot of the Unity Editor"""
    # Implementation here
    return Image(data=image_data, format="png")

# Add a prompt template
@mcp.prompt()
def create_object() -> str:
    """Template for creating objects"""
    return "Create object with {properties}"
```

## Running the Server

1. **Development Mode**:
   - Use `fastmcp dev` for testing with the MCP Inspector UI
   - Allows interactive testing of tools and resources
   - Provides detailed logs and error messages

2. **Claude Desktop Integration**:
   - Use `fastmcp install` to add the server to Claude Desktop
   - Server runs in an isolated environment with its dependencies
   - Can specify environment variables with `-e` or from a file with `-f`

3. **Direct Execution**:
   - Use Python directly: `python server/mcp_server.py`
   - Or use the FastMCP CLI: `fastmcp run server/mcp_server.py`
   - You are responsible for ensuring all dependencies are available

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