"""Modify a Unity GameObject property"""

from typing import Any, Optional
from fastmcp import FastMCP, Context
from server.mcp.unity_client_util import execute_unity_operation

async def modify_unity_object(object_id: str, property_path: str, property_value: Any, ctx: Context) -> str:
    """
    Modify a property of a Unity GameObject.
    
    Args:
        object_id: ID or path of the GameObject
        property_path: Path to the property to modify
        property_value: New value for the property
        ctx: MCP context
        
    Returns:
        Result of the modification
    """
    try:
        result = await execute_unity_operation(
            "object modification", 
            lambda client: client.modify_object(object_id, property_path, property_value),
            ctx, 
            "Error modifying object"
        )
        return str(result)
    except Exception as e:
        return f"Error: {str(e)}"

def register_modify_object(mcp: FastMCP) -> None:
    """Register the modify_object tool with the MCP instance"""
    mcp.tool()(modify_unity_object)