"""Utility for invoking dynamically registered tools and resources directly"""

import logging
import json
from typing import Any, Dict, Optional
from server.unity_socket_client import get_client
from server.connection_manager import get_unity_connection_manager

logger = logging.getLogger("dynamic_invoker")

async def invoke_dynamic_tool(tool_name: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
    """
    Invoke a dynamically registered tool directly with automatic reconnection.
    
    Args:
        tool_name: Name of the tool to invoke
        parameters: Tool parameters
        
    Returns:
        Tool result
    """
    client = get_client()
    connection_manager = get_unity_connection_manager()
    
    # Perform connection with retry logic
    if not client.connected:
        logger.info("Not connected to Unity, attempting to reconnect...")
        connected = await connection_manager.reconnect()
        if not connected:
            message = "Not connected to Unity and reconnection failed. Please check the Unity connection"
            logger.error(message)
            return {"error": message}
            
    # Create the operation to execute with reconnection
    async def execute_tool_operation():
        # Check if the tool exists
        exists = await client.has_command(tool_name)
        if not exists:
            message = f"Tool {tool_name} does not exist in Unity schema"
            logger.error(message)
            return {"error": message}
            
        # Invoke the tool
        logger.info(f"Invoking dynamic tool {tool_name} with parameters: {json.dumps(parameters or {})}")
        return await client.send_command(tool_name, parameters or {})
    
    # Execute with reconnection
    try:
        return await connection_manager.execute_with_reconnect(execute_tool_operation)
    except Exception as e:
        message = f"Error invoking tool {tool_name}: {str(e)}"
        logger.error(message)
        return {"error": message}

async def invoke_dynamic_resource(resource_name: str, parameters: Optional[Dict[str, Any]] = None) -> Any:
    """
    Invoke a dynamically registered resource directly with automatic reconnection.
    
    Args:
        resource_name: Name of the resource to access
        parameters: Resource parameters
        
    Returns:
        Resource content
    """
    client = get_client()
    connection_manager = get_unity_connection_manager()
    
    # Perform connection with retry logic
    if not client.connected:
        logger.info("Not connected to Unity, attempting to reconnect...")
        connected = await connection_manager.reconnect()
        if not connected:
            message = "Not connected to Unity and reconnection failed. Please check the Unity connection"
            logger.error(message)
            return {"error": message}
            
    # Create the operation to execute with reconnection
    async def execute_resource_operation():
        logger.info(f"Accessing dynamic resource {resource_name} with parameters: {json.dumps(parameters or {})}")
        return await client.send_command("access_resource", {
            "resource_name": resource_name,
            "parameters": parameters or {}
        })
    
    # Execute with reconnection
    try:
        return await connection_manager.execute_with_reconnect(execute_resource_operation)
    except Exception as e:
        message = f"Error accessing resource {resource_name}: {str(e)}"
        logger.error(message)
        return {"error": message}