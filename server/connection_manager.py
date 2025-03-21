"""
Connection manager for WebSocket connections.
Manages both server-side connections and client-side reconnection.
"""

import asyncio
import logging
import time
from typing import List, Optional, Callable, Dict, Any
from fastapi import WebSocket
from server.unity_socket_client import get_client

logger = logging.getLogger("mcp_server")

# Server-side connection manager for FastAPI WebSockets
class ConnectionManager:
    def __init__(self) -> None:
        self.active_connections: List[WebSocket] = []

    async def connect(self, websocket: WebSocket) -> None:
        try:
            await websocket.accept()
            self.active_connections.append(websocket)
            logger.info(f"WebSocket connected. Active connections: {len(self.active_connections)}")
            # Log the client details
            client = websocket.client
            logger.info(f"Client connected from: {client.host}:{client.port}")
        except Exception as e:
            logger.error(f"Error accepting WebSocket connection: {str(e)}")

    def disconnect(self, websocket: WebSocket) -> None:
        try:
            if websocket in self.active_connections:
                self.active_connections.remove(websocket)
                # Try to get client info before disconnection
                client_info = "Unknown"
                try:
                    if hasattr(websocket, 'client') and websocket.client:
                        client_info = f"{websocket.client.host}:{websocket.client.port}"
                except:
                    pass
                
                logger.info(f"WebSocket disconnected from {client_info}. Active connections: {len(self.active_connections)}")
            else:
                logger.warning("Attempted to disconnect a WebSocket that was not in active_connections")
        except Exception as e:
            logger.error(f"Error during WebSocket disconnection: {str(e)}")

    async def send_message(self, websocket: WebSocket, message: str) -> None:
        try:
            # Add timing information
            import time
            start_time = time.time()
            
            # Log message size
            logger.debug(f"Sending message of length {len(message)} to client")
            
            # Send the message
            await websocket.send_text(message)
            
            # Log the time it took to send
            elapsed = time.time() - start_time
            if elapsed > 0.5:  # Log if it took more than 500ms
                logger.warning(f"Slow message send: {elapsed:.2f}s for {len(message)} bytes")
        except Exception as e:
            logger.error(f"Error sending message: {str(e)}")

    async def broadcast(self, message: str) -> None:
        if not self.active_connections:
            logger.warning("Attempted to broadcast message but no active connections exist")
            return
            
        logger.info(f"Broadcasting message of length {len(message)} to {len(self.active_connections)} clients")
        
        failed_connections = []
        for connection in self.active_connections:
            try:
                await connection.send_text(message)
            except Exception as e:
                logger.error(f"Error broadcasting to client: {str(e)}")
                failed_connections.append(connection)
        
        # Remove any connections that failed
        for failed in failed_connections:
            if failed in self.active_connections:
                self.active_connections.remove(failed)
                logger.warning("Removed failed connection from active_connections")


# Client-side connection manager for Unity WebSocket client
class UnityConnectionManager:
    """
    Connection manager that handles automatic reconnection to Unity
    """
    
    def __init__(self, 
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
        self.client = get_client()
        self.reconnect_attempts = reconnect_attempts
        self.reconnect_delay = reconnect_delay
        self.auto_reconnect = auto_reconnect
        self.reconnect_task: Optional[asyncio.Task] = None
        self.is_reconnecting = False
        self.connection_listeners: List[Callable] = []
        self.disconnection_listeners: List[Callable] = []
        
        # Register event handlers
        self.client.on("disconnected", self._handle_disconnect)
    
    async def connect(self, url: str = "ws://localhost:8080/") -> bool:
        """
        Connect to Unity WebSocket server with automatic reconnection.
        
        Args:
            url: WebSocket server URL
            
        Returns:
            True if connected successfully, False otherwise
        """
        if self.client.connected:
            logger.info("Already connected to Unity")
            return True
            
        logger.info(f"Connecting to Unity at {url}...")
        
        try:
            result = await self.client.connect()
            if result:
                logger.info("Connected to Unity successfully")
                # Notify connection listeners
                await self._notify_connection_listeners()
                return True
            else:
                logger.error("Failed to connect to Unity")
                return False
        except Exception as e:
            logger.error(f"Error connecting to Unity: {str(e)}")
            return False
    
    async def disconnect(self) -> None:
        """
        Disconnect from Unity WebSocket server.
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