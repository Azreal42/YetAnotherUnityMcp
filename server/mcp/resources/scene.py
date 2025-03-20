"""Unity scene MCP resource implementation"""

import logging
from typing import Dict, Any
from server.websocket_utils import send_request
from server.async_utils import AsyncExecutor

logger = logging.getLogger("mcp_server")

def get_scene_handler(scene_name: str) -> Dict[str, Any]:
    """Get information about a specific Unity scene

    Args:
        scene_name: Name of the scene

    Returns:
        Information about the scene
    """
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: get_scene(scene_name),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting scene info: {str(e)}")
        return {"name": scene_name, "error": str(e)}

async def get_scene(scene_name: str) -> Dict[str, Any]:
    """Get information about a specific Unity scene implementation"""
    from server.mcp_server import manager, pending_requests
    
    logger.info(f"Getting scene info: {scene_name}")
    
    try:
        if manager.active_connections:
            response: Dict[str, Any] = await send_request(
                manager, 
                f"unity://scene/{scene_name}", 
                {}, 
                pending_requests
            )
            
            if response.get("status") == "error":
                logger.error(f"Error getting scene info: {response.get('error')}")
            else:
                return response.get("result", {})
        
        return {"name": scene_name, "error": "No Unity clients connected"}
    except Exception as e:
        logger.error(f"Error getting scene info: {str(e)}")
        return {"name": scene_name, "error": str(e)}