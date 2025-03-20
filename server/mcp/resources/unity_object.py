"""Unity object resource"""

import logging
from typing import Any, Dict
from fastmcp import FastMCP, Context
from server.mcp_client import get_client

logger = logging.getLogger("mcp_resources")

async def get_unity_object(object_id: str, ctx: Context) -> Dict[str, Any]:
    """
    Get information about a Unity GameObject.
    
    Args:
        object_id: ID or path of the GameObject to get information about
        ctx: MCP context
        
    Returns:
        Dictionary containing object information
    """
    client = get_client()
    
    if not client.connected:
        ctx.error("Not connected to Unity. Please check the Unity connection")
        return {"error": "Not connected to Unity"}
    
    try:
        ctx.info(f"Getting information about Unity object: {object_id}...")
        # Currently direct object resource is not implemented in the WebSocket client
        # We'll use execute_code to get object information
        
        # First, determine if the object_id is a numeric ID or a path/name
        try:
            int_id = int(object_id)
            # If we got here, it's a numeric ID
            find_method = $"UnityEngine.Object.FindObjectFromInstanceID({int_id})"
        except ValueError:
            # It's a path or name
            if "/" in object_id:
                # It's a path
                find_method = $"GameObject.Find(\"{object_id}\")"
            else:
                # It's a name
                find_method = $"GameObject.Find(\"{object_id}\")"
        
        code = f"""
            var obj = {find_method} as GameObject;
            var result = new Dictionary<string, object>();
            
            if (obj != null) {{
                result["name"] = obj.name;
                result["id"] = obj.GetInstanceID();
                result["activeSelf"] = obj.activeSelf;
                result["activeInHierarchy"] = obj.activeInHierarchy;
                result["tag"] = obj.tag;
                result["layer"] = obj.layer;
                result["isStatic"] = obj.isStatic;
                
                // Get transform information
                var transform = obj.transform;
                result["position"] = new Dictionary<string, float> {{
                    {{"x", transform.position.x}},
                    {{"y", transform.position.y}},
                    {{"z", transform.position.z}}
                }};
                
                result["rotation"] = new Dictionary<string, float> {{
                    {{"x", transform.rotation.eulerAngles.x}},
                    {{"y", transform.rotation.eulerAngles.y}},
                    {{"z", transform.rotation.eulerAngles.z}}
                }};
                
                result["scale"] = new Dictionary<string, float> {{
                    {{"x", transform.localScale.x}},
                    {{"y", transform.localScale.y}},
                    {{"z", transform.localScale.z}}
                }};
                
                // Get component information
                var components = obj.GetComponents<Component>();
                var componentsList = new List<Dictionary<string, object>>();
                
                foreach (var component in components) {{
                    if (component != null) {{
                        var compInfo = new Dictionary<string, object> {{
                            {{"type", component.GetType().Name}},
                            {{"enabled", component is Behaviour ? (component as Behaviour).enabled : true}}
                        }};
                        componentsList.Add(compInfo);
                    }}
                }}
                
                result["components"] = componentsList;
                
                // Get child objects
                var children = new List<Dictionary<string, object>>();
                for (int i = 0; i < transform.childCount; i++) {{
                    var child = transform.GetChild(i).gameObject;
                    var childInfo = new Dictionary<string, object> {{
                        {{"name", child.name}},
                        {{"id", child.GetInstanceID()}},
                        {{"activeSelf", child.activeSelf}}
                    }};
                    children.Add(childInfo);
                }}
                
                result["children"] = children;
                result["status"] = "success";
            }} else {{
                result["status"] = "error";
                result["error"] = $"GameObject with ID/path '{object_id}' not found";
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
                "error": f"Failed to parse object information: {result}"
            }
    except Exception as e:
        error_msg = f"Error getting object information: {str(e)}"
        ctx.error(error_msg)
        logger.error(error_msg)
        return {"error": str(e)}

def register_unity_object(mcp: FastMCP) -> None:
    """
    Register the unity_object resource with the MCP instance.
    
    Args:
        mcp: MCP instance
    """
    @mcp.resource("unity://object/{object_id}")
    async def unity_object_resource(object_id: str, ctx: Context) -> Dict[str, Any]:
        """
        Get information about a Unity GameObject.
        
        Args:
            object_id: ID or path of the GameObject to get information about
            
        Returns:
            Dictionary containing object information
        """
        return await get_unity_object(object_id, ctx)