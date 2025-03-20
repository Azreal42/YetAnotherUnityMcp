"""Unity information MCP resource implementation"""

import logging
from typing import Dict, Any
from server.websocket_utils import send_request
from server.async_utils import AsyncExecutor

logger = logging.getLogger("mcp_server")

def get_unity_info_handler() -> Dict[str, Any]:
    """Get information about the Unity environment

    Returns:
        Information about the Unity environment
    """
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: get_unity_info(),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting Unity info: {str(e)}")
        return {"error": str(e)}

async def get_unity_info() -> Dict[str, Any]:
    """Get information about the Unity environment implementation"""
    from server.mcp_server import manager, pending_requests
    
    logger.info("Getting Unity info")
    
    try:
        if manager.active_connections:
            response: Dict[str, Any] = await send_request(manager, "unity://info", {}, pending_requests)
            
            if response.get("status") == "error":
                logger.error(f"Error getting Unity info: {response.get('error')}")
            else:
                return response.get("result", {})
        
        return {"error": "No Unity clients connected"}
    except Exception as e:
        logger.error(f"Error getting Unity info: {str(e)}")
        return {"error": str(e)}