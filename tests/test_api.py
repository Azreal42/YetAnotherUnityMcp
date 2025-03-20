"""
Tests for the WebSocket-based MCP API
"""

import pytest
import unittest.mock as mock
import json
import asyncio
from fastapi.testclient import TestClient
from fastapi import WebSocket
from server.mcp_server import app
from server.connection_manager import ConnectionManager
from server.websocket_handler import websocket_endpoint


@pytest.fixture
def client():
    """Create a test client for the FastAPI app"""
    return TestClient(app)


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


def create_mock_websocket():
    """Create a mock WebSocket for testing"""
    websocket = mock.MagicMock(spec=WebSocket)
    # Mock the receive_text method to return different messages
    mock_messages = [
        json.dumps({
            "id": "req-1",
            "command": "get_unity_info",
            "parameters": {}
        }),
        json.dumps({
            "id": "req-2",
            "command": "execute_code",
            "parameters": {"code": "Debug.Log('Test');"}
        }),
        # Simulate client disconnection after these messages
        WebSocket.Disconnect()
    ]
    
    # Configure the websocket.receive_text mock to return messages in sequence
    websocket.receive_text = mock.AsyncMock(side_effect=mock_messages)
    
    return websocket


@pytest.mark.asyncio
async def test_websocket_connection_lifecycle(mock_manager, pending_requests):
    """Test the complete lifecycle of a WebSocket connection"""
    # Create a mock websocket
    websocket = create_mock_websocket()
    
    # Define mocks for the functions called in the handler
    with mock.patch("server.mcp.resources.unity_info.get_unity_info") as mock_get_info, \
         mock.patch("server.mcp.tools.execute_code.execute_code") as mock_execute_code:
        
        # Configure mocks to return appropriate values
        mock_get_info.return_value = {"unity_version": "2022.3.1f1", "platform": "Windows"}
        mock_execute_code.return_value = "Code executed successfully"
        
        # Call the websocket endpoint
        with pytest.raises(WebSocket.Disconnect):
            await websocket_endpoint(websocket, mock_manager, pending_requests)
        
        # Verify the manager methods were called in the expected sequence
        mock_manager.connect.assert_called_once_with(websocket)
        
        # Check that send_message was called twice (once for each API call)
        assert mock_manager.send_message.call_count == 2
        
        # Verify we attempted to process get_unity_info
        mock_get_info.assert_called_once()
        
        # Verify we attempted to process execute_code
        mock_execute_code.assert_called_once()
        
        # Verify websocket was disconnected
        mock_manager.disconnect.assert_called_once_with(websocket)


@pytest.mark.asyncio
async def test_ws_execute_code_command(mock_manager, pending_requests):
    """Test the execute_code command over WebSocket"""
    # Create a websocket that only sends an execute_code command
    websocket = mock.MagicMock(spec=WebSocket)
    websocket.receive_text = mock.AsyncMock(side_effect=[
        json.dumps({
            "id": "req-1",
            "command": "execute_code",
            "parameters": {"code": "Debug.Log('Hello World');"}
        }),
        WebSocket.Disconnect()
    ])
    
    # Mock the execute_code function
    with mock.patch("server.mcp.tools.execute_code.execute_code") as mock_execute_code:
        # Set up the mock to return a success message
        mock_execute_code.return_value = "Code executed successfully"
        
        # Call the websocket endpoint
        with pytest.raises(WebSocket.Disconnect):
            await websocket_endpoint(websocket, mock_manager, pending_requests)
        
        # Verify execute_code was called with the right arguments
        mock_execute_code.assert_called_once()
        # The first arg should be the code string
        assert mock_execute_code.call_args[0][0] == "Debug.Log('Hello World');"
        
        # Verify the response was sent
        mock_manager.send_message.assert_called_once()
        # The response should be JSON with a success status
        response_json = mock_manager.send_message.call_args[0][1]
        response = json.loads(response_json)
        assert response["status"] == "success"
        assert response["result"] == "Code executed successfully"


