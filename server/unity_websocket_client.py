"""
Unity WebSocket client for communicating with Unity via WebSocket
"""

import asyncio
import json
import logging
from typing import Dict, Any, Optional, List, Union, Callable
from server.websocket_client import WebSocketClient

logger = logging.getLogger("unity_client")

class UnityWebSocketClient:
    """
    Client for communicating with Unity via WebSocket.
    Implements the MCP protocol.
    """
    
    def __init__(self, url: str = "ws://localhost:8080/"):
        """
        Initialize the Unity WebSocket client.
        
        Args:
            url: WebSocket server URL
        """
        self.ws_client = WebSocketClient(url)
        self.connected = False
        self.callbacks: Dict[str, List[Callable]] = {
            "connected": [],
            "disconnected": [],
            "error": []
        }
        
        # Register WebSocket event handlers
        self.ws_client.on("connected", self._on_ws_connected)
        self.ws_client.on("disconnected", self._on_ws_disconnected)
        self.ws_client.on("error", self._on_ws_error)
    
    async def connect(self) -> bool:
        """
        Connect to the Unity WebSocket server.
        
        Returns:
            True if connected successfully, False otherwise
        """
        try:
            result = await self.ws_client.connect()
            self.connected = result
            return result
        except Exception as e:
            logger.error(f"Error connecting to Unity: {str(e)}")
            return False
    
    async def disconnect(self) -> None:
        """
        Disconnect from the Unity WebSocket server.
        """
        try:
            await self.ws_client.disconnect()
            self.connected = False
        except Exception as e:
            logger.error(f"Error disconnecting from Unity: {str(e)}")
    
    async def execute_code(self, code: str) -> Any:
        """
        Execute C# code in Unity.
        
        Args:
            code: C# code to execute
            
        Returns:
            Result of the code execution
        """
        return await self.ws_client.execute_code(code)
    
    async def take_screenshot(self, output_path: str, width: int = 1920, height: int = 1080) -> Any:
        """
        Take a screenshot in Unity.
        
        Args:
            output_path: Path to save the screenshot
            width: Width of the screenshot
            height: Height of the screenshot
            
        Returns:
            Result of the screenshot operation
        """
        return await self.ws_client.take_screenshot(output_path, width, height)
    
    async def modify_object(self, object_id: str, property_path: str, property_value: Any) -> Any:
        """
        Modify a property of a Unity object.
        
        Args:
            object_id: ID of the object to modify
            property_path: Path to the property to modify
            property_value: New value for the property
            
        Returns:
            Result of the modification
        """
        return await self.ws_client.modify_object(object_id, property_path, property_value)
    
    async def get_logs(self, max_logs: int = 100) -> Any:
        """
        Get logs from Unity.
        
        Args:
            max_logs: Maximum number of logs to retrieve
            
        Returns:
            Unity logs
        """
        return await self.ws_client.get_logs(max_logs)
    
    async def get_unity_info(self) -> Any:
        """
        Get information about the Unity environment.
        
        Returns:
            Unity environment information
        """
        return await self.ws_client.get_unity_info()
        
    async def get_schema(self) -> Any:
        """
        Get information about available tools and resources in Unity.
        
        Returns:
            Dictionary containing tools and resources information
        """
        return await self.ws_client.send_command("get_schema")
    
    async def send_command(self, command: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
        """
        Send a command to the Unity WebSocket server.
        
        Args:
            command: Command to execute
            parameters: Command parameters
            
        Returns:
            Command result
        """
        return await self.ws_client.send_command(command, parameters)
    
    def on(self, event: str, callback: Callable) -> None:
        """
        Register a callback for an event.
        
        Args:
            event: Event name (connected, disconnected, error)
            callback: Callback function
        """
        if event not in self.callbacks:
            logger.warning(f"Unknown event: {event}")
            return
            
        self.callbacks[event].append(callback)
    
    def off(self, event: str, callback: Callable) -> None:
        """
        Unregister a callback for an event.
        
        Args:
            event: Event name (connected, disconnected, error)
            callback: Callback function
        """
        if event not in self.callbacks:
            logger.warning(f"Unknown event: {event}")
            return
            
        if callback in self.callbacks[event]:
            self.callbacks[event].remove(callback)
    
    async def _trigger_callbacks(self, event: str, data: Any = None) -> None:
        """
        Trigger callbacks for an event.
        
        Args:
            event: Event name
            data: Event data
        """
        if event not in self.callbacks:
            return
            
        for callback in self.callbacks[event]:
            try:
                if data is not None:
                    await callback(data)
                else:
                    await callback()
            except Exception as e:
                logger.error(f"Error in {event} callback: {str(e)}")
    
    async def _on_ws_connected(self) -> None:
        """
        Handle WebSocket connected event.
        """
        self.connected = True
        await self._trigger_callbacks("connected")
    
    async def _on_ws_disconnected(self) -> None:
        """
        Handle WebSocket disconnected event.
        """
        self.connected = False
        await self._trigger_callbacks("disconnected")
    
    async def _on_ws_error(self, error: str) -> None:
        """
        Handle WebSocket error event.
        
        Args:
            error: Error message
        """
        await self._trigger_callbacks("error", error)

# Singleton instance for easy access
_instance: Optional[UnityWebSocketClient] = None

def get_client(url: str = "ws://localhost:8080/") -> UnityWebSocketClient:
    """
    Get the Unity WebSocket client instance.
    
    Args:
        url: WebSocket server URL
        
    Returns:
        Unity WebSocket client instance
    """
    global _instance
    if _instance is None:
        _instance = UnityWebSocketClient(url)
    return _instance