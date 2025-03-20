"""Unity scene resource"""

import logging
from typing import Any, Dict
from fastmcp import FastMCP, Context
from server.mcp_client import get_client

logger = logging.getLogger("mcp_resources")

async def get_unity_scene(scene_name: str, ctx: Context) -> Dict[str, Any]:
    """
    Get information about a Unity scene.
    
    Args:
        scene_name: Name of the scene to get information about
        ctx: MCP context
        
    Returns:
        Dictionary containing scene information
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return {"error": "Not connected to Unity"}
    
    try:
        ctx.info(f"Getting information about Unity scene: {scene_name}...")
        # Currently direct scene resource is not implemented in the WebSocket client
        # We'll use execute_code to get scene information
        code = f"""
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("{scene_name}");
            var result = new Dictionary<string, object>();
            
            if (scene.IsValid()) {{
                result["name"] = scene.name;
                result["path"] = scene.path;
                result["isLoaded"] = scene.isLoaded;
                result["rootCount"] = scene.rootCount;
                
                // Get all root objects
                var rootObjects = scene.GetRootGameObjects();
                var objectsList = new List<Dictionary<string, object>>();
                
                foreach (var obj in rootObjects) {{
                    var objInfo = new Dictionary<string, object> {{
                        {{"name", obj.name}},
                        {{"id", obj.GetInstanceID()}},
                        {{"activeSelf", obj.activeSelf}},
                        {{"tag", obj.tag}},
                        {{"layer", obj.layer}}
                    }};
                    objectsList.Add(objInfo);
                }}
                
                result["rootObjects"] = objectsList;
                result["status"] = "success";
            }} else {{
                result["status"] = "error";
                result["error"] = $"Scene '{scene_name}' not found or not valid";
            }}
            
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        """
        
        result = await client.execute_code(code)
        
        # Parse the result into a dictionary
        try:
            import json
            parsed_result = json.loads(result)
            return parsed_result
        except:
            return {
                "status": "error",
                "error": f"Failed to parse scene information: {result}"
            }
    except Exception as e:
        error_msg = f"Error getting scene information: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return {"error": str(e)}

def register_unity_scene(mcp: FastMCP) -> None:
    """
    Register the unity_scene resource with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.resource("unity://scene/{scene_name}")
    async def unity_scene_resource(scene_name: str, ctx: Context) -> Dict[str, Any]:
        """
        Get information about a Unity scene.
        
        Args:
            scene_name: Name of the scene to get information about
            
        Returns:
            Dictionary containing scene information
        """
        return await get_unity_scene(scene_name, ctx)