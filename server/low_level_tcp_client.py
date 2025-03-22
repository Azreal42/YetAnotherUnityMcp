"""
TCP client for connecting to Unity TCP server.

Despite the filename, this is a TCP client implementation (not WebSocket). 
The name is kept for backward compatibility reasons.

This module provides:
- Low-level TCP communication with the Unity server
- Custom binary framing protocol for message boundaries
- Reconnection and keep-alive mechanisms
- Asynchronous request-response pattern
"""

import json
import logging
import uuid
import asyncio
import struct
import socket
import time
from typing import Dict, Any, Optional, List, Union, Callable

logger = logging.getLogger("mcp_client")

# Protocol constants
START_MARKER = 0x02  # STX (Start of Text)
END_MARKER = 0x03    # ETX (End of Text)
PING_MESSAGE = "PING"
PONG_RESPONSE = "PONG"
HANDSHAKE_REQUEST = "YAUM_HANDSHAKE_REQUEST"
HANDSHAKE_RESPONSE = "YAUM_HANDSHAKE_RESPONSE"
RECONNECT_DELAY = 2  # seconds

class LowLevelTcpClient:
    """
    TCP client for connecting to the Unity MCP TCP server.
    Named WebSocketClient for backward compatibility.
    """
    
    def __init__(self, url: str = "tcp://localhost:8080/"):
        """
        Initialize the TCP client.
        
        Args:
            url: TCP server URL (tcp://host:port/)
        """
        self.url = url
        self.reader = None
        self.writer = None
        self.connected = False
        self.pending_requests: Dict[str, asyncio.Future] = {}
        self.receive_task = None
        self.callbacks: Dict[str, List[Callable]] = {
            "connected": [],
            "disconnected": [],
            "message": [],
            "error": []
        }
        
        if url.startswith("tcp://"):
            self.host = url.replace("tcp://", "").split("/")[0].split(":")[0]
            port_str = url.replace("tcp://", "").split("/")[0].split(":")
            self.port = int(port_str[1]) if len(port_str) > 1 else 8080
        else:
            # Assume basic host:port format
            parts = url.split(":")
            self.host = parts[0]
            self.port = int(parts[1]) if len(parts) > 1 else 8080
            
    async def connect(self, max_attempts: int = 5) -> bool:
        """
        Connect to the Unity TCP server with retry mechanism.
        
        Args:
            max_attempts: Maximum number of connection attempts
            
        Returns:
            True if connected successfully, False otherwise
        """
        if self.connected:
            logger.warning("Already connected to Unity TCP server")
            return True
        
        attempts = 0
        connected = False
        last_error = None
        
        while attempts < max_attempts and not connected:
            attempts += 1
            try:
                logger.info(f"Connecting to Unity TCP server at {self.host}:{self.port} (attempt {attempts}/{max_attempts})")
                
                # Create a TCP connection
                self.reader, self.writer = await asyncio.open_connection(self.host, self.port)
                
                # Perform handshake
                if await self._perform_handshake():
                    self.connected = True
                    connected = True
                    logger.info("Connected to Unity TCP server")
                    
                    # Start the message receive loop
                    self.receive_task = asyncio.create_task(self._receive_messages())
                    
                    # Trigger connected callbacks
                    await self._trigger_callbacks("connected")
                    
                    return True
                else:
                    logger.error("Handshake failed")
                    # Close connection and retry
                    self.writer.close()
                    await self.writer.wait_closed()
                    self.reader = None
                    self.writer = None
            except Exception as e:
                last_error = str(e)
                logger.error(f"Error connecting to Unity TCP server: {last_error}")
                
                # Wait before retrying
                if attempts < max_attempts:
                    wait_time = RECONNECT_DELAY * attempts  # Progressive backoff
                    logger.info(f"Retrying in {wait_time} seconds...")
                    await asyncio.sleep(wait_time)
        
        # All attempts failed
        await self._trigger_callbacks("error", f"Connection error: {last_error}")
        return False
            
    async def disconnect(self) -> None:
        """
        Disconnect from the Unity TCP server.
        """
        if not self.connected:
            logger.warning("Not connected to Unity TCP server")
            return
            
        try:
            logger.info("Disconnecting from Unity TCP server")
            
            # Cancel the receive task
            if self.receive_task:
                self.receive_task.cancel()
                self.receive_task = None
                
            # Close the TCP connection
            if self.writer:
                self.writer.close()
                await self.writer.wait_closed()
                self.writer = None
                self.reader = None
                
            self.connected = False
            logger.info("Disconnected from Unity TCP server")
            
            # Trigger disconnected callbacks
            await self._trigger_callbacks("disconnected")
            
            # Cancel all pending requests
            for request_id, future in self.pending_requests.items():
                if not future.done():
                    future.set_exception(Exception("Disconnected from server"))
            self.pending_requests.clear()
        except Exception as e:
            logger.error(f"Error disconnecting from Unity TCP server: {str(e)}")
            await self._trigger_callbacks("error", f"Disconnection error: {str(e)}")
    
    async def _perform_handshake(self) -> bool:
        """
        Perform handshake with the TCP server.
        
        Returns:
            True if handshake was successful, False otherwise
        """
        try:
            # Send handshake request - ensure it's exactly the expected string with no CR/LF
            handshake_bytes = HANDSHAKE_REQUEST.encode('utf-8')
            logger.info(f"Sending handshake request: {HANDSHAKE_REQUEST} ({len(handshake_bytes)} bytes)")
            # Log hex representation for debugging
            hex_bytes = ' '.join(f'{b:02x}' for b in handshake_bytes)
            logger.info(f"Handshake bytes: {hex_bytes}")
            
            self.writer.write(handshake_bytes)
            await self.writer.drain()
            
            # Read handshake response with timeout
            logger.info("Waiting for handshake response...")
            response_bytes = await asyncio.wait_for(self.reader.read(1024), timeout=5.0)
            
            # Log hex representation for debugging
            hex_bytes = ' '.join(f'{b:02x}' for b in response_bytes)
            logger.info(f"Response bytes: {hex_bytes}")
            
            response = response_bytes.decode('utf-8').strip()
            logger.info(f"Received handshake response: '{response}' ({len(response_bytes)} bytes)")
            
            if response == HANDSHAKE_RESPONSE:
                logger.info("Handshake successful")
                # Add a delay to ensure server is ready
                await asyncio.sleep(0.5)
                logger.info("Handshake completed, now ready for framed communication")
                return True
            else:
                logger.error(f"Invalid handshake response: {response}")
                return False
        except asyncio.TimeoutError:
            logger.error("Handshake timeout")
            return False
        except Exception as e:
            logger.error(f"Handshake error: {str(e)}")
            return False
    
    async def _send_frame(self, message: str) -> None:
        """
        Send a framed message to the TCP server.
        
        Args:
            message: Message to send
        """
        if not self.connected or not self.writer:
            raise Exception("Not connected to Unity TCP server")
        
        # Convert message to bytes
        message_bytes = message.encode('utf-8')
        
        # Create frame: STX + [LENGTH:4] + [MESSAGE] + ETX
        frame = bytearray()
        frame.append(START_MARKER)
        frame.extend(struct.pack("<I", len(message_bytes)))  # Length as 4-byte little-endian
        frame.extend(message_bytes)
        frame.append(END_MARKER)
        
        # Log frame details for debugging
        logger.debug(f"Sending frame: STX + {len(message_bytes)} bytes + ETX (total: {len(frame)} bytes)")
        if len(message) > 200:
            logger.debug(f"Message content (truncated): {message[:100]}... (total: {len(message)} bytes)")
        else:
            logger.debug(f"Message content: {message}")
        
        # Send the frame in a single operation
        self.writer.write(frame)
        await self.writer.drain()
        
        logger.debug("Frame sent successfully")
    
    async def _receive_frame(self) -> Optional[str]:
        """
        Receive a framed message from the TCP server.
        
        Returns:
            Received message as string, or None if connection closed
        """
        if not self.connected or not self.reader:
            raise Exception("Not connected to Unity TCP server")
        
        try:
            # Read until start marker (STX)
            logger.debug("Waiting for start marker (STX)...")
            bytes_checked = 0
            start_marker_found = False
            
            # Store initial bytes for debugging
            initial_bytes = bytearray()
            
            while bytes_checked < 1000:  # Reasonable limit to avoid infinite loop
                try:
                    b = await asyncio.wait_for(self.reader.readexactly(1), timeout=0.5)
                    bytes_checked += 1
                    
                    # Store initial bytes for debugging (up to 16 bytes)
                    if len(initial_bytes) < 16:
                        initial_bytes.append(b[0])
                    
                    if b[0] == START_MARKER:
                        logger.debug(f"Found start marker (STX) after {bytes_checked} bytes")
                        start_marker_found = True
                        break
                    
                    # Log occasionally
                    if bytes_checked % 10 == 0:
                        hex_initial = ' '.join(f'{b:02x}' for b in initial_bytes)
                        logger.debug(f"Checked {bytes_checked} bytes, no start marker yet. Initial bytes: {hex_initial}")
                except asyncio.TimeoutError:
                    # Timeout reading, try again
                    continue
                except asyncio.IncompleteReadError:
                    logger.error("Connection closed while waiting for start marker")
                    return None
            
            if not start_marker_found:
                hex_initial = ' '.join(f'{b:02x}' for b in initial_bytes)
                logger.error(f"No start marker found after {bytes_checked} bytes. Initial bytes: {hex_initial}")
                return None
            
            # Read message length (4 bytes)
            logger.debug("Reading message length (4 bytes)...")
            try:
                length_bytes = await self.reader.readexactly(4)
                message_length = struct.unpack("<I", length_bytes)[0]
                
                # Sanity check for message length
                if message_length <= 0 or message_length > 10 * 1024 * 1024:  # Max 10 MB
                    logger.error(f"Invalid message length: {message_length}")
                    return None
                
                logger.debug(f"Message length: {message_length} bytes")
                
                # Read message data
                logger.debug(f"Reading message data ({message_length} bytes)...")
                message_bytes = await self.reader.readexactly(message_length)
                
                # Prepare to read end marker (ETX)
                logger.debug("Reading end marker (ETX)...")
                
                # Log the last few bytes of the message for debugging
                last_bytes = message_bytes[-min(10, len(message_bytes)):]
                logger.debug(f"Last {len(last_bytes)} bytes of message: {' '.join(f'{b:02x}' for b in last_bytes)} (ASCII: {''.join(chr(b) if 32 <= b < 127 else '.' for b in last_bytes)})")
                
                # Try to read the end marker with more debug info
                try:
                    end_marker = await self.reader.readexactly(1)
                    logger.debug(f"End marker byte: 0x{end_marker[0]:02x} (expected: 0x{END_MARKER:02x})")
                    
                    if end_marker[0] != END_MARKER:
                        # Special case: if the byte we got is '}' (0x7D), this might be the end of a JSON message
                        # Let's try to be resilient and accept it anyway
                        if end_marker[0] == 0x7D:  # ASCII '}'
                            logger.warning("Got '}' (0x7D) instead of ETX marker - potential JSON end, trying to recover")
                            # Try to decode and validate the message
                            try:
                                message_text = message_bytes.decode('utf-8')
                                if message_text.strip().endswith('}'):
                                    # Seems like valid JSON, let's try to parse it
                                    try:
                                        json.loads(message_text)
                                        logger.warning("Message is valid JSON despite incorrect end marker - accepting anyway")
                                        return message_text
                                    except json.JSONDecodeError:
                                        logger.warning("Message ends with '}' but is not valid JSON, rejecting")
                            except:
                                logger.warning("Failed to decode message as UTF-8, rejecting")
                        
                        # Try to read a few more bytes to see what follows
                        try:
                            extra_bytes = await asyncio.wait_for(self.reader.read(10), timeout=0.5)
                            if extra_bytes:
                                logger.error(f"Missing end marker, got: 0x{end_marker[0]:02x} followed by: {' '.join(f'{b:02x}' for b in extra_bytes)}")
                            else:
                                logger.error(f"Missing end marker, got: 0x{end_marker[0]:02x} (no additional bytes available)")
                        except:
                            logger.error(f"Missing end marker, got: 0x{end_marker[0]:02x}")
                        return None
                except Exception as e:
                    logger.error(f"Error reading end marker: {str(e)}")
                    return None
                
                # We already handled the error case above
                # If we get here, the end marker was correct
                
                # Convert to string
                message = message_bytes.decode('utf-8')
                logger.debug(f"Successfully received framed message: {message[:100]}...")
                return message
            except asyncio.IncompleteReadError:
                logger.error("Connection closed while reading message")
                return None
            
        except Exception as e:
            logger.error(f"Error receiving frame: {str(e)}")
            return None
    
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
        Send a command to the Unity TCP server.
        
        Args:
            command: Command to execute
            parameters: Command parameters
            
        Returns:
            Command result
        """
        if not self.connected:
            raise Exception("Not connected to Unity TCP server")
            
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
            await self._send_frame(json.dumps(request))
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
        Receive and process messages from the Unity TCP server.
        """
        if not self.connected:
            logger.error("TCP not connected")
            return
        
        logger.info("Starting message receive loop")
        
        # Send a ping every 30 seconds
        ping_interval = 30
        last_ping_time = time.time()
        
        # Send an initial ping right away to make sure the framing works
        try:
            logger.info("Sending initial PING to test framing...")
            await self._send_frame(PING_MESSAGE)
            logger.info("Initial PING sent successfully")
        except Exception as e:
            logger.error(f"Error sending initial ping: {str(e)}")
            # Continue anyway, we'll try to recover
            
        try:
            while self.connected:
                # Check if it's time to send a ping
                current_time = time.time()
                if current_time - last_ping_time >= ping_interval:
                    try:
                        logger.info("Sending periodic PING...")
                        await self._send_frame(PING_MESSAGE)
                        last_ping_time = current_time
                        logger.info("PING sent successfully")
                    except Exception as e:
                        logger.error(f"Error sending ping: {str(e)}")
                        break  # Connection likely broken
                
                # Try to read with a short timeout
                try:
                    # Use wait_for with a short timeout to allow for ping checks
                    logger.debug("Waiting to receive frame from server...")
                    message = await asyncio.wait_for(self._receive_frame(), timeout=1.0)
                    
                    # Check if connection closed
                    if message is None:
                        logger.error("Connection closed by server")
                        break
                    
                    # Handle special messages
                    if message == PONG_RESPONSE:
                        logger.debug("Received PONG")
                        continue
                    elif message == PING_MESSAGE:
                        # Respond to PING with PONG
                        logger.debug("Received PING, responding with PONG")
                        await self._send_frame(PONG_RESPONSE)
                        continue
                    
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
                except asyncio.TimeoutError:
                    # This is expected - just continue to next iteration
                    continue
                
        except asyncio.CancelledError:
            logger.info("TCP receive task cancelled")
        except Exception as e:
            logger.error(f"TCP receive error: {str(e)}")
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
