"""
Unity Model Context Protocol (MCP) Server with WebSocket client
Provides real-time communication between Python and Unity
"""

import asyncio
import sys
import time
import logging
from typing import Dict, Any, AsyncIterator
from contextlib import asynccontextmanager
from mcp.server.fastmcp import FastMCP

# Import components
from server.dynamic_tools import DynamicToolManager

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp_server")


@asynccontextmanager
async def server_lifespan(server: Any) -> AsyncIterator[Dict[str, Any]]:
    """
    Lifespan manager for MCP server.
    Handles TCP client initialization and cleanup.
    
    Args:
        server: The FastMCP server instance (not used, but required by FastMCP)
        
    Yields:
        Empty dictionary for state (not used)
    """
    logger.info("Server starting: initializing Unity TCP client...")
    
    try:
        from server.dynamic_tools import DynamicToolManager
        from server.connection_manager import UnityConnectionManager
        from server.unity_tcp_client import UnityTcpClient

        # Get the connection manager
        client = UnityTcpClient("tcp://localhost:8080/")
        connection_manager = UnityConnectionManager(client)
        
        # Register connected event handler for dynamic tool registration through the connection manager
        async def connected_callback():
            logger.info("Connection established, registering dynamic tools...")
            # Get the Unity client from the connection manager
            unity_client = connection_manager.get_client()
            # Pass the client explicitly (required)
            dynamic_manager = DynamicToolManager(mcp, unity_client)
            await register_dynamic_tools(dynamic_manager)
        connection_manager.add_connection_listener(connected_callback)

        # Connect to Unity TCP server
        if await connection_manager.connect():
            logger.info("Unity TCP client successfully connected")
        else:
            logger.warning("Unity TCP client failed to connect, MCP functions may not work")
            logger.info("Auto-reconnection is enabled, the client will try to reconnect automatically")
        
        # Yield to the server (FastMCP will run during this time)
        yield {}
    finally:
        # Clean up when the server stops
        logger.info("Server stopping: disconnecting Unity TCP client...")
        connection_manager = get_unity_connection_manager()
        await connection_manager.disconnect()
        logger.info("Unity TCP client disconnected")

async def register_dynamic_tools(dynamic_manager: DynamicToolManager):
    """
    Register dynamic tools from Unity schema.
    
    Args:
        dynamic_manager: Dynamic tool manager instance
    """
    logger.info("Registering dynamic tools from Unity schema...")
    result = await dynamic_manager.register_from_schema()
    if result:
        logger.info("Dynamic tools registered successfully")
    else:
        logger.warning("Failed to register dynamic tools")


# Create FastMCP instance

mcp: FastMCP = FastMCP(
    "Unity MCP WebSocket Client",
    description="WebSocket-based Model Context Protocol for Unity Integration (Client Mode)",
    lifespan=server_lifespan  # Use our lifespan manager
)

def main() -> None:
    """
    Main entry point.
    """
    try:
        # Set Windows event loop policy if needed
        if sys.platform == 'win32':
            asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        
        logger.info("Starting MCP server with SSE transport...")
        mcp.run("sse")
    except KeyboardInterrupt:
        logger.info("MCP server stopped by user")
    except Exception as e:
        logger.error(f"MCP server encountered an error: {str(e)}")

if __name__ == "__main__":
    main()