@pytest.mark.asyncio
async def test_ws_take_screenshot_command(mock_manager, pending_requests):
    """Test the screen_shot_editor command over WebSocket"""
    # Create a websocket that only sends a screen_shot_editor command
    websocket = mock.MagicMock(spec=WebSocket)
    websocket.receive_text = mock.AsyncMock(side_effect=[
        json.dumps({
            "id": "req-1",
            "command": "screen_shot_editor",
            "parameters": {"output_path": "test.png", "width": 800, "height": 600}
        }),
        WebSocket.Disconnect()
    ])
    
    # Mock the screen_shot_editor function
    with mock.patch("server.mcp.tools.screen_shot.screen_shot_editor") as mock_screenshot:
        # Set up the mock to return a success message
        from fastmcp import Image
        mock_image = Image(data=b"test_image_data", format="png")
        mock_screenshot.return_value = mock_image
        
        # Call the websocket endpoint
        with pytest.raises(WebSocket.Disconnect):
            await websocket_endpoint(websocket, mock_manager, pending_requests)
        
        # Verify screen_shot_editor was called with the right arguments
        mock_screenshot.assert_called_once()
        # Check parameters
        assert mock_screenshot.call_args[0][0] == "test.png"
        assert mock_screenshot.call_args[0][1] == 800
        assert mock_screenshot.call_args[0][2] == 600
        
        # Verify the response was sent
        mock_manager.send_message.assert_called_once()
        # The response should be JSON with a success status
        response_json = mock_manager.send_message.call_args[0][1]
        response = json.loads(response_json)
        assert response["status"] == "success"
        # The result will be a string message about the screenshot being saved
        assert "Screenshot saved to test.png" in response["result"]


@pytest.mark.asyncio
async def test_ws_response_to_pending_request(mock_manager):
    """Test handling a response to a pending request"""
    # Create a dictionary for pending requests with a future
    future = asyncio.Future()
    pending_requests = {"req-pending": future}
    
    # Create a websocket that sends a response to the pending request
    websocket = mock.MagicMock(spec=WebSocket)
    websocket.receive_text = mock.AsyncMock(side_effect=[
        json.dumps({
            "id": "req-pending",
            "type": "response",
            "status": "success",
            "result": {"key": "value"}
        }),
        WebSocket.Disconnect()
    ])
    
    # Call the websocket endpoint
    with pytest.raises(WebSocket.Disconnect):
        await websocket_endpoint(websocket, mock_manager, pending_requests)
    
    # Verify the future was completed with the expected result
    assert future.done()
    result = future.result()
    assert result["status"] == "success"
    assert result["result"] == {"key": "value"}
    
    # Verify no response was sent (since this was handling a pending request)
    mock_manager.send_message.assert_not_called()


@pytest.mark.asyncio
async def test_ws_error_handling(mock_manager, pending_requests):
    """Test error handling in the WebSocket endpoint"""
    # Create a websocket that sends an invalid command
    websocket = mock.MagicMock(spec=WebSocket)
    websocket.receive_text = mock.AsyncMock(side_effect=[
        json.dumps({
            "id": "req-1",
            "command": "invalid_command",
            "parameters": {}
        }),
        WebSocket.Disconnect()
    ])
    
    # Call the websocket endpoint
    with pytest.raises(WebSocket.Disconnect):
        await websocket_endpoint(websocket, mock_manager, pending_requests)
    
    # Verify an error response was sent
    mock_manager.send_message.assert_called_once()
    response_json = mock_manager.send_message.call_args[0][1]
    response = json.loads(response_json)
    assert response["status"] == "error"
    assert "Unknown command" in response["error"]


@pytest.mark.asyncio
async def test_ws_invalid_json(mock_manager, pending_requests):
    """Test handling invalid JSON in the WebSocket endpoint"""
    # Create a websocket that sends invalid JSON
    websocket = mock.MagicMock(spec=WebSocket)
    websocket.receive_text = mock.AsyncMock(side_effect=[
        "This is not valid JSON",
        WebSocket.Disconnect()
    ])
    
    # Call the websocket endpoint
    with pytest.raises(WebSocket.Disconnect):
        await websocket_endpoint(websocket, mock_manager, pending_requests)
    
    # Verify an error response was sent
    mock_manager.send_message.assert_called_once()
    response_json = mock_manager.send_message.call_args[0][1]
    response = json.loads(response_json)
    assert response["status"] == "error"
    assert "Invalid JSON format" in response["error"]
