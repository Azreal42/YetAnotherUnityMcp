"""Screenshot MCP tool implementation"""

import logging
import base64
import asyncio
from io import BytesIO
from typing import Any, Dict, Optional
from PIL import Image as PILImage
from fastmcp import Context, Image
from server.websocket_utils import send_request
from server.async_utils import AsyncOperation, AsyncExecutor

logger = logging.getLogger("mcp_server")

def screen_shot_editor_handler(
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
    try:
        with AsyncOperation("screen_shot_editor", {
            "output_path": output_path,
            "resolution": f"{width}x{height}"
        }, timeout=30.0):
            # Use our utility to run the coroutine safely across thread boundaries
            return AsyncExecutor.run_in_thread_or_loop(
                lambda: screen_shot_editor(output_path, width, height, ctx),
                timeout=30.0
            )
    except Exception as e:
        if ctx:
            ctx.error(f"Error taking screenshot: {str(e)}")
        logger.error(f"Error taking screenshot: {str(e)}")
        # Return an empty image on error
        return Image(data=b"", format="png")

async def screen_shot_editor(
    output_path: str, 
    width: int, 
    height: int,
    ctx: Optional[Context]
) -> Image:
    """Take a screenshot of the Unity Editor implementation"""
    from server.mcp_server import manager, pending_requests
    
    with AsyncOperation("screen_shot_editor_impl", {
        "output_path": output_path,
        "resolution": f"{width}x{height}"
    }, timeout=30.0):
        if ctx:
            ctx.info(f"Taking screenshot: {output_path} ({width}x{height})")
        else:
            logger.info(f"Taking screenshot: {output_path} ({width}x{height})")
        
        try:
            if manager.active_connections:
                response: Dict[str, Any] = await send_request(manager, "screen_shot_editor", {
                    "output_path": output_path,
                    "width": width,
                    "height": height
                }, pending_requests)
                
                if response.get("status") == "error":
                    if ctx:
                        ctx.error(f"Error taking screenshot: {response.get('error')}")
                    else:
                        logger.error(f"Error taking screenshot: {response.get('error')}")
                else:
                    result: Dict[str, Any] = response.get("result", {})
                    image_data: Optional[str] = result.get("image")
                    if image_data:
                        # Decode base64 image data
                        binary_data: bytes = base64.b64decode(image_data)
                        return Image(data=binary_data, format=result.get("format", "png"))
            
            # Create a fallback image if no clients or no image data
            if ctx:
                ctx.info("Using fallback image generation")
            else:
                logger.info("Using fallback image generation")
                
            img: PILImage.Image = PILImage.new('RGB', (width, height), color = 'gray')
            buffer: BytesIO = BytesIO()
            img.save(buffer, format="PNG")
            buffer.seek(0)
            
            # Return the image as a data URI
            return Image(data=buffer.getvalue(), format="png")
        except Exception as e:
            if ctx:
                ctx.error(f"Error taking screenshot: {str(e)}")
            else:
                logger.error(f"Error taking screenshot: {str(e)}")
            # Create error fallback image
            img: PILImage.Image = PILImage.new('RGB', (width, height), color = 'red')
            buffer: BytesIO = BytesIO()
            img.save(buffer, format="PNG")
            buffer.seek(0)
            return Image(data=buffer.getvalue(), format="png")