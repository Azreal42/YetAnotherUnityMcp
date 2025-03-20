"""Get information about available tools and resources in Unity"""

from typing import Any, Dict
from fastmcp import FastMCP, Context
from server.mcp.unity_client_util import execute_unity_operation

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

def register_get_schema(mcp: FastMCP) -> None:
    """Register the get_schema tool with the MCP instance"""
    mcp.tool()(get_unity_schema)