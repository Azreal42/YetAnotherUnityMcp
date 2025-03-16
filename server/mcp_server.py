"""
WebSocket-based Unity Model Context Protocol (MCP) Server
Provides real-time communication between Unity and AI models
"""

import asyncio
import sys
import uvicorn
import threading
from typing import Dict, List, Any, Optional, Callable, NoReturn
import logging
from fastapi import FastAPI, WebSocket, Request
from fastmcp import FastMCP, Context, Image

# Import components
from server.connection_manager import ConnectionManager
from server.websocket_handler import websocket_endpoint

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp_server")

# Create FastMCP and FastAPI instances
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

# Register MCP tools
from server.mcp_tools import execute_code as _execute_code
from server.mcp_tools import screen_shot_editor as _screen_shot_editor
from server.mcp_tools import modify_object as _modify_object

@mcp.tool()
def execute_code(code: str, ctx: Context) -> str:
    """Execute C# code in Unity Editor

    Args:
        code: The C# code to execute

    Returns:
        Result of the code execution
    """
    from server.async_utils import AsyncExecutor, AsyncOperation
    
    try:
        with AsyncOperation("execute_code", {"code_length": len(code)}, timeout=60.0):
            # Use our utility to run the coroutine safely across thread boundaries
            return AsyncExecutor.run_in_thread_or_loop(
                lambda: _execute_code(code, ctx, manager, pending_requests),
                timeout=60.0  # 60 second timeout for code execution
            )
    except Exception as e:
        ctx.error(f"Error executing code: {str(e)}")
        return f"Error: {str(e)}"

@mcp.tool()
def screen_shot_editor(
    output_path: str = "screenshot.png", 
    width: int = 1920, 
    height: int = 1080,
    ctx: Optional[Context] = None
) -> Image:
    """Take a screenshot of the Unity Editor

    Args:
        output_path: Path to save the screenshot
        width: Screenshot width
        height: Screenshot height

    Returns:
        Screenshot as an Image
    """
    from server.async_utils import AsyncExecutor, AsyncOperation
    
    try:
        with AsyncOperation("screen_shot_editor", {
            "output_path": output_path,
            "resolution": f"{width}x{height}"
        }, timeout=30.0):
            # Use our utility to run the coroutine safely across thread boundaries
            return AsyncExecutor.run_in_thread_or_loop(
                lambda: _screen_shot_editor(output_path, width, height, ctx, manager, pending_requests),
                timeout=30.0
            )
    except Exception as e:
        if ctx:
            ctx.error(f"Error taking screenshot: {str(e)}")
        logger.error(f"Error taking screenshot: {str(e)}")
        # Return an empty image on error
        return Image(data=b"", format="png")

@mcp.tool()
def modify_object(
    object_id: str, 
    property_path: str, 
    property_value: Any,
    ctx: Optional[Context] = None
) -> str:
    """Modify a property of a Unity GameObject

    Args:
        object_id: The name or ID of the GameObject
        property_path: Path to the property (e.g. "position.x" or "GetComponent<Renderer>().material.color")
        property_value: The new value for the property

    Returns:
        Result of the object modification
    """
    from server.async_utils import AsyncExecutor, AsyncOperation
    
    try:
        with AsyncOperation("modify_object", {
            "object_id": object_id,
            "property_path": property_path
        }, timeout=30.0):
            # Use our utility to run the coroutine safely across thread boundaries
            return AsyncExecutor.run_in_thread_or_loop(
                lambda: _modify_object(object_id, property_path, property_value, ctx, manager, pending_requests),
                timeout=30.0
            )
    except Exception as e:
        if ctx:
            ctx.error(f"Error modifying object: {str(e)}")
        logger.error(f"Error modifying object: {str(e)}")
        return f"Error: {str(e)}"

# Register MCP resources
from server.mcp_resources import get_unity_info as _get_unity_info
from server.mcp_resources import get_logs as _get_logs
from server.mcp_resources import get_scene as _get_scene
from server.mcp_resources import get_object as _get_object

@mcp.resource("unity://info")
def get_unity_info() -> Dict[str, Any]:
    """Get information about the Unity environment

    Returns:
        Information about the Unity environment
    """
    from server.async_utils import AsyncExecutor
    
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: _get_unity_info(manager, pending_requests),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting Unity info: {str(e)}")
        return {"error": str(e)}

@mcp.resource("unity://logs/{max_logs}")
def get_logs(max_logs: int = 100) -> List[Dict[str, Any]]:
    """Get logs from the Unity Editor

    Args:
        max_logs: Maximum number of logs to return

    Returns:
        List of log entries
    """
    from server.async_utils import AsyncExecutor
    
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: _get_logs(max_logs, manager, pending_requests),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting logs: {str(e)}")
        return [{"error": str(e)}]

@mcp.resource("unity://scene/{scene_name}")
def get_scene(scene_name: str) -> Dict[str, Any]:
    """Get information about a specific Unity scene

    Args:
        scene_name: Name of the scene

    Returns:
        Information about the scene
    """
    from server.async_utils import AsyncExecutor
    
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: _get_scene(scene_name, manager, pending_requests),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting scene info: {str(e)}")
        return {"name": scene_name, "error": str(e)}

@mcp.resource("unity://object/{object_id}")
def get_object(object_id: str) -> Dict[str, Any]:
    """Get information about a specific Unity GameObject

    Args:
        object_id: Name or ID of the GameObject

    Returns:
        Information about the GameObject
    """
    from server.async_utils import AsyncExecutor
    
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: _get_object(object_id, manager, pending_requests),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting object info: {str(e)}")
        return {"name": object_id, "error": str(e)}

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