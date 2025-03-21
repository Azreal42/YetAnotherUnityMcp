"""
Unity WebSocket client for communicating with Unity (for backward compatibility)
This is a legacy wrapper that delegates to the UnityTcpClient
"""

import asyncio
import logging
from typing import Dict, Any, Optional, List, Union, Callable

from server.unity_tcp_client import UnityTcpClient, get_client as get_tcp_client

logger = logging.getLogger("unity_websocket_client")

class UnitySocketClient:
    """
    Legacy client for communicating with Unity via WebSocket.
    Now delegates to UnityTcpClient for all operations.
    """
    
    def __init__(self, url: str = "tcp://localhost:8080/"):
        """
        Initialize the Unity WebSocket client.
        
        Args:
            url: WebSocket server URL (tcp://host:port/), will be converted to TCP URL
        """
        tcp_url = url
            
        logger.info(f"Creating TCP client with URL: {tcp_url} (from WebSocket URL: {url})")
        self.tcp_client = UnityTcpClient(tcp_url)
        
    async def connect(self) -> bool:
        """
        Connect to the Unity server.
        
        Returns:
            True if connected successfully, False otherwise
        """
        return await self.tcp_client.connect()
    
    async def disconnect(self) -> None:
        """
        Disconnect from the Unity server.
        """
        await self.tcp_client.disconnect()
    
    async def execute_code(self, code: str) -> Any:
        """
        Execute C# code in Unity.
        
        Args:
            code: C# code to execute
            
        Returns:
            Result of the code execution
        """
        return await self.tcp_client.execute_code(code)
    
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
        return await self.tcp_client.take_screenshot(output_path, width, height)
    
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
        return await self.tcp_client.modify_object(object_id, property_path, property_value)
    
    async def get_logs(self, max_logs: int = 100) -> Any:
        """
        Get logs from Unity.
        
        Args:
            max_logs: Maximum number of logs to retrieve
            
        Returns:
            Unity logs
        """
        return await self.tcp_client.get_logs(max_logs)
    
    async def get_unity_info(self) -> Any:
        """
        Get information about the Unity environment.
        
        Returns:
            Unity environment information
        """
        return await self.tcp_client.get_unity_info()
        
    async def get_schema(self) -> Any:
        """
        Get information about available tools and resources in Unity.
        
        Returns:
            Dictionary containing tools and resources information
        """
        return await self.tcp_client.get_schema()
        
    async def has_command(self, command_name: str) -> bool:
        """
        Check if a command exists in the Unity schema.
        
        Args:
            command_name: Name of the command to check
            
        Returns:
            True if the command exists, False otherwise
        """
        return await self.tcp_client.has_command(command_name)
    
    async def send_command(self, command: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
        """
        Send a command to the Unity server.
        
        Args:
            command: Command to execute
            parameters: Command parameters
            
        Returns:
            Command result
        """
        return await self.tcp_client.send_command(command, parameters)
    
    def on(self, event: str, callback: Callable) -> None:
        """
        Register a callback for an event.
        
        Args:
            event: Event name (connected, disconnected, error)
            callback: Callback function
        """
        self.tcp_client.on(event, callback)
    
    def off(self, event: str, callback: Callable) -> None:
        """
        Unregister a callback for an event.
        
        Args:
            event: Event name (connected, disconnected, error)
            callback: Callback function
        """
        self.tcp_client.off(event, callback)

# Singleton instance for easy access
_instance: Optional[UnitySocketClient] = None

def get_client(url: str = "ws://localhost:8080/") -> UnitySocketClient:
    """
    Get the Unity WebSocket client instance.
    
    Args:
        url: WebSocket server URL (ws://host:port/)
        
    Returns:
        Unity WebSocket client instance
    """
    global _instance
    if _instance is None:
        _instance = UnitySocketClient(url)
    return _instance