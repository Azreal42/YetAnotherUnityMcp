"""Utility functions for Unity client operations"""

import logging
from typing import Any, Callable, TypeVar, Awaitable
from mcp.server.fastmcp import Context
from server.connection_manager import UnityConnectionManager
from server.unity_tcp_client import UnityTcpClient

logger = logging.getLogger("unity_client")

T = TypeVar('T')  # Return type

class UnityClientUtil:
    @staticmethod
    async def execute_unity_operation(
        connection_manager: UnityConnectionManager,
        operation_name: str,
        operation: Callable[[UnityTcpClient], Awaitable[T]],
        ctx: Context,
        error_prefix: str = "Error"
    ) -> T:
        """
        Execute an operation with the Unity client with proper error handling and automatic reconnection.
        
        Args:
            operation_name: Name of the operation for logging
            operation: Async function that takes a UnityTcpClient and returns a result
            ctx: MCP context
            error_prefix: Prefix for error messages
            
        Returns:
            Result of the operation
        """
        
        # Execute with automatic reconnection
        async def execute_operation():
            if ctx:
                await ctx.info(f"Executing {operation_name}...")
            logger.info(f"Executing {operation_name}...")
            
            result = await operation()
            
            # Handle MCP response format
            if isinstance(result, dict) and "content" in result:
                # Extract content from MCP response
                content = result["content"]
                if content and isinstance(content, list):
                    if ctx:
                        await ctx.info(f"Received MCP response with {len(content)} content items")
                    logger.info(f"Received MCP response with {len(content)} content items")
                    
                    # Log content types for debugging
                    content_types = [item.get("type") for item in content if isinstance(item, dict)]
                    if ctx:
                        await ctx.debug(f"MCP content types: {content_types}")
                    logger.debug(f"MCP content types: {content_types}")
            
            return result
        
        try:
            # Use the connection manager to execute with automatic reconnection
            return await connection_manager.execute_with_reconnect(execute_operation)
        except Exception as e:
            error_msg = f"{error_prefix}: {str(e)}"
            if ctx:
                await ctx.error(error_msg)
            logger.error(error_msg)
            raise Exception(error_msg)