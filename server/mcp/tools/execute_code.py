"""Execute C# code in Unity"""

from typing import Any
from fastmcp import FastMCP, Context
from server.mcp.unity_client_util import execute_unity_operation

async def execute_code_in_unity(code: str, ctx: Context) -> str:
    """
    Execute C# code in Unity.
    
    Args:
        code: C# code to execute
        ctx: MCP context
        
    Returns:
        Result of the code execution
    """
    try:
        result = await execute_unity_operation(
            "code execution", 
            lambda client: client.execute_code(code),
            ctx, 
            "Error executing code"
        )
        return str(result)
    except Exception as e:
        return f"Error: {str(e)}"

def register_execute_code(mcp: FastMCP) -> None:
    """Register the execute_code tool with the MCP instance"""
    mcp.tool()(execute_code_in_unity)