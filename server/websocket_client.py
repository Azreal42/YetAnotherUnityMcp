"""
WebSocket client for connecting to Unity WebSocket server.
Provides asynchronous communication with Unity.
"""

import json
import logging
import uuid
import asyncio
import websockets
import time
from typing import Dict, Any, Optional, List, Union, Callable

logger = logging.getLogger("mcp_client")

class WebSocketClient:
    """
    WebSocket client for connecting to the Unity MCP WebSocket server.
    """
    
    def __init__(self, url: str = "ws://localhost:8080/"):
        """
        Initialize the WebSocket client.
        
        Args:
            url: WebSocket server URL
        """
        self.url = url
        self.websocket = None
        self.connected = False
        self.pending_requests: Dict[str, asyncio.Future] = {}
        self.receive_task = None
        self.callbacks: Dict[str, List[Callable]] = {
            "connected": [],
            "disconnected": [],
            "message": [],
            "error": []
        }
        
    async def connect(self) -> bool:
        """
        Connect to the Unity WebSocket server.
        
        Returns:
            True if connected successfully, False otherwise
        """
        if self.connected:
            logger.warning("Already connected to Unity WebSocket server")
            return True
            
        try:
            logger.info(f"Connecting to Unity WebSocket server at {self.url}")
            self.websocket = await websockets.connect(self.url)
            self.connected = True
            logger.info("Connected to Unity WebSocket server")
            
            # Start the message receive loop
            self.receive_task = asyncio.create_task(self._receive_messages())
            
            # Trigger connected callbacks
            await self._trigger_callbacks("connected")
            
            return True
        except Exception as e:
            logger.error(f"Error connecting to Unity WebSocket server: {str(e)}")
            await self._trigger_callbacks("error", f"Connection error: {str(e)}")
            return False
            
    async def disconnect(self) -> None:
        """
        Disconnect from the Unity WebSocket server.
        """
        if not self.connected:
            logger.warning("Not connected to Unity WebSocket server")
            return
            
        try:
            logger.info("Disconnecting from Unity WebSocket server")
            
            # Cancel the receive task
            if self.receive_task:
                self.receive_task.cancel()
                self.receive_task = None
                
            # Close the WebSocket connection
            if self.websocket:
                await self.websocket.close()
                self.websocket = None
                
            self.connected = False
            logger.info("Disconnected from Unity WebSocket server")
            
            # Trigger disconnected callbacks
            await self._trigger_callbacks("disconnected")
            
            # Cancel all pending requests
            for request_id, future in self.pending_requests.items():
                if not future.done():
                    future.set_exception(Exception("Disconnected from server"))
            self.pending_requests.clear()
        except Exception as e:
            logger.error(f"Error disconnecting from Unity WebSocket server: {str(e)}")
            await self._trigger_callbacks("error", f"Disconnection error: {str(e)}")
    
    async def execute_code(self, code: str) -> Any:
        """
        Execute C# code in Unity.
        
        Args:
            code: C# code to execute
            
        Returns:
            Result of the code execution
        """
        parameters = {"code": code}
        return await self.send_command("execute_code", parameters)
        
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
        parameters = {
            "output_path": output_path,
            "width": width,
            "height": height
        }
        return await self.send_command("take_screenshot", parameters)
        
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
        parameters = {
            "object_id": object_id,
            "property_path": property_path,
            "property_value": property_value
        }
        return await self.send_command("modify_object", parameters)
        
    async def get_logs(self, max_logs: int = 100) -> Any:
        """
        Get logs from Unity.
        
        Args:
            max_logs: Maximum number of logs to retrieve
            
        Returns:
            Unity logs
        """
        parameters = {"max_logs": max_logs}
        return await self.send_command("get_logs", parameters)
        
    async def get_unity_info(self) -> Any:
        """
        Get information about the Unity environment.
        
        Returns:
            Unity environment information
        """
        return await self.send_command("get_unity_info", None)
    
    async def send_command(self, command: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
        """
        Send a command to the Unity WebSocket server.
        
        Args:
            command: Command to execute
            parameters: Command parameters
            
        Returns:
            Command result
        """
        if not self.connected:
            raise Exception("Not connected to Unity WebSocket server")
            
        # Generate a unique request ID
        request_id = f"req_{uuid.uuid4().hex}"
        
        # Create a future for the response
        future = asyncio.get_running_loop().create_future()
        self.pending_requests[request_id] = future
        
        # Create the request message
        request = {
            "id": request_id,
            "command": command,
            "client_timestamp": int(time.time() * 1000)
        }
        
        if parameters:
            request["parameters"] = parameters
            
        # Send the request
        try:
            await self.websocket.send(json.dumps(request))
            logger.debug(f"Sent request {request_id}: {command}")
            
            # Wait for the response with a timeout
            try:
                response = await asyncio.wait_for(future, timeout=60.0)
                logger.debug(f"Received response for request {request_id}")
                
                # Process the response
                if response.get("status") == "error":
                    error_message = response.get("error", "Unknown error")
                    raise Exception(f"Error executing command {command}: {error_message}")
                    
                return response.get("result")
            except asyncio.TimeoutError:
                self.pending_requests.pop(request_id, None)
                raise Exception(f"Timeout waiting for response to command {command}")
        except Exception as e:
            self.pending_requests.pop(request_id, None)
            logger.error(f"Error sending command {command}: {str(e)}")
            raise
    
    async def _receive_messages(self) -> None:
        """
        Receive and process messages from the Unity WebSocket server.
        """
        if not self.websocket:
            logger.error("WebSocket not connected")
            return
            
        try:
            async for message in self.websocket:
                try:
                    # Parse the message
                    data = json.loads(message)
                    
                    # Log the message (truncated if large)
                    message_str = message
                    if len(message_str) > 500:
                        message_str = message_str[:500] + "... (truncated)"
                    logger.debug(f"Received message: {message_str}")
                    
                    # Trigger message callbacks
                    await self._trigger_callbacks("message", data)
                    
                    # Check if this is a response to a pending request
                    request_id = data.get("id")
                    if request_id in self.pending_requests:
                        future = self.pending_requests.pop(request_id)
                        if not future.done():
                            future.set_result(data)
                except json.JSONDecodeError:
                    logger.error(f"Invalid JSON received: {message}")
                except Exception as e:
                    logger.exception(f"Error processing message: {str(e)}")
        except asyncio.CancelledError:
            logger.info("WebSocket receive task cancelled")
        except Exception as e:
            logger.error(f"WebSocket receive error: {str(e)}")
            await self._trigger_callbacks("error", f"Receive error: {str(e)}")
            
            # Close the connection on error
            if self.connected:
                await self.disconnect()
    
    def on(self, event: str, callback: Callable) -> None:
        """
        Register a callback for an event.
        
        Args:
            event: Event name (connected, disconnected, message, error)
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
            event: Event name (connected, disconnected, message, error)
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