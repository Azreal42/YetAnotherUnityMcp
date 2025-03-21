"""
DEPRECATED: Unity Socket client compatibility wrapper

This module is maintained for backward compatibility only and will be removed in a future version.
It simply delegates all calls to the UnityTcpClient implementation.

For new code, use unity_tcp_client.py directly.
"""

import warnings

# Show deprecation warning when module is imported
warnings.warn(
    "unity_socket_client is deprecated and will be removed in a future version. "
    "Use unity_tcp_client instead.",
    DeprecationWarning,
    stacklevel=2
)

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
    
    def __init__(self, url: str = "tcp://127.0.0.1:8080/"):
        """
        Initialize the Unity WebSocket client.
        
        Args:
            url: WebSocket server URL (tcp://host:port/), will be converted to TCP URL
        """
        tcp_url = url
            
        logger.info(f"Creating TCP client with URL: {tcp_url} (from WebSocket URL: {url})")
        self.tcp_client = UnityTcpClient(tcp_url)
        
    @property
    def connected(self) -> bool:
        """
        Check if the client is connected to the server.
        
        Returns:
            True if connected, False otherwise
        """
        return self.tcp_client.connected
        
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

def get_client(url: str = "tcp://127.0.0.1:8080/") -> UnitySocketClient:
    """
    Get the Unity WebSocket client instance.
    
    Args:
        url: WebSocket server URL (tcp://host:port/)
        
    Returns:
        Unity WebSocket client instance
    """
    global _instance
    if _instance is None:
        _instance = UnitySocketClient(url)
    return _instance