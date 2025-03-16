"""
Tests for the Unity MCP server
"""
import pytest
from mcp.client import ClientSession
from mcp.client.stdio import stdio_client
from mcp.shared.types import StdioServerParameters
import asyncio
import os
import sys


@pytest.mark.asyncio
async def test_list_tools():
    """Test that the server correctly exposes tools"""
    # Set up server parameters pointing to our server implementation
    server_path = os.path.join(os.path.dirname(__file__), "..", "server", "mcp_server.py")
    server_params = StdioServerParameters(
        command=sys.executable,
        args=[server_path],
    )
    
    # Connect to the server via stdio
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            # Initialize the connection
            await session.initialize()
            
            # List available tools
            tools = await session.list_tools()
            
            # Verify expected tools are available
            tool_names = [tool.name for tool in tools]
            assert "execute_code" in tool_names
            assert "screen_shot_editor" in tool_names
            assert "modify_object" in tool_names


@pytest.mark.asyncio
async def test_list_resources():
    """Test that the server correctly exposes resources"""
    # Set up server parameters pointing to our server implementation
    server_path = os.path.join(os.path.dirname(__file__), "..", "server", "mcp_server.py")
    server_params = StdioServerParameters(
        command=sys.executable,
        args=[server_path],
    )
    
    # Connect to the server via stdio
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            # Initialize the connection
            await session.initialize()
            
            # List available resources
            resources = await session.list_resources()
            
            # Verify expected resources are available
            resource_urls = [resource.url for resource in resources]
            assert "unity://info" in resource_urls
            assert "unity://logs" in resource_urls
            
            # Check for parameterized resources (may not be exact match)
            assert any("unity://scene/" in url for url in resource_urls)
            assert any("unity://object/" in url for url in resource_urls)


@pytest.mark.asyncio
async def test_read_resource():
    """Test reading a resource from the server"""
    # Set up server parameters pointing to our server implementation
    server_path = os.path.join(os.path.dirname(__file__), "..", "server", "mcp_server.py")
    server_params = StdioServerParameters(
        command=sys.executable,
        args=[server_path],
    )
    
    # Connect to the server via stdio
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            # Initialize the connection
            await session.initialize()
            
            # Read a resource
            content, mime_type = await session.read_resource("unity://info")
            
            # Verify content contains expected information
            assert isinstance(content, str)
            assert "Unity Version:" in content
            assert "Platform:" in content
            assert "Project:" in content
            
            # Check MIME type
            assert mime_type == "text/plain"