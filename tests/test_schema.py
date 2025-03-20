"""
Tests for the schema retrieval system
"""

import pytest
import unittest.mock as mock
import json
import asyncio
from fastapi import WebSocket
from fastmcp import Context

from server.mcp.tools.get_schema import get_unity_schema
from server.unity_websocket_client import UnityWebSocketClient
from server.websocket_handler import websocket_endpoint
from server.connection_manager import ConnectionManager


@pytest.fixture
def mock_unity_client():
    """Mock the UnityWebSocketClient"""
    client = mock.MagicMock(spec=UnityWebSocketClient)
    client.connected = True
    client.get_schema = mock.AsyncMock()
    
    # Create a sample schema response
    schema = {
        "tools": [
            {
                "name": "execute_code",
                "description": "Execute C# code in Unity",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "code": {
                            "type": "string",
                            "description": "C# code to execute",
                            "required": True
                        }
                    },
                    "required": ["code"]
                }
            },
            {
                "name": "get_schema",
                "description": "Get information about available tools and resources",
                "inputSchema": {
                    "type": "object",
                    "properties": {},
                    "required": []
                }
            }
        ],
        "resources": [
            {
                "name": "unity_info",
                "description": "Get information about the Unity environment",
                "urlPattern": "unity://info"
            },
            {
                "name": "unity_schema",
                "description": "Get information about available tools and resources",
                "urlPattern": "unity://schema"
            }
        ]
    }
    
    # Configure the mock to return the sample schema
    client.get_schema.return_value = schema
    
    return client


@pytest.fixture
def mock_manager():
    """Mock ConnectionManager for testing"""
    manager = mock.MagicMock(spec=ConnectionManager)
    manager.active_connections = []
    # Mock the methods we'll use
    manager.connect = mock.AsyncMock()
    manager.send_message = mock.AsyncMock()
    manager.disconnect = mock.MagicMock()
    return manager


@pytest.fixture
def pending_requests():
    """Create a dictionary for pending requests"""
    return {}


@pytest.mark.asyncio
async def test_get_unity_schema(mock_unity_client):
    """Test the get_unity_schema function in the MCP tool"""
    # Create a mock context
    ctx = mock.MagicMock(spec=Context)
    
    # Patch the get_client function to return our mock client
    with mock.patch("server.mcp.unity_client_util.get_client", return_value=mock_unity_client):
        # Call the function
        result = await get_unity_schema(ctx)
        
        # Verify the client method was called
        mock_unity_client.get_schema.assert_called_once()
        
        # Verify the result is correct
        assert "tools" in result
        assert "resources" in result
        assert len(result["tools"]) == 2
        assert len(result["resources"]) == 2


@pytest.mark.asyncio
async def test_ws_get_schema_command(mock_manager, pending_requests):
    """Test the get_schema command over WebSocket"""
    # Create a websocket that only sends a get_schema command
    websocket = mock.MagicMock(spec=WebSocket)
    websocket.receive_text = mock.AsyncMock(side_effect=[
        json.dumps({
            "id": "req-1",
            "command": "get_schema",
            "parameters": {}
        }),
        WebSocket.Disconnect()
    ])
    
    # Mock the get_unity_schema function
    with mock.patch("server.mcp.tools.get_schema.get_unity_schema") as mock_get_schema:
        # Set up the mock to return a sample schema
        mock_get_schema.return_value = {
            "tools": [
                {
                    "name": "execute_code",
                    "description": "Execute C# code in Unity",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "code": {
                                "type": "string",
                                "description": "C# code to execute",
                                "required": True
                            }
                        },
                        "required": ["code"]
                    }
                }
            ],
            "resources": [
                {
                    "name": "unity_info",
                    "description": "Get information about the Unity environment",
                    "urlPattern": "unity://info"
                }
            ]
        }
        
        # Call the websocket endpoint
        with pytest.raises(WebSocket.Disconnect):
            await websocket_endpoint(websocket, mock_manager, pending_requests)
        
        # Verify get_schema was called
        mock_get_schema.assert_called_once()
        
        # Verify the response was sent
        mock_manager.send_message.assert_called_once()
        # The response should be JSON with a success status
        response_json = mock_manager.send_message.call_args[0][1]
        response = json.loads(response_json)
        assert response["status"] == "success"
        assert "tools" in response["result"]
        assert "resources" in response["result"]


@pytest.mark.asyncio
async def test_unity_client_get_schema():
    """Test the get_schema method in UnityWebSocketClient"""
    # Create an instance of UnityWebSocketClient
    client = UnityWebSocketClient("ws://localhost:8080/")
    
    # Mock the send_command method
    client.ws_client.send_command = mock.AsyncMock()
    
    # Set up the mock to return a sample schema
    sample_schema = {
        "tools": [{"name": "tool1", "description": "Test tool"}],
        "resources": [{"name": "resource1", "description": "Test resource"}]
    }
    client.ws_client.send_command.return_value = sample_schema
    
    # Call the method
    result = await client.get_schema()
    
    # Verify send_command was called with the right command
    client.ws_client.send_command.assert_called_once_with("get_schema")
    
    # Verify the result is correct
    assert result == sample_schema


@pytest.mark.asyncio
async def test_connection_error_handling():
    """Test error handling when Unity is not connected"""
    # Create a mock context
    ctx = mock.MagicMock(spec=Context)
    
    # Create a mock client that is not connected
    client = mock.MagicMock(spec=UnityWebSocketClient)
    client.connected = False
    
    # Mock connect to fail
    client.connect = mock.AsyncMock(return_value=False)
    
    # Patch the get_client function to return our mock client
    with mock.patch("server.mcp.unity_client_util.get_client", return_value=client):
        # Call the function and expect an exception
        with pytest.raises(Exception) as excinfo:
            await get_unity_schema(ctx)
        
        # Verify the exception message contains the expected text
        assert "Not connected to Unity" in str(excinfo.value)
        
        # Verify connect was called (attempted reconnection)
        client.connect.assert_called_once()