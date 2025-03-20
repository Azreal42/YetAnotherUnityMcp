"""
Unity Model Context Protocol (MCP) Server with WebSocket client
Provides real-time communication between Python and Unity
"""

import asyncio
import sys
import time
import threading
from typing import Dict, List, Any
import logging

# Import components
from server.mcp_client import MCPClient, get_client
from server.mcp import init_mcp

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp_server")

# Create FastMCP instance
from fastmcp import FastMCP
mcp: FastMCP = FastMCP(
    "Unity MCP WebSocket Client",
    description="WebSocket-based Model Context Protocol for Unity Integration (Client Mode)",
    dependencies=["pillow", "websockets"]
)

# Initialize MCP client
unity_client: MCPClient = get_client()

# Initialize and register MCP tools and resources
init_mcp(mcp)

# Store the event loop for background tasks
loop = None

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

async def main_async() -> None:
    """
    Main async function.
    """
    # Set up Unity connection
    await connect_to_unity()
    
    # Keep the program running
    while True:
        await asyncio.sleep(1)

def main() -> None:
    """
    Main entry point.
    """
    global loop
    
    try:
        # Set Windows event loop policy if needed
        if sys.platform == 'win32':
            asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        
        # Create and set event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        
        # Start background task for Unity connection
        unity_task = loop.create_task(main_async())
        
        # Run MCP server with STDIO transport
        logger.info("Starting MCP server with STDIO transport...")
        mcp.run("stdio")
    except KeyboardInterrupt:
        logger.info("MCP server stopped")
    finally:
        # Clean up
        if loop and not loop.is_closed():
            # Cancel all tasks
            for task in asyncio.all_tasks(loop):
                task.cancel()
            
            # Disconnect from Unity
            loop.run_until_complete(unity_client.disconnect())
            
            # Close the event loop
            loop.close()

if __name__ == "__main__":
    main()