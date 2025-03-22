"""
Unity TCP client for communicating with Unity via TCP
"""

import asyncio
import json
import logging
from typing import Dict, Any, Optional, List, Union, Callable
from server.low_level_tcp_client import LowLevelTcpClient

logger = logging.getLogger("unity_client")

class UnityTcpClient:
    """
    High-level client for communicating with Unity via TCP.
    Implements the MCP protocol with specific Unity command methods.
    """
    
    def __init__(self, url: str = "tcp://localhost:8080/"):
        """
        Initialize the Unity TCP client.
        
        Args:
            url: TCP server URL (tcp://host:port/)
        """
        self.tcp_client = LowLevelTcpClient(url)  # Using the low-level TCP client
        self.connected = False
        self.callbacks: Dict[str, List[Callable]] = {
            "connected": [],
            "disconnected": [],
            "error": []
        }
        
        # Register TCP event handlers
        self.tcp_client.on("connected", self._on_tcp_connected)
        self.tcp_client.on("disconnected", self._on_tcp_disconnected)
        self.tcp_client.on("error", self._on_tcp_error)
    
    async def connect(self) -> bool:
        """
        Connect to the Unity TCP server.
        
        Returns:
            True if connected successfully, False otherwise
        """
        try:
            result = await self.tcp_client.connect()
            self.connected = result
            return result
        except Exception as e:
            logger.error(f"Error connecting to Unity: {str(e)}")
            return False
    
    async def disconnect(self) -> None:
        """
        Disconnect from the Unity TCP server.
        """
        try:
            await self.tcp_client.disconnect()
            self.connected = False
        except Exception as e:
            logger.error(f"Error disconnecting from Unity: {str(e)}")
    
    async def get_schema(self) -> Any:
        """
        Get information about available tools and resources in Unity.
        
        Returns:
            Dictionary containing tools and resources information
        """
        return await self.tcp_client.send_command("get_schema")
        
    async def has_command(self, command_name: str) -> bool:
        """
        Check if a command exists in the Unity schema.
        
        Args:
            command_name: Name of the command to check
            
        Returns:
            True if the command exists, False otherwise
        """
        try:
            schema = await self.get_schema()
            if not schema or not isinstance(schema, dict):
                return False
                
            tools = schema.get('tools', [])
            for tool in tools:
                if tool.get('name') == command_name:
                    return True
                    
            return False
        except Exception as e:
            logger.error(f"Error checking for command {command_name}: {str(e)}")
            return False
    
    async def send_command(self, command: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
        """
        Send a command to the Unity TCP server.
        
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
    
    async def _on_tcp_connected(self) -> None:
        """
        Handle TCP connected event.
        """
        self.connected = True
        await self._trigger_callbacks("connected")
    
    async def _on_tcp_disconnected(self) -> None:
        """
        Handle TCP disconnected event.
        """
        self.connected = False
        await self._trigger_callbacks("disconnected")
    
    async def _on_tcp_error(self, error: str) -> None:
        """
        Handle TCP error event.
        
        Args:
            error: Error message
        """
        await self._trigger_callbacks("error", error)

# Singleton instance for easy access
_instance: Optional[UnityTcpClient] = None

def get_client(url: str = "tcp://localhost:8080/") -> UnityTcpClient:
    """
    Get the Unity TCP client instance.
    
    Args:
        url: TCP server URL (tcp://host:port/)
        
    Returns:
        Unity TCP client instance
    """
    global _instance
    if _instance is None:
        _instance = UnityTcpClient(url)
    return _instance