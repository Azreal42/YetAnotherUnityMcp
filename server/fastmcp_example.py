"""
FastMCP Example - Simplified Unity MCP Server

Run with:
    python -m server.fastmcp_example       # Direct execution
    fastmcp dev server/fastmcp_example.py  # Run with Inspector UI
    fastmcp install server/fastmcp_example.py  # Install in Claude Desktop
"""
from fastmcp import FastMCP, Context, Image
from PIL import Image as PILImage, ImageDraw

# Create the Unity MCP Server with dependencies
mcp = FastMCP(
    "Unity MCP - Simple",
    dependencies=["pillow", "numpy"]  # Dependencies for deployment
)


# ===== MCP Tools =====

@mcp.tool()
def execute_code(code: str, ctx: Context) -> str:
    """Execute C# code in the Unity Editor
    
    Args:
        code: C# code to execute
        ctx: MCP Context object
    
    Returns:
        Execution result
    """
    # Log the execution
    ctx.info(f"Executing code in Unity...")
    
    # This would be implemented to actually execute code in Unity
    return f"Code executed successfully:\n```csharp\n{code}\n```"


@mcp.tool()
def set_position(object_name: str, x: float, y: float, z: float) -> str:
    """Set the position of a GameObject in Unity
    
    Args:
        object_name: The name of the GameObject
        x: X position
        y: Y position
        z: Z position
    
    Returns:
        Confirmation message
    """
    # This would be implemented to actually modify object position in Unity
    return f"Position of {object_name} set to ({x}, {y}, {z})"


@mcp.tool()
def take_screenshot(width: int = 1920, height: int = 1080) -> Image:
    """Take a screenshot of the Unity Editor
    
    Args:
        width: Screenshot width in pixels
        height: Screenshot height in pixels
    
    Returns:
        Screenshot image
    """
    # Generate a placeholder image (in real implementation, this would be from Unity)
    img = PILImage.new("RGB", (width, height), color=(30, 30, 30))
    draw = ImageDraw.Draw(img)
    draw.rectangle(
        [(width * 0.1, height * 0.1), (width * 0.9, height * 0.9)],
        outline=(0, 100, 200),
        width=5,
    )
    draw.text((width / 2 - 100, height / 2), "Unity Editor", fill=(255, 255, 255))
    
    # FastMCP automatically handles conversion
    return Image(data=img, format="png")


# ===== MCP Resources =====

@mcp.resource("unity://info")
def get_unity_info() -> str:
    """Get information about the Unity environment"""
    return """
Unity Version: 2022.3.16f1
Platform: Windows
Project: MCP Demo
"""


@mcp.resource("unity://scene/{scene_name}")
def get_scene_info(scene_name: str) -> str:
    """Get information about a specific Unity scene
    
    Args:
        scene_name: Name of the scene to get info about
    
    Returns:
        Scene information as text
    """
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
        object_id: ID of the GameObject
    
    Returns:
        Object information as text
    """
    return f"""
GameObject: {object_id}
Position: (0, 1, 0)
Rotation: (0, 0, 0, 1)
Scale: (1, 1, 1)
Components: Transform, MeshRenderer, BoxCollider
"""


# ===== MCP Prompts =====

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


if __name__ == "__main__":
    # Direct execution
    mcp.run()