"""Take a screenshot in Unity"""

from typing import Optional
from fastmcp import FastMCP, Context, Image
from server.mcp.unity_client_util import execute_unity_operation
import os
import base64

async def take_screenshot(output_path: str = "screenshot.png", width: int = 1920, height: int = 1080, ctx: Context) -> Image:
    """
    Take a screenshot of the Unity editor.
    
    Args:
        output_path: Path to save the screenshot
        width: Width of the screenshot
        height: Height of the screenshot
        ctx: MCP context
        
    Returns:
        Screenshot image
    """
    try:
        result = await execute_unity_operation(
            "screenshot capture", 
            lambda client: client.take_screenshot(output_path, width, height),
            ctx, 
            "Error taking screenshot"
        )
        
        # The result should be an object with a filePath property
        if isinstance(result, dict) and 'filePath' in result:
            file_path = result['filePath']
            
            # Read the image file
            if os.path.exists(file_path):
                with open(file_path, 'rb') as f:
                    image_data = f.read()
                
                # Return as an Image
                return Image(data=image_data, format="png")
            else:
                raise Exception(f"Screenshot file not found: {file_path}")
        else:
            raise Exception(f"Invalid screenshot result: {result}")
    except Exception as e:
        ctx.error(f"Screenshot error: {str(e)}")
        # Return a placeholder error image (1x1 red pixel)
        error_pixel = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==")
        return Image(data=error_pixel, format="png")

def register_take_screenshot(mcp: FastMCP) -> None:
    """Register the take_screenshot tool with the MCP instance"""
    mcp.tool()(take_screenshot)