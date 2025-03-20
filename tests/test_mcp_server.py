"""
Tests for the Unity MCP server with the new modular structure
"""
import pytest
import unittest.mock as mock
import os
import sys
import json
from fastapi.testclient import TestClient
from fastmcp import FastMCP, Context, Image
from server.mcp_server import app, mcp
from server.mcp.tools.execute_code import execute_code_handler
from server.mcp.tools.screen_shot import screen_shot_editor_handler
from server.mcp.tools.modify_object import modify_object_handler
from server.mcp.resources.unity_info import get_unity_info_handler


@pytest.fixture
def client():
    """Create a test client for the FastAPI app"""
    return TestClient(app)


@pytest.fixture
def mock_mcp():
    """Mock FastMCP for testing"""
    with mock.patch("server.mcp_server.mcp") as mock_mcp:
        yield mock_mcp


def test_mcp_instance():
    """Test that MCP is properly initialized"""
    # Check that the MCP instance is properly configured
    assert mcp.name == "Unity MCP WebSocket"
    assert "websockets" in mcp.dependencies
    assert "pillow" in mcp.dependencies


def test_mcp_tools_registered():
    """Test that all expected tools are registered with MCP"""
    # Get all registered tools from MCP
    tools = mcp._tools  # This accesses internal state, might need adjustment
    
    # Check that our tools are registered
    tool_names = [tool.name for tool in tools]
    assert "execute_code_handler" in tool_names
    assert "screen_shot_editor_handler" in tool_names
    assert "modify_object_handler" in tool_names


def test_mcp_resources_registered():
    """Test that all expected resources are registered with MCP"""
    # Get all registered resources from MCP
    resources = mcp._resources  # This accesses internal state, might need adjustment
    
    # Get all resource URLs
    resource_urls = [resource.url_pattern for resource in resources]
    
    # Check expected resources
    assert "unity://info" in resource_urls
    assert "unity://logs/{max_logs}" in resource_urls
    assert "unity://scene/{scene_name}" in resource_urls
    assert "unity://object/{object_id}" in resource_urls


def test_execute_code_handler():
    """Test the execute_code handler function with mocks"""
    ctx = mock.MagicMock(spec=Context)
    
    # Mock AsyncExecutor.run_in_thread_or_loop to return a known value
    with mock.patch("server.mcp.tools.execute_code.AsyncExecutor.run_in_thread_or_loop") as mock_run:
        # Set up the mock to return a specific value
        mock_run.return_value = "Code executed successfully"
        
        # Call the handler
        result = execute_code_handler("print('test')", ctx)
        
        # Verify the result
        assert result == "Code executed successfully"
        mock_run.assert_called_once()


def test_screen_shot_handler():
    """Test the screen_shot_editor handler function with mocks"""
    ctx = mock.MagicMock(spec=Context)
    
    # Mock AsyncExecutor.run_in_thread_or_loop to return a known value
    with mock.patch("server.mcp.tools.screen_shot.AsyncExecutor.run_in_thread_or_loop") as mock_run:
        # Create a dummy image result
        dummy_image = Image(data=b"test_image_data", format="png")
        mock_run.return_value = dummy_image
        
        # Call the handler
        result = screen_shot_editor_handler("test.png", 800, 600, ctx)
        
        # Verify the result
        assert result is dummy_image
        assert result.format == "png"
        mock_run.assert_called_once()


def test_unity_info_handler():
    """Test the get_unity_info handler function with mocks"""
    # Mock AsyncExecutor.run_in_thread_or_loop to return a known value
    with mock.patch("server.mcp.resources.unity_info.AsyncExecutor.run_in_thread_or_loop") as mock_run:
        # Set up the mock to return a specific dictionary
        mock_run.return_value = {
            "unity_version": "2022.3.1f1",
            "platform": "Windows",
            "project_name": "TestProject"
        }
        
        # Call the handler
        result = get_unity_info_handler()
        
        # Verify the result
        assert result["unity_version"] == "2022.3.1f1"
        assert result["platform"] == "Windows"
        assert result["project_name"] == "TestProject"
        mock_run.assert_called_once()


@pytest.mark.asyncio
async def test_websocket_connection(client):
    """Test WebSocket connection and basic message handling"""
    # This is a bit more complex as we need to mock the WebSocket connection
    # Note: The TestClient doesn't fully support WebSockets so we're mocking here
    
    with mock.patch("server.websocket_handler.websocket_endpoint") as mock_endpoint:
        # Set up the mock to perform basic functionality
        mock_endpoint.return_value = None
        
        # Create a WebSocket connection (this will call our mocked endpoint)
        with client.websocket_connect("/ws") as websocket:
            # In a real test, we would send and receive messages here
            # Since we've mocked the endpoint, we'll just verify it was called
            mock_endpoint.assert_called_once()
            
            # The first argument should be the WebSocket object
            assert mock_endpoint.call_args[0][0] is not None