"""Take screenshots from Unity"""

import logging
import base64
from typing import Any, Optional
from fastmcp import FastMCP, Context, Image
from server.unity_websocket_client import get_client
import os

logger = logging.getLogger("mcp_tools")

async def take_screenshot(output_path: str, width: int, height: int, ctx: Context) -> Image:
    """
    Take a screenshot in Unity.
    
    Args:
        output_path: Path to save the screenshot
        width: Width of the screenshot
        height: Height of the screenshot
        ctx: MCP context
        
    Returns:
        Screenshot image
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return Image(data=b"", format="png")
    
    try:
        ctx.info(f"Taking screenshot from Unity ({width}x{height})...")
        result = await client.take_screenshot(output_path, width, height)
        
        # Check if the screenshot was saved successfully
        if not os.path.exists(output_path):
            ctx.warning(f"Screenshot was taken but file not found at {output_path}")
            return Image(data=b"", format="png")
        
        # Read the screenshot file
        with open(output_path, "rb") as f:
            image_data = f.read()
        
        # Return as an Image
        return Image(data=image_data, format="png")
    except Exception as e:
        error_msg = f"Error taking screenshot: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return Image(data=b"", format="png")

def register_take_screenshot(mcp: FastMCP) -> None:
    """
    Register the take_screenshot tool with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.tool()
    async def unity_screenshot(output_path: str = "screenshot.png", width: int = 1920, height: int = 1080, ctx: Context = None) -> Image:
        """
        Take a screenshot of the Unity editor window.
        
        Args:
            output_path: Path to save the screenshot
            width: Width of the screenshot
            height: Height of the screenshot
            
        Returns:
            Screenshot image
        """
        return await take_screenshot(output_path, width, height, ctx)