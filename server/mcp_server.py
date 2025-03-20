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

# Import components
from server.unity_websocket_client import UnityWebSocketClient, get_client
from server.mcp import init_mcp

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp_server")

# Unity WebSocket client instance
unity_client: UnityWebSocketClient = get_client()

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
    from server.mcp.dynamic_tools import get_manager

    logger.info("Server starting: initializing Unity WebSocket client...")
    
    try:
        # Connect to Unity WebSocket server
        if await connect_to_unity():
            logger.info("Unity WebSocket client successfully connected")
            
            # Register dynamic tools from Unity schema
            # Note: We need to pass the actual server instance here, which is the mcp object
            # from the enclosing scope (global mcp variable defined below)
            dynamic_manager = get_manager(mcp)
            
            # Register connected event handler for dynamic tool registration
            unity_client.on("connected", lambda: register_dynamic_tools(dynamic_manager))
            
            # Initial registration
            await register_dynamic_tools(dynamic_manager)
            
        else:
            logger.warning("Unity WebSocket client failed to connect, MCP functions may not work")
        
        # Yield to the server (FastMCP will run during this time)
        yield {}
    finally:
        # Clean up when the server stops
        logger.info("Server stopping: disconnecting Unity WebSocket client...")
        await unity_client.disconnect()
        logger.info("Unity WebSocket client disconnected")

async def register_dynamic_tools(dynamic_manager):
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
    
    Args:
        url: WebSocket server URL
        
    Returns:
        True if connected, False otherwise
    """
    try:
        logger.info(f"Connecting to Unity WebSocket server at {url}...")
        result = await unity_client.connect()
        
        if result:
            logger.info("Connected to Unity WebSocket server")
            
            # Get Unity info as a connection test
            try:
                info = await unity_client.get_unity_info()
                logger.info(f"Unity info: {info}")
            except Exception as e:
                logger.error(f"Error getting Unity info: {str(e)}")
        else:
            logger.error("Failed to connect to Unity WebSocket server")
            
        return result
    except Exception as e:
        logger.error(f"Error connecting to Unity: {str(e)}")
        return False

# Create FastMCP instance
from fastmcp import FastMCP
mcp: FastMCP = FastMCP(
    "Unity MCP WebSocket Client",
    description="WebSocket-based Model Context Protocol for Unity Integration (Client Mode)",
    dependencies=["pillow", "websockets"],
    lifespan=server_lifespan  # Use our lifespan manager
)

# Initialize and register MCP tools and resources
init_mcp(mcp)

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