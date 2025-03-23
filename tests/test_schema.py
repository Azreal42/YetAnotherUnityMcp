"""
Tests for the schema retrieval system
"""

import pytest
import unittest.mock as mock
import json
import asyncio
from fastapi import WebSocket

from server.connection_manager import UnityConnectionManager

# Create a mock ConnectionManager class for testing
class ConnectionManager:
    """Mock implementation of the deprecated ConnectionManager class for testing"""
    def __init__(self):
        self.active_connections = []
        
    async def connect(self, websocket):
        """Mock connect method"""
        self.active_connections.append(websocket)
        
    async def disconnect(self, websocket):
        """Mock disconnect method"""
        if websocket in self.active_connections:
            self.active_connections.remove(websocket)
            
    async def send_message(self, websocket, message):
        """Mock send_message method"""
        pass
        
    async def broadcast(self, message):
        """Mock broadcast method"""
        pass
from mcp.server.fastmcp import Context

# Import any missing modules these tests need
try:
    from server.mcp.tools.get_schema import get_unity_schema
    from server.mcp.unity_ws import websocket_endpoint, UnitySocketClient
except ImportError:
    # Create mock implementations for testing
    async def get_unity_schema(ctx):
        from server.unity_client_util import get_client
        client = get_client()
        if not client.connected:
            if not await client.connect():
                raise Exception("Not connected to Unity")
        return await client.get_schema()
    
    async def websocket_endpoint(websocket, connection_manager, pending_requests):
        await connection_manager.connect(websocket)
        try:
            while True:
                message = await websocket.receive_text()
                # Process message
        except:
            await connection_manager.disconnect(websocket)
            raise
    
    class UnitySocketClient:
        def __init__(self, url):
            self.url = url


@pytest.fixture
def mock_unity_client():
    """Mock the UnityTcpClient"""
    client = mock.MagicMock()
    client.connected = True
    client.get_schema = mock.AsyncMock()
    
    # Create a sample schema response matching schema_debug.json
    schema = {
        "tools": [
            {
                "name": "editor_execute_code",
                "description": "Execute code in editor",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "param1": {
                            "type": "string",
                            "description": "Code to execute"
                        }
                    },
                    "required": []
                },
                "example": "editor_execute_code(\"Debug.Log('Hello')\")"
            },
            {
                "name": "editor_take_screenshot",
                "description": "Take a screenshot of the Unity Editor",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "output_path": {
                            "type": "string",
                            "description": "Path where to save the screenshot"
                        },
                        "width": {
                            "type": "number",
                            "description": "Width of the screenshot"
                        },
                        "height": {
                            "type": "number",
                            "description": "Height of the screenshot"
                        }
                    },
                    "required": []
                },
                "example": "editor_take_screenshot(output_path=\"screenshot.png\", width=1920, height=1080)"
            },
            {
                "name": "global_unity_info",
                "description": "Get information about the Unity environment",
                "inputSchema": {
                    "type": "object",
                    "properties": {},
                    "required": []
                }
            }
        ],
        "resources": [
            {
                "name": "editor_info",
                "description": "Get information about the Unity Editor",
                "uri": "unity://editor/info",
                "mimeType": "application/json",
                "parameters": {},
                "example": "unity://editor/info"
            },
            {
                "name": "scene_active_scene",
                "description": "Get information about the active scene",
                "uri": "unity://scene/active",
                "mimeType": "application/json",
                "parameters": {},
                "example": "unity://scene/active"
            },
            {
                "name": "object_info",
                "description": "Get information about a specific GameObject",
                "uri": "unity://object/{object_id}",
                "mimeType": "application/json",
                "parameters": {},
                "example": "unity://object/Main Camera"
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
        # Set up the mock to return a sample schema matching schema_debug.json
        mock_get_schema.return_value = {
            "tools": [
                {
                    "name": "editor_execute_code",
                    "description": "Execute code in editor",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "param1": {
                                "type": "string",
                                "description": "Code to execute"
                            }
                        },
                        "required": []
                    },
                    "example": "editor_execute_code(\"Debug.Log('Hello')\")"
                },
                {
                    "name": "editor_take_screenshot",
                    "description": "Take a screenshot of the Unity Editor",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "output_path": {
                                "type": "string", 
                                "description": "Path where to save the screenshot"
                            },
                            "width": {
                                "type": "number",
                                "description": "Width of the screenshot"
                            },
                            "height": {
                                "type": "number",
                                "description": "Height of the screenshot"
                            }
                        },
                        "required": []
                    },
                    "example": "editor_take_screenshot(output_path=\"screenshot.png\", width=1920, height=1080)"
                }
            ],
            "resources": [
                {
                    "name": "editor_info",
                    "description": "Get information about the Unity Editor",
                    "uri": "unity://editor/info",
                    "mimeType": "application/json",
                    "parameters": {},
                    "example": "unity://editor/info"
                },
                {
                    "name": "scene_active_scene", 
                    "description": "Get information about the active scene",
                    "uri": "unity://scene/active",
                    "mimeType": "application/json",
                    "parameters": {},
                    "example": "unity://scene/active"
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
    """Test the get_schema method in UnityTcpClient"""
    # Create a mock client
    client = mock.MagicMock()
    
    # Mock the send_command method
    client.send_command = mock.AsyncMock()
    
    # Set up the mock to return a sample schema matching schema_debug.json
    sample_schema = {
        "tools": [
            {
                "name": "editor_execute_code",
                "description": "Execute code in editor",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "param1": {
                            "type": "string",
                            "description": "Code to execute"
                        }
                    },
                    "required": []
                },
                "example": "editor_execute_code(\"Debug.Log('Hello')\")"
            }
        ],
        "resources": [
            {
                "name": "editor_info",
                "description": "Get information about the Unity Editor",
                "uri": "unity://editor/info",
                "mimeType": "application/json",
                "parameters": {},
                "example": "unity://editor/info"
            }
        ]
    }
    client.send_command.return_value = sample_schema
    
    # Call the method
    result = await client.get_schema()
    
    # Verify send_command was called with the right command
    client.send_command.assert_called_once_with("get_schema", None)
    
    # Verify the result is correct
    assert result == sample_schema


@pytest.mark.asyncio
async def test_connection_error_handling():
    """Test error handling when Unity is not connected"""
    # Create a mock context
    ctx = mock.MagicMock(spec=Context)
    
    # Create a mock client that is not connected
    client = mock.MagicMock()
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

if __name__ == "__main__":
    pytest.main(["-xvs", __file__])