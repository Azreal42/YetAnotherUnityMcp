"""MCP resources for Unity integration"""

import logging
import asyncio
from typing import Dict, Any, List, Optional
from fastmcp import FastMCP

from server.websocket_utils import send_request
from server.async_utils import AsyncExecutor

logger = logging.getLogger("mcp_server")

# MCP instance to be initialized by the server
mcp = None

def init_mcp(mcp_instance: FastMCP):
    """Initialize the MCP instance"""
    global mcp
    mcp = mcp_instance
    _register_resources()

def _register_resources():
    """Register all MCP resources with the FastMCP instance"""
    global mcp
    if mcp is None:
        logger.error("Cannot register resources: MCP instance not initialized")
        return
    
    # Register resource decorators
    mcp.resource("unity://info")(get_unity_info_handler)
    mcp.resource("unity://logs/{max_logs}")(get_logs_handler)
    mcp.resource("unity://scene/{scene_name}")(get_scene_handler)
    mcp.resource("unity://object/{object_id}")(get_object_handler)

# Handler functions that will be decorated with @mcp.resource()
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

def get_logs_handler(max_logs: int = 100) -> List[Dict[str, Any]]:
    """Get logs from the Unity Editor

    Args:
        max_logs: Maximum number of logs to return

    Returns:
        List of log entries
    """
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: get_logs(max_logs),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting logs: {str(e)}")
        return [{"error": str(e)}]

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

# Actual implementation functions
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

async def get_logs(max_logs: int) -> List[Dict[str, Any]]:
    """Get logs from the Unity Editor implementation"""
    from server.mcp_server import manager, pending_requests
    
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