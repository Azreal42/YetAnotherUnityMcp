"""Execute code MCP tool implementation"""

import logging
import asyncio
from typing import Any, Dict
from fastmcp import Context
from server.websocket_utils import send_request
from server.async_utils import AsyncOperation, AsyncExecutor

logger = logging.getLogger("mcp_server")

def execute_code_handler(code: str, ctx: Context) -> str:
    """Execute C# code in Unity Editor

    Args:
        code: The C# code to execute

    Returns:
        Result of the code execution
    """
    try:
        with AsyncOperation("execute_code", {"code_length": len(code)}, timeout=60.0):
            # Use our utility to run the coroutine safely across thread boundaries
            return AsyncExecutor.run_in_thread_or_loop(
                lambda: execute_code(code, ctx),
                timeout=60.0  # 60 second timeout for code execution
            )
    except Exception as e:
        ctx.error(f"Error executing code: {str(e)}")
        return f"Error: {str(e)}"

async def execute_code(
    code: str, 
    ctx: Context
) -> str:
    """Execute C# code in Unity Editor implementation"""
    from server.mcp_server import manager, pending_requests
    
    with AsyncOperation("execute_code_impl", {"code_length": len(code)}, timeout=60.0) as op:
        ctx.info(f"Executing code of length {len(code)}")
        
        try:
            if manager.active_connections:
                response: Dict[str, Any] = await send_request(manager, "execute_code", {"code": code}, pending_requests)
                if response.get("status") == "error":
                    return f"Error: {response.get('error')}"
                
                result: Dict[str, Any] = response.get("result", {})
                output: str = result.get("output", "")
                logs: list = result.get("logs", [])
                return_value: Any = result.get("returnValue")
                
                # Get processing time if available
                server_time = response.get("server_processing_time_ms", 0)
                
                # Build the response with timing information
                return f"Output: {output}\nLogs: {', '.join(logs)}\nReturn: {return_value}\nServer processing time: {server_time}ms"
            else:
                return "No Unity clients connected to execute code"
        except Exception as e:
            ctx.error(f"Error executing code: {str(e)}")
            return f"Error: {str(e)}"