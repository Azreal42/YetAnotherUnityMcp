import json
import uuid
import asyncio
import logging
from typing import Dict, Any, Optional
from server.connection_manager import ConnectionManager

logger = logging.getLogger("mcp_server")

async def send_request(
    manager: ConnectionManager, 
    action: str, 
    payload: Dict[str, Any], 
    pending_requests: Dict[str, asyncio.Future]
) -> Dict[str, Any]:
    """Send a request to Unity via WebSocket and wait for response"""
    if not manager.active_connections:
        logger.warning(f"No Unity clients connected when attempting to send: {action}")
        return {
            "status": "error",
            "error": "No Unity clients connected",
            "result": None
        }
    
    # Generate a unique request ID
    request_id: str = f"req-{uuid.uuid4()}"
    
    # Record the start time of this request
    import time
    start_time_ms = int(time.time() * 1000)
    
    # Log timing information
    logger.info(f"Starting request '{action}' at {start_time_ms}ms")
    
    # Create the request message with timestamp
    request: Dict[str, Any] = {
        "id": request_id,
        "type": "request",
        "action": action,
        "payload": payload,
        "server_timestamp": start_time_ms  # Server timestamp in milliseconds
    }
    
    # Create a future to receive the response
    future: asyncio.Future = asyncio.get_running_loop().create_future()
    pending_requests[request_id] = future
    
    # Convert request to JSON once for efficiency
    json_request = json.dumps(request)
    
    # Determine the appropriate timeout based on action type
    timeout = 60.0  # Default to 60 seconds for most operations
    
    # Send the request to the first connected client
    await manager.send_message(manager.active_connections[0], json_request)
    
    try:
        # Wait for the response with the appropriate timeout
        response: Dict[str, Any] = await asyncio.wait_for(future, timeout=timeout)
        
        # Log completion time
        end_time_ms = int(time.time() * 1000)
        elapsed_ms = end_time_ms - start_time_ms
        logger.info(f"Completed request '{action}' in {elapsed_ms}ms (started at {start_time_ms}ms)")
        
        # Add timing information to the response for debugging
        if isinstance(response, dict):
            response["server_processing_time_ms"] = elapsed_ms
            
        return response
    except asyncio.TimeoutError:
        logger.warning(f"Request timed out: {action}")
        return {
            "status": "error",
            "error": f"Request timed out: {action}",
            "result": None
        }
    except Exception as e:
        logger.exception(f"Error in send_request for {action}: {str(e)}")
        return {
            "status": "error",
            "error": f"Request error: {str(e)}",
            "result": None
        }
    finally:
        pending_requests.pop(request_id, None) 