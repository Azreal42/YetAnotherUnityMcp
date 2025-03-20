"""Modify object MCP tool implementation"""

import logging
import asyncio
from typing import Any, Dict, Optional
from fastmcp import Context
from server.websocket_utils import send_request
from server.async_utils import AsyncOperation, AsyncExecutor

logger = logging.getLogger("mcp_server")

def modify_object_handler(
    object_id: str, 
    property_path: str, 
    property_value: Any,
    ctx: Optional[Context] = None
) -> str:
    """Modify a property of a Unity GameObject

    Args:
        object_id: The name or ID of the GameObject
        property_path: Path to the property (e.g. "position.x" or "GetComponent<Renderer>().material.color")
        property_value: The new value for the property

    Returns:
        Result of the object modification
    """
    try:
        with AsyncOperation("modify_object", {
            "object_id": object_id,
            "property_path": property_path
        }, timeout=30.0):
            # Use our utility to run the coroutine safely across thread boundaries
            return AsyncExecutor.run_in_thread_or_loop(
                lambda: modify_object(object_id, property_path, property_value, ctx),
                timeout=30.0
            )
    except Exception as e:
        if ctx:
            ctx.error(f"Error modifying object: {str(e)}")
        logger.error(f"Error modifying object: {str(e)}")
        return f"Error: {str(e)}"

async def modify_object(
    object_id: str, 
    property_path: str, 
    property_value: Any,
    ctx: Optional[Context] 
) -> str:
    """Modify a property of a Unity GameObject implementation"""
    from server.mcp_server import manager, pending_requests
    
    with AsyncOperation("modify_object_impl", {
        "object_id": object_id,
        "property_path": property_path
    }, timeout=30.0):
        if ctx:
            ctx.info(f"Modifying object {object_id}: {property_path} = {property_value}")
        else:
            logger.info(f"Modifying object {object_id}: {property_path} = {property_value}")
        
        try:
            if manager.active_connections:
                response: Dict[str, Any] = await send_request(manager, "modify_object", {
                    "object_id": object_id,
                    "property_path": property_path,
                    "property_value": property_value
                }, pending_requests)
                
                if response.get("status") == "error":
                    return f"Error: {response.get('error')}"
                
                # Get processing time if available
                server_time = response.get("server_processing_time_ms", 0)
                return f"Modified {object_id}.{property_path} = {property_value} in {server_time}ms"
            else:
                return "No Unity clients connected to modify object"
        except Exception as e:
            if ctx:
                ctx.error(f"Error modifying object: {str(e)}")
            else:
                logger.error(f"Error modifying object: {str(e)}")
            return f"Error: {str(e)}"