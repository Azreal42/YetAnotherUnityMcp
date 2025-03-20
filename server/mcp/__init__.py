"""MCP module for Unity integration"""

import logging
from fastmcp import FastMCP, Context
from typing import Dict, Any
from server.mcp.unity_client_util import execute_unity_operation

logger = logging.getLogger("mcp")

def init_mcp(mcp_instance: FastMCP) -> None:
    """
    Initialize MCP instance with the core schema tool.
    All other tools are dynamically registered from the Unity schema.
    
    Args:
        mcp_instance: FastMCP instance
    """
    logger.info("Initializing MCP with core bootstrap tools")
    
    # Register only the essential tools needed for dynamic registration
    register_get_schema(mcp_instance)
    register_get_unity_info(mcp_instance)

async def get_unity_schema(ctx: Context) -> Dict[str, Any]:
    """
    Get information about available tools and resources in Unity.
    
    Args:
        ctx: MCP context
        
    Returns:
        Dictionary containing tools and resources information
    """
    try:
        result = await execute_unity_operation(
            "schema retrieval", 
            lambda client: client.get_schema(),
            ctx, 
            "Error getting schema"
        )
        
        # Ensure we return a dictionary
        if isinstance(result, dict):
            return result
        else:
            # Convert to a dictionary if it's not already
            return {"schema": str(result)}
    except Exception as e:
        ctx.error(f"Error getting schema: {str(e)}")
        return {"error": str(e)}

async def get_unity_info(ctx: Context) -> Dict[str, Any]:
    """
    Get information about the Unity environment.
    
    Args:
        ctx: MCP context
        
    Returns:
        Dictionary containing Unity environment information
    """
    try:
        result = await execute_unity_operation(
            "Unity info retrieval", 
            lambda client: client.get_unity_info(),
            ctx, 
            "Error getting Unity info"
        )
        
        # Ensure we return a dictionary
        if isinstance(result, dict):
            return result
        else:
            # Convert to a dictionary if it's not already
            return {"info": str(result)}
    except Exception as e:
        ctx.error(f"Error getting Unity info: {str(e)}")
        return {"error": str(e)}

def register_get_schema(mcp: FastMCP) -> None:
    """Register the get_schema tool with the MCP instance"""
    mcp.tool()(get_unity_schema)

def register_get_unity_info(mcp: FastMCP) -> None:
    """Register the get_unity_info tool with the MCP instance"""
    mcp.tool()(get_unity_info)

__all__ = ["init_mcp"]