"""Unity environment information resource"""

import logging
from typing import Any, Dict
from fastmcp import FastMCP, Context
from server.mcp_client import get_client

logger = logging.getLogger("mcp_resources")

async def get_unity_info(ctx: Context) -> Dict[str, Any]:
    """
    Get information about the Unity environment.
    
    Args:
        ctx: MCP context
        
    Returns:
        Dictionary containing Unity environment information
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return {"error": "Not connected to Unity"}
    
    try:
        ctx.info("Getting Unity environment information...")
        result = await client.get_unity_info()
        
        # Parse the result into a dictionary if it's not already
        if isinstance(result, str):
            try:
                import json
                result = json.loads(result)
            except:
                # If parsing fails, return as a simple dictionary
                return {"info": result}
        
        # Ensure the result is a dictionary
        if not isinstance(result, dict):
            return {"info": str(result)}
            
        return result
    except Exception as e:
        error_msg = f"Error getting Unity info: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return {"error": str(e)}

def register_unity_info(mcp: FastMCP) -> None:
    """
    Register the unity_info resource with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.resource("unity://info")
    async def unity_info_resource(ctx: Context) -> Dict[str, Any]:
        """
        Get information about the Unity environment.
        
        Returns:
            Dictionary containing Unity environment information
        """
        return await get_unity_info(ctx)