import logging
import asyncio
from typing import Dict, Any, List, Optional
from server.websocket_utils import send_request
from server.connection_manager import ConnectionManager

logger = logging.getLogger("mcp_server")

# MCP Resources
async def get_unity_info(
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> Dict[str, Any]:
    """Get information about the Unity environment

    Returns:
        Information about the Unity environment
    """
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

async def get_logs(
    max_logs: int, 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> List[Dict[str, Any]]:
    """Get logs from the Unity Editor

    Args:
        max_logs: Maximum number of logs to return

    Returns:
        List of log entries
    """
    logger.info(f"Getting logs (max: {max_logs})")
    
    try:
        if manager.active_connections:
            response: Dict[str, Any] = await send_request(
                manager, 
                "unity://logs", 
                {"max_logs": max_logs},
                pending_requests
            )
            
            if response.get("status") == "error":
                logger.error(f"Error getting Unity logs: {response.get('error')}")
            else:
                return response.get("result", [])
        
        return [{"error": "No Unity clients connected"}]
    except Exception as e:
        logger.error(f"Error getting Unity logs: {str(e)}")
        return [{"error": str(e)}]

async def get_scene(
    scene_name: str, 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> Dict[str, Any]:
    """Get information about a specific Unity scene

    Args:
        scene_name: Name of the scene

    Returns:
        Information about the scene
    """
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

async def get_object(
    object_id: str, 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> Dict[str, Any]:
    """Get information about a specific Unity GameObject

    Args:
        object_id: Name or ID of the GameObject

    Returns:
        Information about the GameObject
    """
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