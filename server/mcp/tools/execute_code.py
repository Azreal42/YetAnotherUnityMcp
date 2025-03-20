"""Execute C# code in Unity"""

import logging
from typing import Any
from fastmcp import FastMCP, Context
from server.unity_websocket_client import get_client

logger = logging.getLogger("mcp_tools")

async def execute_code(code: str, ctx: Context) -> str:
    """
    Execute C# code in Unity.
    
    Args:
        code: C# code to execute
        ctx: MCP context
        
    Returns:
        Result of the code execution
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return "Error: Not connected to Unity"
    
    try:
        ctx.info(f"Executing code in Unity...")
        result = await client.execute_code(code)
        return str(result)
    except Exception as e:
        error_msg = f"Error executing code: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return f"Error: {str(e)}"

def register_execute_code(mcp: FastMCP) -> None:
    """
    Register the execute_code tool with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.tool()
    async def execute_code_in_unity(code: str, ctx: Context) -> str:
        """
        Execute C# code in Unity.
        
        Args:
            code: C# code to execute
            
        Returns:
            Result of the code execution
        """
        return await execute_code(code, ctx)