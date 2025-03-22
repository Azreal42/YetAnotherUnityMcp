"""
DEPRECATED: Connection manager for WebSocket/TCP connections.

This module contains two connection managers:
1. ConnectionManager - A server-side FastAPI WebSocket connection manager (deprecated)
2. UnityConnectionManager - A client-side connection manager for Unity TCP connections

The server-side ConnectionManager class is no longer used and will be removed in a future version.
Only the UnityConnectionManager is actively maintained.
"""

import warnings

# Show deprecation warning for ConnectionManager
warnings.warn(
    "ConnectionManager class is deprecated and will be removed in a future version. "
    "Only UnityConnectionManager should be used.",
    DeprecationWarning,
    stacklevel=2
)

import asyncio
import logging
import time
from typing import List, Optional, Callable, Dict, Any
from server.unity_tcp_client import get_client
logger = logging.getLogger("mcp_server")


# Client-side connection manager for Unity TCP client
class UnityConnectionManager:
    """
    Connection manager that handles automatic reconnection to Unity
    """
    
    def __init__(self, 
                url: str = "tcp://localhost:8080/",
                reconnect_attempts: int = 5, 
                reconnect_delay: float = 2.0,
                auto_reconnect: bool = True):
        """
        Initialize the connection manager.
        
        Args:
            reconnect_attempts: Maximum number of reconnect attempts
            reconnect_delay: Delay between reconnection attempts in seconds
            auto_reconnect: Whether to automatically reconnect on disconnect
        """
        self.client = get_client(url)
        self.reconnect_attempts = reconnect_attempts
        self.reconnect_delay = reconnect_delay
        self.auto_reconnect = auto_reconnect
        self.reconnect_task: Optional[asyncio.Task] = None
        self.is_reconnecting = False
        self.connection_listeners: List[Callable] = []
        self.disconnection_listeners: List[Callable] = []
        
        # Register event handlers
        self.client.on("disconnected", self._handle_disconnect)
    
    async def connect(self) -> bool:
        """
        Connect to Unity TCP server with automatic reconnection.
            
        Returns:
            True if connected successfully, False otherwise
        """
        return await self.reconnect()
    
    async def disconnect(self) -> None:
        """
        Disconnect from Unity TCP server.
        """
        if not self.client.connected:
            logger.info("Not connected to Unity")
            return
            
        logger.info("Disconnecting from Unity...")
        
        # Cancel any reconnection task
        if self.reconnect_task and not self.reconnect_task.done():
            self.reconnect_task.cancel()
            self.reconnect_task = None
            
        # Disconnect the client
        await self.client.disconnect()
        
        # Notify disconnection listeners
        await self._notify_disconnection_listeners()
    
    async def execute_with_reconnect(self, operation: Callable, *args, **kwargs) -> Any:
        """
        Execute an operation with automatic reconnection if disconnected.
        
        Args:
            operation: Async function to execute
            *args: Arguments to pass to the operation
            **kwargs: Keyword arguments to pass to the operation
            
        Returns:
            Result of the operation
        """
        if not self.client.connected:
            logger.info("Not connected to Unity, attempting to reconnect...")
            connected = await self.reconnect()
            if not connected:
                raise Exception("Not connected to Unity and reconnection failed")
                
        try:
            return await operation(*args, **kwargs)
        except Exception as e:
            # Check if this is a connection-related error
            if "Not connected" in str(e):
                logger.warning(f"Connection error during operation: {str(e)}")
                connected = await self.reconnect()
                if connected:
                    # Retry the operation
                    logger.info("Retrying operation after successful reconnection")
                    return await operation(*args, **kwargs)
                else:
                    raise Exception(f"Operation failed and reconnection failed: {str(e)}")
            else:
                # Not a connection error, re-raise
                raise
    
    async def reconnect(self) -> bool:
        """
        Attempt to reconnect to Unity with multiple attempts.
        
        Returns:
            True if reconnected successfully, False otherwise
        """
        if self.client.connected:
            logger.info("Already connected to Unity")
            return True
            
        if self.is_reconnecting:
            logger.info("Reconnection already in progress")
            # Wait for the existing reconnection to complete
            if self.reconnect_task and not self.reconnect_task.done():
                try:
                    return await self.reconnect_task
                except Exception:
                    return False
            return False
            
        self.is_reconnecting = True
        
        try:
            logger.info(f"Attempting to reconnect to Unity (max {self.reconnect_attempts} attempts)")
            
            for attempt in range(1, self.reconnect_attempts + 1):
                logger.info(f"Reconnection attempt {attempt}/{self.reconnect_attempts}")
                
                try:
                    result = await self.client.connect()
                    if result:
                        logger.info("Reconnected to Unity successfully")
                        # Notify connection listeners
                        await self._notify_connection_listeners()
                        return True
                except Exception as e:
                    logger.error(f"Error during reconnection attempt {attempt}: {str(e)}")
                
                if attempt < self.reconnect_attempts:
                    # Wait before next attempt with exponential backoff
                    delay = self.reconnect_delay * (2 ** (attempt - 1))
                    logger.info(f"Waiting {delay:.1f} seconds before next reconnection attempt")
                    await asyncio.sleep(delay)
            
            logger.error(f"Failed to reconnect to Unity after {self.reconnect_attempts} attempts")
            return False
        finally:
            self.is_reconnecting = False
    
    async def _handle_disconnect(self) -> None:
        """
        Handle disconnection event from Unity.
        """
        logger.info("Disconnected from Unity")
        
        # Notify disconnection listeners
        await self._notify_disconnection_listeners()
        
        # Start automatic reconnection if enabled
        if self.auto_reconnect and not self.is_reconnecting:
            logger.info("Starting automatic reconnection task")
            self.reconnect_task = asyncio.create_task(self._auto_reconnect())
    
    async def _auto_reconnect(self) -> bool:
        """
        Automatic reconnection task.
        
        Returns:
            True if reconnected successfully, False otherwise
        """
        # Add a small delay before reconnecting to avoid rapid reconnection attempts
        await asyncio.sleep(1.0)
        return await self.reconnect()
    
    def add_connection_listener(self, listener: Callable) -> None:
        """
        Add a listener that will be called when connection is established.
        
        Args:
            listener: Async function to call on connection
        """
        if listener not in self.connection_listeners:
            self.connection_listeners.append(listener)
    
    def remove_connection_listener(self, listener: Callable) -> None:
        """
        Remove a connection listener.
        
        Args:
            listener: Listener to remove
        """
        if listener in self.connection_listeners:
            self.connection_listeners.remove(listener)
    
    def add_disconnection_listener(self, listener: Callable) -> None:
        """
        Add a listener that will be called when disconnected.
        
        Args:
            listener: Async function to call on disconnection
        """
        if listener not in self.disconnection_listeners:
            self.disconnection_listeners.append(listener)
    
    def remove_disconnection_listener(self, listener: Callable) -> None:
        """
        Remove a disconnection listener.
        
        Args:
            listener: Listener to remove
        """
        if listener in self.disconnection_listeners:
            self.disconnection_listeners.remove(listener)
    
    async def _notify_connection_listeners(self) -> None:
        """
        Notify all connection listeners.
        """
        for listener in self.connection_listeners:
            try:
                await listener()
            except Exception as e:
                logger.error(f"Error in connection listener: {str(e)}")
    
    async def _notify_disconnection_listeners(self) -> None:
        """
        Notify all disconnection listeners.
        """
        for listener in self.disconnection_listeners:
            try:
                await listener()
            except Exception as e:
                logger.error(f"Error in disconnection listener: {str(e)}")

# Singleton instance for easy access
_unity_connection_manager: Optional[UnityConnectionManager] = None

def get_unity_connection_manager() -> UnityConnectionManager:
    """
    Get the Unity connection manager instance.
    
    Returns:
        Unity connection manager instance
    """
    global _unity_connection_manager
    if _unity_connection_manager is None:
        _unity_connection_manager = UnityConnectionManager()
    return _unity_connection_manager