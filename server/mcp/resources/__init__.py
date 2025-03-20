"""MCP resources module for Unity integration"""

import logging
from fastmcp import FastMCP

from server.mcp.resources.unity_info import get_unity_info_handler
from server.mcp.resources.logs import get_logs_handler
from server.mcp.resources.scene import get_scene_handler
from server.mcp.resources.object import get_object_handler

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

__all__ = [
    "init_mcp",
    "get_unity_info_handler",
    "get_logs_handler",
    "get_scene_handler",
    "get_object_handler"
]