"""Utility functions for Unity client operations"""

import logging
from typing import Any, Callable, TypeVar, Awaitable, Optional, cast
from fastmcp import Context
from server.unity_websocket_client import get_client, UnityWebSocketClient
from server.connection_manager import get_unity_connection_manager

logger = logging.getLogger("unity_client")

T = TypeVar('T')  # Return type

async def execute_unity_operation(
    operation_name: str,
    operation: Callable[[UnityWebSocketClient], Awaitable[T]],
    ctx: Context,
    error_prefix: str = "Error"
) -> T:
    """
    Execute an operation with the Unity client with proper error handling and automatic reconnection.
    
    Args:
        operation_name: Name of the operation for logging
        operation: Async function that takes a UnityWebSocketClient and returns a result
        ctx: MCP context
        error_prefix: Prefix for error messages
        
    Returns:
        Result of the operation
    """
    # Get the connection manager for automatic reconnection
    connection_manager = get_unity_connection_manager()
    client = get_client()
    
    # Execute with automatic reconnection
    async def execute_operation():
        ctx.info(f"Executing {operation_name}...")
        return await operation(client)
    
    try:
        # Use the connection manager to execute with automatic reconnection
        return await connection_manager.execute_with_reconnect(execute_operation)
    except Exception as e:
        error_msg = f"{error_prefix}: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        raise Exception(error_msg)