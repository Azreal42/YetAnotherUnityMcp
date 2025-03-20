"""Utility functions for Unity client operations"""

import logging
from typing import Any, Callable, TypeVar, Awaitable, Optional, cast
from fastmcp import Context
from server.unity_websocket_client import get_client, UnityWebSocketClient

logger = logging.getLogger("unity_client")

T = TypeVar('T')  # Return type

async def execute_unity_operation(
    operation_name: str,
    operation: Callable[[UnityWebSocketClient], Awaitable[T]],
    ctx: Context,
    error_prefix: str = "Error"
) -> T:
    """
    Execute an operation with the Unity client with proper error handling.
    
    Args:
        operation_name: Name of the operation for logging
        operation: Async function that takes a UnityWebSocketClient and returns a result
        ctx: MCP context
        error_prefix: Prefix for error messages
        
    Returns:
        Result of the operation
    """
    client = get_client()
    
    if not client.connected:
        message = "Not connected to Unity. Please check the Unity connection"
        ctx.error(message)
        # This is a hack to satisfy the type system - we're raising an exception
        # so this return value will never be used
        default: Any = f"{error_prefix}: {message}"
        
        # Try to automatically reconnect before failing
        try:
            ctx.info("Attempting to reconnect to Unity...")
            await client.connect()
            if client.connected:
                ctx.info("Successfully reconnected to Unity")
            else:
                raise Exception("Reconnection failed")
        except Exception as e:
            ctx.error(f"Reconnection failed: {str(e)}")
            raise Exception(message)
    
    try:
        ctx.info(f"Executing {operation_name}...")
        return await operation(client)
    except Exception as e:
        error_msg = f"{error_prefix}: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        raise Exception(error_msg)