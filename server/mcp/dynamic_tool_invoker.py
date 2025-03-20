"""Utility for invoking dynamically registered tools directly"""

import logging
import json
from typing import Any, Dict, Optional
from server.unity_websocket_client import get_client

logger = logging.getLogger("dynamic_invoker")

async def invoke_dynamic_tool(tool_name: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
    """
    Invoke a dynamically registered tool directly.
    
    Args:
        tool_name: Name of the tool to invoke
        parameters: Tool parameters
        
    Returns:
        Tool result
    """
    client = get_client()
    
    if not client.connected:
        message = "Not connected to Unity. Please check the Unity connection"
        logger.error(message)
        try:
            await client.connect()
            if not client.connected:
                raise Exception("Failed to connect to Unity")
        except Exception as e:
            logger.error(f"Reconnection failed: {str(e)}")
            return {"error": message}
            
    # Check if the tool exists
    exists = await client.has_command(tool_name)
    if not exists:
        message = f"Tool {tool_name} does not exist in Unity schema"
        logger.error(message)
        return {"error": message}
        
    # Invoke the tool
    try:
        logger.info(f"Invoking dynamic tool {tool_name} with parameters: {json.dumps(parameters or {})}")
        result = await client.send_command(tool_name, parameters or {})
        return result
    except Exception as e:
        message = f"Error invoking tool {tool_name}: {str(e)}"
        logger.error(message)
        return {"error": message}