"""
Unity Model Context Protocol (MCP) Server with WebSocket client
Provides real-time communication between Python and Unity
"""

import asyncio
import sys
import time
import logging
from typing import Dict, List, Any, AsyncIterator
from contextlib import asynccontextmanager
from mcp.server.fastmcp import FastMCP

# Import components
from server.dynamic_tools import DynamicToolManager
from server.unity_socket_client import UnitySocketClient, get_client

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp_server")

# Unity WebSocket client instance
unity_client: UnitySocketClient = get_client()

@asynccontextmanager
async def server_lifespan(server: Any) -> AsyncIterator[Dict[str, Any]]:
    """
    Lifespan manager for MCP server.
    Handles WebSocket client initialization and cleanup.
    
    Args:
        server: The FastMCP server instance (not used, but required by FastMCP)
        
    Yields:
        Empty dictionary for state (not used)
    """
    logger.info("Server starting: initializing Unity WebSocket client...")
    
    try:
        from server.dynamic_tools import get_manager
        from server.connection_manager import get_unity_connection_manager

        # Get the connection manager
        connection_manager = get_unity_connection_manager()
        
        # Register connected event handler for dynamic tool registration through the connection manager
        async def connected_callback():
            logger.info("Connection established, registering dynamic tools...")
            await register_dynamic_tools(dynamic_manager)
        connection_manager.add_connection_listener(connected_callback)

        # Connect to Unity WebSocket server
        if await connection_manager.connect():
            logger.info("Unity WebSocket client successfully connected")
            
            # Register dynamic tools from Unity schema
            # Note: We need to pass the actual server instance here, which is the mcp object
            # from the enclosing scope (global mcp variable defined below)
            dynamic_manager = get_manager(mcp)
                        
            # Initial registration
            await register_dynamic_tools(dynamic_manager)
            
        else:
            logger.warning("Unity WebSocket client failed to connect, MCP functions may not work")
            logger.info("Auto-reconnection is enabled, the client will try to reconnect automatically")
        
        # Yield to the server (FastMCP will run during this time)
        yield {}
    finally:
        # Clean up when the server stops
        logger.info("Server stopping: disconnecting Unity WebSocket client...")
        connection_manager = get_unity_connection_manager()
        await connection_manager.disconnect()
        logger.info("Unity WebSocket client disconnected")

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

async def connect_to_unity(url: str = "ws://localhost:8080/") -> bool:
    """
    Connect to Unity WebSocket server.
    Deprecated: Use the connection manager instead.
    
    Args:
        url: WebSocket server URL
        
    Returns:
        True if connected, False otherwise
    """
    from server.connection_manager import get_unity_connection_manager
    
    # Use the connection manager for more robust connection handling
    connection_manager = get_unity_connection_manager()
    return await connection_manager.connect(url)

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
        
        # Run MCP server with STDIO transport
        logger.info("Starting MCP server with STDIO transport...")
        mcp.run("stdio")
    except KeyboardInterrupt:
        logger.info("MCP server stopped by user")
    except Exception as e:
        logger.error(f"MCP server encountered an error: {str(e)}")

if __name__ == "__main__":
    main()