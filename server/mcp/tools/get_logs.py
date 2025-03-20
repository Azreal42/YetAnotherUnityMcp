"""Get logs from Unity"""

from typing import Any, List, Dict
from fastmcp import FastMCP, Context
from server.mcp.unity_client_util import execute_unity_operation

async def get_unity_logs(max_logs: int = 100, ctx: Context) -> List[Dict[str, Any]]:
    """
    Get recent logs from Unity.
    
    Args:
        max_logs: Maximum number of logs to retrieve
        ctx: MCP context
        
    Returns:
        List of log entries
    """
    try:
        result = await execute_unity_operation(
            "log retrieval", 
            lambda client: client.get_logs(max_logs),
            ctx, 
            "Error getting logs"
        )
        
        # Ensure the result is a list
        if isinstance(result, list):
            return result
        elif isinstance(result, dict) and 'logs' in result:
            return result['logs']
        else:
            return [{"message": "Invalid log format returned", "type": "Error"}]
    except Exception as e:
        ctx.error(f"Error getting logs: {str(e)}")
        return [{"message": f"Error: {str(e)}", "type": "Error"}]

def register_get_logs(mcp: FastMCP) -> None:
    """Register the get_logs tool with the MCP instance"""
    mcp.tool()(get_unity_logs)