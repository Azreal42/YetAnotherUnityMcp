"""Get information about Unity environment"""

from typing import Any, Dict
from fastmcp import FastMCP, Context
from server.mcp.unity_client_util import execute_unity_operation

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

def register_get_unity_info(mcp: FastMCP) -> None:
    """Register the get_unity_info tool with the MCP instance"""
    mcp.tool()(get_unity_info)