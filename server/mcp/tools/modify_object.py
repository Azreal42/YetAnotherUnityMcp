"""Modify Unity objects"""

import logging
from typing import Any, Optional
from fastmcp import FastMCP, Context
from server.unity_websocket_client import get_client

logger = logging.getLogger("mcp_tools")

async def modify_object(object_id: str, property_path: str, property_value: Any, ctx: Context) -> str:
    """
    Modify a property of a Unity object.
    
    Args:
        object_id: ID of the object to modify
        property_path: Path to the property to modify
        property_value: New value for the property
        ctx: MCP context
        
    Returns:
        Result of the modification
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return "Error: Not connected to Unity"
    
    try:
        ctx.info(f"Modifying object {object_id}, property {property_path} to {property_value}...")
        result = await client.modify_object(object_id, property_path, property_value)
        return str(result)
    except Exception as e:
        error_msg = f"Error modifying object: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return f"Error: {str(e)}"

def register_modify_object(mcp: FastMCP) -> None:
    """
    Register the modify_object tool with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.tool()
    async def unity_modify_object(object_id: str, property_path: str, property_value: Any, ctx: Context) -> str:
        """
        Modify a property of a Unity object.
        
        Args:
            object_id: ID or path to the object to modify (e.g. "Main Camera")
            property_path: Path to the property to modify (e.g. "position.x")
            property_value: New value for the property
            
        Returns:
            Result of the modification
        """
        return await modify_object(object_id, property_path, property_value, ctx)