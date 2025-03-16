"""
Unity Model Context Protocol (MCP) Server Implementation
Using the MCP Python SDK to expose Unity functionality to AI
"""

from contextlib import asynccontextmanager
from dataclasses import dataclass
from typing import Any, AsyncIterator, Dict, List, Optional

from mcp.server.fastmcp import Context, FastMCP, Image
from pydantic import BaseModel

# Create the Unity MCP Server
mcp = FastMCP("Unity MCP")


# Optional: Define typed context for the server
@dataclass
class UnityContext:
    unity_version: str = "2022.3.16f1"
    platform: str = "Windows"
    project_name: str = "MCP Demo"
    connected: bool = False


# Set up application lifecycle management
@asynccontextmanager
async def unity_lifespan(server: FastMCP) -> AsyncIterator[UnityContext]:
    """Manage Unity connection lifecycle"""
    # Initialize Unity connection on startup
    context = UnityContext()
    print("Connecting to Unity...")
    # In a real implementation, we would establish a connection to Unity here
    context.connected = True
    try:
        yield context
    finally:
        # Cleanup on shutdown
        print("Disconnecting from Unity...")
        context.connected = False


# Initialize server with lifespan
mcp = FastMCP("Unity MCP", lifespan=unity_lifespan)


# ==== MCP Tools ====

@mcp.tool()
def execute_code(code: str, ctx: Context) -> str:
    """Execute C# code in the Unity Editor
    
    Args:
        code: C# code to execute
        
    Returns:
        Execution result or error message
    """
    # This will be implemented to actually execute code in Unity
    try:
        unity_ctx = ctx.request_context.lifespan_context
        ctx.info(f"Executing code in Unity {unity_ctx.unity_version}...")
        return f"Code executed successfully:\n```csharp\n{code}\n```"
    except Exception as e:
        return f"Error: {str(e)}"


@mcp.tool()
async def screen_shot_editor(output_path: str, width: int = 1920, height: int = 1080, ctx: Context) -> Image:
    """Take a screenshot of the Unity Editor
    
    Args:
        output_path: Where to save the screenshot
        width: Screenshot width
        height: Screenshot height
        
    Returns:
        Screenshot image
    """
    # This will be implemented to actually take screenshots in Unity
    ctx.info(f"Taking screenshot ({width}x{height}) to {output_path}...")
    
    # Generate a placeholder image (in real implementation, this would be from Unity)
    from PIL import Image as PILImage, ImageDraw
    
    img = PILImage.new("RGB", (width, height), color=(30, 30, 30))
    draw = ImageDraw.Draw(img)
    draw.rectangle(
        [(width * 0.1, height * 0.1), (width * 0.9, height * 0.9)],
        outline=(0, 100, 200),
        width=5,
    )
    draw.text((width / 2 - 100, height / 2), "Unity Editor", fill=(255, 255, 255))
    
    return Image(data=img, format="png")


@mcp.tool()
def modify_object(object_id: str, property_name: str, property_value: Any) -> Dict[str, Any]:
    """Modify a property of a Unity GameObject
    
    Args:
        object_id: The GameObject identifier
        property_name: The property to modify
        property_value: The new value for the property
        
    Returns:
        Result of the modification operation
    """
    # This will be implemented to actually modify objects in Unity
    return {
        "success": True,
        "object_id": object_id,
        "property": property_name,
        "new_value": property_value,
    }


# ==== MCP Resources ====

@mcp.resource("unity://info")
def get_unity_info(ctx: Context) -> str:
    """Get information about the Unity environment"""
    unity_ctx = ctx.request_context.lifespan_context
    return f"""
Unity Version: {unity_ctx.unity_version}
Platform: {unity_ctx.platform}
Project: {unity_ctx.project_name}
Connected: {unity_ctx.connected}
"""


@mcp.resource("unity://logs")
def get_logs() -> str:
    """Get logs from the Unity editor"""
    # This will be implemented to return real logs from Unity
    return """
[10:30:45] [Info] Application started
[10:30:47] [Info] Scene loaded: SampleScene
[10:31:02] [Warning] Missing reference in GameObject 'Player'
"""


@mcp.resource("unity://scene/{scene_name}")
def get_scene_info(scene_name: str) -> str:
    """Get information about a specific Unity scene
    
    Args:
        scene_name: The name of the scene to get information about
    """
    # This will be implemented to return real scene data from Unity
    return f"""
Scene: {scene_name}
GameObjects: 24
Lights: 2
Cameras: 1
"""


@mcp.resource("unity://object/{object_id}")
def get_object_info(object_id: str) -> str:
    """Get information about a specific Unity GameObject
    
    Args:
        object_id: The GameObject identifier
    """
    # This will be implemented to return real object data from Unity
    return f"""
GameObject: {object_id}
Position: (0, 1, 0)
Rotation: (0, 0, 0, 1)
Scale: (1, 1, 1)
Components: Transform, MeshRenderer, BoxCollider
"""


# ==== MCP Prompts ====

@mcp.prompt()
def create_object() -> str:
    """Prompt template for creating a new GameObject"""
    return """
I need to create a new GameObject in Unity.

Please create a GameObject with these specifications:
- Name: {name}
- Position: ({x}, {y}, {z})
- Components: {components}

Use the execute_code tool to create this object.
"""


@mcp.prompt()
def debug_error() -> str:
    """Prompt template for debugging an error in Unity"""
    return """
I'm getting the following error in Unity:

```
{error_message}
```

Please help me diagnose and fix this issue. You can use the unity://logs resource to see more context.
"""


# Run the server directly if executed as a script
if __name__ == "__main__":
    mcp.run()