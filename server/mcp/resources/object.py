"""Unity GameObject MCP resource implementation"""

import logging
from typing import Dict, Any
from server.websocket_utils import send_request
from server.async_utils import AsyncExecutor

logger = logging.getLogger("mcp_server")

def get_object_handler(object_id: str) -> Dict[str, Any]:
    """Get information about a specific Unity GameObject

    Args:
        object_id: Name or ID of the GameObject

    Returns:
        Information about the GameObject
    """
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: get_object(object_id),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting object info: {str(e)}")
        return {"name": object_id, "error": str(e)}

async def get_object(object_id: str) -> Dict[str, Any]:
    """Get information about a specific Unity GameObject implementation"""
    from server.mcp_server import manager, pending_requests
    
    logger.info(f"Getting object info: {object_id}")
    
    try:
        if manager.active_connections:
            response: Dict[str, Any] = await send_request(
                manager, 
                f"unity://object/{object_id}", 
                {}, 
                pending_requests
            )
            
            if response.get("status") == "error":
                logger.error(f"Error getting object info: {response.get('error')}")
            else:
                return response.get("result", {})
        
        return {"name": object_id, "error": "No Unity clients connected"}
    except Exception as e:
        logger.error(f"Error getting object info: {str(e)}")
        return {"name": object_id, "error": str(e)}