"""Get logs from Unity"""

import logging
from typing import Any, List, Dict
from fastmcp import FastMCP, Context
from server.unity_websocket_client import get_client

logger = logging.getLogger("mcp_tools")

async def get_logs(max_logs: int, ctx: Context) -> List[Dict[str, Any]]:
    """
    Get logs from Unity.
    
    Args:
        max_logs: Maximum number of logs to retrieve
        ctx: MCP context
        
    Returns:
        List of log entries
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return [{"type": "error", "message": "Not connected to Unity"}]
    
    try:
        ctx.info(f"Getting logs from Unity (max: {max_logs})...")
        result = await client.get_logs(max_logs)
        
        # Parse the result into a list of log entries if it's not already
        if isinstance(result, str):
            try:
                import json
                result = json.loads(result)
            except:
                # If parsing fails, return as a single log message
                return [{"type": "info", "message": result}]
        
        # Ensure the result is a list
        if not isinstance(result, list):
            return [{"type": "info", "message": str(result)}]
            
        return result
    except Exception as e:
        error_msg = f"Error getting logs: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return [{"type": "error", "message": f"Error: {str(e)}"}]

def register_get_logs(mcp: FastMCP) -> None:
    """
    Register the get_logs tool with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.tool()
    async def unity_logs(max_logs: int = 100, ctx: Context = None) -> List[Dict[str, Any]]:
        """
        Get logs from Unity.
        
        Args:
            max_logs: Maximum number of logs to retrieve
            
        Returns:
            List of log entries
        """
        return await get_logs(max_logs, ctx)