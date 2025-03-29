"""
Tests for the Unity MCP server with the dynamic tools structure.

These tests verify the functionality of the MCP server components using
dependency injection instead of singletons.
"""
import pytest
import asyncio
import unittest.mock as mock
from unittest.mock import AsyncMock, MagicMock, patch
import json
import sys

from mcp.server.fastmcp import FastMCP, Context
from server.mcp_server import mcp, register_dynamic_tools
from server.unity_tcp_client import UnityTcpClient
from server.connection_manager import UnityConnectionManager
from server.dynamic_tools import DynamicToolManager
from server.dynamic_tool_invoker import DynamicToolInvoker


def test_mcp_instance():
    """Test that the MCP instance is properly created and configured"""
    # Verify MCP instance configuration
    assert mcp.name == "Unity MCP WebSocket Client"
    assert mcp.settings.lifespan is not None


@pytest.mark.asyncio
async def test_dynamic_tools_registration():
    """Test the registration of dynamic tools from schema"""
    # Create a mock client
    mock_client = AsyncMock(spec=UnityTcpClient)
    mock_client.connected = True
    mock_client.get_schema = AsyncMock(return_value={
        "tools": [
            {
                "name": "execute_code",
                "description": "Execute C# code in Unity"
            }
        ],
        "resources": [
            {
                "name": "unity_info",
                "description": "Get Unity information",
                "uri": "unity://info"
            }
        ]
    })
    
    # Create tool manager with our MCP instance and mock client
    connection_manager = UnityConnectionManager(mock_client)
    tool_manager = DynamicToolManager(mcp, connection_manager)
    
    # Register dynamic tools
    await register_dynamic_tools(tool_manager)
    
    # Verify tools and resources were registered
    assert "execute_code" in tool_manager.registered_tools
    assert "unity_info" in tool_manager.registered_resources
    
    # Verify the schema was retrieved
    mock_client.get_schema.assert_called_once()


@pytest.mark.asyncio
async def test_dynamic_tool_invocation():
    """Test invoking a dynamic tool through the DynamicToolInvoker"""
    # Create mock client
    mock_client = AsyncMock(spec=UnityTcpClient)
    mock_client.connected = True
    mock_client.send_command = AsyncMock(return_value={
        "result": {
            "content": [
                {
                    "type": "text",
                    "text": "Code executed successfully"
                }
            ],
            "isError": False
        }
    })
    
    # Create mock context
    ctx = AsyncMock(spec=Context)
    ctx.info = AsyncMock()
    ctx.error = AsyncMock()
    
    # Create connection manager and invoker
    connection_manager = UnityConnectionManager(mock_client)
    # Make execute_with_reconnect actually await the coroutine and return the result
    async def execute_with_reconnect_side_effect(func):
        return await func()
    connection_manager.execute_with_reconnect = AsyncMock(side_effect=execute_with_reconnect_side_effect)
    tool_invoker = DynamicToolInvoker(connection_manager)
    
    # Invoke a tool
    code = "Debug.Log(\"Test\");"
    result = await tool_invoker.invoke_dynamic_tool("execute_code", {"code": code}, ctx)
    
    # Verify the result
    assert result is not None
    assert "result" in result
    assert "content" in result["result"]
    assert not result["result"]["isError"]
    assert "Code executed successfully" in result["result"]["content"][0]["text"]
    
    # Verify the client was called with the right parameters
    mock_client.send_command.assert_called_once_with("execute_code", {"code": code})


@pytest.mark.asyncio
async def test_dynamic_resource_access():
    """Test accessing a dynamic resource through the DynamicToolInvoker"""
    # Create mock client
    mock_client = AsyncMock(spec=UnityTcpClient)
    mock_client.connected = True
    mock_client.send_command = AsyncMock(return_value={
        "result": {
            "content": [
                {
                    "type": "text",
                    "text": json.dumps({
                        "unity_version": "2022.3.1f1",
                        "platform": "Windows",
                        "project_name": "TestProject"
                    })
                }
            ],
            "isError": False
        }
    })
    
    # Create mock context
    ctx = AsyncMock(spec=Context)
    ctx.info = AsyncMock()
    ctx.error = AsyncMock()
    
    # Create connection manager and invoker
    connection_manager = UnityConnectionManager(mock_client)
    # Make execute_with_reconnect actually await the coroutine and return the result
    async def execute_with_reconnect_side_effect(func):
        return await func()
    connection_manager.execute_with_reconnect = AsyncMock(side_effect=execute_with_reconnect_side_effect)
    tool_invoker = DynamicToolInvoker(connection_manager)
    
    # Access a resource
    result = await tool_invoker.invoke_dynamic_resource("unity_info", {}, ctx)
    
    # Verify the result
    assert result is not None
    assert "result" in result
    assert "content" in result["result"]
    assert not result["result"]["isError"]
    
    # Check the JSON structure
    content_text = result["result"]["content"][0]["text"]
    info_data = json.loads(content_text)
    assert info_data["unity_version"] == "2022.3.1f1"
    assert info_data["platform"] == "Windows"
    assert info_data["project_name"] == "TestProject"
    
    # Verify the client was called with the right parameters
    mock_client.send_command.assert_called_once_with("access_resource", {
        "resource_name": "unity_info",
        "parameters": {}
    })


# Helper to configure asyncio for Windows
def pytest_configure(config):
    """Configure pytest for async tests"""
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

if __name__ == "__main__":
    # Run the tests
    pytest.main(["-xvs", __file__])
