# YetAnotherUnityMcp

A Unity Master Control Protocol (MCP) implementation that allows AI agents to control and interact with Unity.

## Overview

This project provides:

1. **MCP Server**: A Python server implementing the Model Context Protocol for AI-Unity communication
2. **Unity Plugin**: A C# plugin that integrates with Unity to execute commands from the server

## Features

- Execute C# code in Unity from AI agents
- Query Unity Editor state through MCP resources
- Capture screenshots with AI-driven parameters
- Get logs and debug information from Unity
- Modify GameObject properties with AI assistance
- List and navigate GameObject hierarchies
- Provide contextual templates through MCP prompts

## What is MCP?

The [Model Context Protocol (MCP)](https://modelcontextprotocol.io) is a standardized way for AI models to interact with applications. It separates the concerns of providing context from the LLM interaction itself, allowing for:

- **Resources**: Providing data to LLMs (like Unity scene hierarchies)
- **Tools**: Allowing LLMs to take actions (like executing code in Unity)
- **Prompts**: Defining interaction templates (like how to create GameObjects)

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

# Run the traditional FastAPI server
python -m uvicorn server.main:app --reload

# Run the MCP server for AI integration
python -m server.mcp_server
```

### MCP Integration

```bash
# Install MCP tools (if you have the MCP SDK locally)
uv pip install -e "/path/to/mcp-sdk"

# Run the server with MCP inspector for debugging
mcp dev server/mcp_server.py

# Install in Claude Desktop
mcp install server/mcp_server.py --name "Unity Controller"
```

### Plugin Setup

Instructions for integrating the Unity plugin will be added soon.

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

## Development

```bash
# Run tests
python -m pytest

# Format code
python -m black .

# Lint code
python -m flake8

# Type check
python -m mypy .

# Test MCP integration
python -m pytest tests/test_mcp_server.py
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
