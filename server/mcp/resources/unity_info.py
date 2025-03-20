"""Unity environment information resource"""

import json
from typing import Any, Dict
from fastmcp import FastMCP, Context
from server.mcp.unity_client_util import execute_unity_operation

async def get_unity_environment_info(ctx: Context) -> Dict[str, Any]:
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
        
        # Parse string results if needed
        if isinstance(result, str):
            try:
                return json.loads(result)
            except json.JSONDecodeError:
                return {"info": result}
        
        # Ensure we return a dictionary
        if isinstance(result, dict):
            return result
        else:
            return {"info": str(result)}
    except Exception as e:
        ctx.error(f"Error getting Unity info: {str(e)}")
        return {"error": str(e)}

def register_unity_info(mcp: FastMCP) -> None:
    """Register the unity_info resource with the MCP instance"""
    @mcp.resource("unity://info")
    async def unity_info_resource(ctx: Context) -> Dict[str, Any]:
        """Get information about the Unity environment"""
        return await get_unity_environment_info(ctx)