"""MCP tools module for Unity integration"""

import logging
from fastmcp import FastMCP

from server.mcp.tools.execute_code import execute_code_handler
from server.mcp.tools.screen_shot import screen_shot_editor_handler
from server.mcp.tools.modify_object import modify_object_handler

logger = logging.getLogger("mcp_server")

# MCP instance to be initialized by the server
mcp = None

def init_mcp(mcp_instance: FastMCP):
    """Initialize the MCP instance"""
    global mcp
    mcp = mcp_instance
    _register_tools()

def _register_tools():
    """Register all MCP tools with the FastMCP instance"""
    global mcp
    if mcp is None:
        logger.error("Cannot register tools: MCP instance not initialized")
        return
    
    # Register tool decorators
    mcp.tool()(execute_code_handler)
    mcp.tool()(screen_shot_editor_handler)
    mcp.tool()(modify_object_handler)

__all__ = [
    "init_mcp",
    "execute_code_handler",
    "screen_shot_editor_handler",
    "modify_object_handler"
]