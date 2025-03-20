"""
WebSocket-based Unity Model Context Protocol (MCP) Server
Provides real-time communication between Unity and AI models
"""

import asyncio
import sys
import uvicorn
import threading
from typing import Dict, List, Any
import logging
from fastapi import FastAPI, WebSocket

# Import components
from server.connection_manager import ConnectionManager
from server.websocket_handler import websocket_endpoint
from server.mcp import init_mcp

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp_server")

# Create FastMCP and FastAPI instances
from fastmcp import FastMCP
mcp: FastMCP = FastMCP(
    "Unity MCP WebSocket",
    description="WebSocket-based Model Context Protocol for Unity Integration",
    dependencies=["pillow", "websockets"]
)

app: FastAPI = FastAPI()

# Store active WebSocket connections
active_connections: List[WebSocket] = []

# Store pending requests and their corresponding futures
pending_requests: Dict[str, asyncio.Future] = {}

# Create connection manager
manager: ConnectionManager = ConnectionManager()

# Initialize and register MCP tools and resources
init_mcp(mcp)

# Define WebSocket endpoint for MCP communication
@app.websocket("/ws")
async def ws_endpoint(websocket: WebSocket) -> None:
    await websocket_endpoint(websocket, manager, pending_requests)

if __name__ == "__main__":
    # Run MCP in STDIO mode directly
    print("Starting MCP server with STDIO transport...")
    try:
        # Set Windows event loop policy if needed
        if sys.platform == 'win32':
            asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        
        # Start WebSocket server in a separate thread
        def run_websocket_server():
            uvicorn.run(app, host="0.0.0.0", port=8080)
            
        ws_thread = threading.Thread(target=run_websocket_server)
        ws_thread.daemon = True
        ws_thread.start()
        
        # Run MCP server with STDIO transport in main thread
        mcp.run("stdio")
    except KeyboardInterrupt:
        print("MCP server stopped")