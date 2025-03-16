import json
import logging
import uuid
import asyncio
from typing import Dict, Any, Optional, List, Union
from fastapi import WebSocket, WebSocketDisconnect
from fastmcp import Context
from server.connection_manager import ConnectionManager
from server.mcp_tools import execute_code, screen_shot_editor, modify_object
from server.mcp_resources import get_unity_info, get_logs, get_scene, get_object

logger = logging.getLogger("mcp_server")

async def websocket_endpoint(
    websocket: WebSocket, 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> None:
    """
    Handle WebSocket connections for MCP communication.
    
    This is the main entry point for WebSocket connections and handles the entire
    WebSocket lifecycle from connection to disconnection.
    """
    # Accept the connection
    await manager.connect(websocket)
    
    try:
        # Process messages in a loop until disconnection
        while True:
            # Receive message from WebSocket client
            data: str = await websocket.receive_text()
            logger.info(f"Received message: {data}")
            
            try:
                # Parse the message as JSON
                message: Dict[str, Any] = json.loads(data)
                
                # Check if this is a response to a pending request
                if message.get("type") == "response" and message.get("id") in pending_requests:
                    request_id: str = message.get("id")
                    future: Optional[asyncio.Future] = pending_requests.get(request_id)
                    if future and not future.done():
                        future.set_result(message)
                    continue
                
                # Extract request ID and command
                request_id: str = message.get("id", f"req-{uuid.uuid4()}")
                command: Optional[str] = message.get("command", message.get("action"))
                parameters: Dict[str, Any] = message.get("parameters", message.get("payload", {}))
                
                # Process the command
                result: Any = None
                error: Optional[str] = None
                
                try:
                    if command == "execute_code":
                        result = await execute_code(
                            parameters.get("code", ""), 
                            Context(),
                            manager,
                            pending_requests
                        )
                    elif command == "screen_shot_editor" or command == "take_screenshot":
                        img = await screen_shot_editor(
                            parameters.get("output_path", "screenshot.png"),
                            parameters.get("width", 1920),
                            parameters.get("height", 1080),
                            Context(),
                            manager,
                            pending_requests
                        )
                        # Convert the image to a base64 string for response
                        result = f"Screenshot saved to {parameters.get('output_path', 'screenshot.png')}"
                    elif command == "modify_object":
                        result = await modify_object(
                            parameters.get("object_id", ""),
                            parameters.get("property_path", ""),
                            parameters.get("property_value", None),
                            Context(),
                            manager,
                            pending_requests
                        )
                    elif command == "get_logs" or command == "unity://logs":
                        max_logs = parameters.get("max_logs", 100)
                        result = await get_logs(max_logs, manager, pending_requests)
                    elif command == "get_unity_info" or command == "unity://info":
                        result = await get_unity_info(manager, pending_requests)
                    elif command and command.startswith("unity://scene/"):
                        scene_name: str = command.replace("unity://scene/", "")
                        result = await get_scene(scene_name, manager, pending_requests)
                    elif command and command.startswith("unity://object/"):
                        object_id: str = command.replace("unity://object/", "")
                        result = await get_object(object_id, manager, pending_requests)
                    else:
                        error = f"Unknown command: {command}"
                        logger.warning(f"Unknown command received: {command}")
                except Exception as e:
                    logger.exception(f"Error processing command {command}")
                    error = f"Error processing command: {str(e)}"
                
                # Get the original timestamp if present
                server_timestamp = message.get("server_timestamp", 0)
                
                import time
                # Send the response with timestamps
                response: Dict[str, Any] = {
                    "id": request_id,
                    "type": "response",
                    "status": "error" if error else "success",
                    "result": result if not error else None,
                    "error": error,
                    "server_timestamp": server_timestamp,  # Original request timestamp
                    "response_timestamp": int(time.time() * 1000)  # When response was created
                }
                
                # Send response back to the client
                await manager.send_message(websocket, json.dumps(response))
                
            except json.JSONDecodeError:
                logger.error(f"Invalid JSON received: {data}")
                await manager.send_message(
                    websocket, 
                    json.dumps({
                        "id": "error",
                        "type": "response",
                        "status": "error",
                        "error": "Invalid JSON format"
                    })
                )
            except Exception as e:
                logger.exception(f"Unexpected error in websocket handler")
                await manager.send_message(
                    websocket, 
                    json.dumps({
                        "id": "error",
                        "type": "response",
                        "status": "error",
                        "error": f"Server error: {str(e)}"
                    })
                )
                
    except WebSocketDisconnect:
        # This is a normal disconnection event
        logger.info("WebSocket client disconnected")
        manager.disconnect(websocket)
    except Exception as e:
        # Handle any other unexpected exceptions to prevent premature return
        logger.exception(f"Unexpected exception in websocket handler: {str(e)}")
        # Make sure we still disconnect the socket
        if websocket in manager.active_connections:
            manager.disconnect(websocket) 