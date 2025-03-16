import logging
import base64
import asyncio
from io import BytesIO
from typing import Any, Dict, Optional, Union
from PIL import Image as PILImage
from fastmcp import Context, Image
from server.websocket_utils import send_request
from server.connection_manager import ConnectionManager

logger = logging.getLogger("mcp_server")

# MCP Tools
async def execute_code(
    code: str, 
    ctx: Context, 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> str:
    """Execute C# code in Unity Editor

    Args:
        code: The C# code to execute

    Returns:
        Result of the code execution
    """
    from server.async_utils import AsyncOperation
    
    with AsyncOperation("execute_code_impl", {"code_length": len(code)}, timeout=60.0) as op:
        ctx.info(f"Executing code of length {len(code)}")
        
        try:
            if manager.active_connections:
                response: Dict[str, Any] = await send_request(manager, "execute_code", {"code": code}, pending_requests)
                if response.get("status") == "error":
                    return f"Error: {response.get('error')}"
                
                result: Dict[str, Any] = response.get("result", {})
                output: str = result.get("output", "")
                logs: list = result.get("logs", [])
                return_value: Any = result.get("returnValue")
                
                # Get processing time if available
                server_time = response.get("server_processing_time_ms", 0)
                
                # Build the response with timing information
                return f"Output: {output}\nLogs: {', '.join(logs)}\nReturn: {return_value}\nServer processing time: {server_time}ms"
            else:
                return "No Unity clients connected to execute code"
        except Exception as e:
            ctx.error(f"Error executing code: {str(e)}")
            return f"Error: {str(e)}"

async def screen_shot_editor(
    output_path: str, 
    width: int, 
    height: int,
    ctx: Optional[Context], 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> Image:
    """Take a screenshot of the Unity Editor

    Args:
        output_path: Path to save the screenshot
        width: Width of the screenshot
        height: Height of the screenshot

    Returns:
        Image object containing the screenshot
    """
    from server.async_utils import AsyncOperation
    
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

async def modify_object(
    object_id: str, 
    property_path: str, 
    property_value: Any,
    ctx: Optional[Context], 
    manager: ConnectionManager, 
    pending_requests: Dict[str, asyncio.Future]
) -> str:
    """Modify a property of a Unity GameObject

    Args:
        object_id: The name or ID of the GameObject
        property_path: Path to the property (e.g. "position.x" or "GetComponent<Renderer>().material.color")
        property_value: The new value for the property

    Returns:
        Result of the object modification
    """
    from server.async_utils import AsyncOperation
    
    with AsyncOperation("modify_object_impl", {
        "object_id": object_id,
        "property_path": property_path
    }, timeout=30.0):
        if ctx:
            ctx.info(f"Modifying object {object_id}: {property_path} = {property_value}")
        else:
            logger.info(f"Modifying object {object_id}: {property_path} = {property_value}")
        
        try:
            if manager.active_connections:
                response: Dict[str, Any] = await send_request(manager, "modify_object", {
                    "object_id": object_id,
                    "property_path": property_path,
                    "property_value": property_value
                }, pending_requests)
                
                if response.get("status") == "error":
                    return f"Error: {response.get('error')}"
                
                # Get processing time if available
                server_time = response.get("server_processing_time_ms", 0)
                return f"Modified {object_id}.{property_path} = {property_value} in {server_time}ms"
            else:
                return "No Unity clients connected to modify object"
        except Exception as e:
            if ctx:
                ctx.error(f"Error modifying object: {str(e)}")
            else:
                logger.error(f"Error modifying object: {str(e)}")
            return f"Error: {str(e)}" 