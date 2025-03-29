"""Dynamic tool and resource invocation utilities"""

import logging
import json
from typing import Dict, Any, Optional

from mcp.server.fastmcp import Context
from server.resource_context import ResourceContext
from server.connection_manager import UnityConnectionManager

logger = logging.getLogger("dynamic_tool_invoker")

class DynamicToolInvoker:
    def __init__(self, connection_manager: UnityConnectionManager):
        self.connection_manager = connection_manager

    async def invoke_tool(self, tool_name: str, params: Dict[str, Any], ctx: Optional[Context] = None) -> Any:
        """
        Invoke a dynamic tool by name.
        
        Args:
            tool_name: Name of the tool to invoke
            params: Parameters to pass to the tool
            ctx: Optional context to use
            client: Optional client for dependency injection
            
        Returns:
            Tool result
        """
        logger.info(f"Invoking dynamic tool {tool_name} with params: {json.dumps(params)}")
        
        # Get the current context or use the provided one
        context = ctx or ResourceContext.get_current_ctx()
        
        try:
            async def execute_tool():
                return await self.connection_manager.client.send_command(tool_name, params)
                
            # Execute with reconnection support
            result = await self.connection_manager.execute_with_reconnect(execute_tool)
            
            # Check if the result indicates an error
            if isinstance(result, dict):
                # Check for error status
                if result.get("status") == "error":
                    error_message = result.get("error", "Unknown error")
                    logger.error(f"Error response from tool {tool_name}: {error_message}")
                    
                    # Check for missing parameter errors
                    if any(keyword in error_message.lower() for keyword in 
                        ["required parameter", "missing parameter", "not provided", "parameter required"]):
                        raise ValueError(f"Missing required parameter for tool {tool_name}: {error_message}")
                    
                    # For other errors, continue with the error result
                
                # Check for error result content
                result_obj = result.get("result", {})
                if result_obj.get("isError") is True:
                    # Get error message from content if available
                    content = result_obj.get("content", [])
                    error_message = "Unknown error"
                    for item in content:
                        if item.get("type") == "text":
                            error_message = item.get("text", "Unknown error")
                            break
                    
                    logger.error(f"Error content from tool {tool_name}: {error_message}")
                    
                    # Check for missing parameter errors
                    if any(keyword in error_message.lower() for keyword in 
                        ["required parameter", "missing parameter", "not provided", "parameter required"]):
                        raise ValueError(f"Missing required parameter for tool {tool_name}: {error_message}")
            
            # Log success and return result
            logger.info(f"Successfully invoked tool {tool_name}")
            return result
        except Exception as e:
            # Log error
            error_message = str(e)
            logger.error(f"Error invoking tool {tool_name}: {error_message}")
            
            # Check for missing parameter errors - these should be re-raised
            if any(keyword in error_message.lower() for keyword in 
                ["required parameter", "missing parameter", "not provided", "parameter required"]):
                logger.error(f"Missing required parameter for tool {tool_name} - re-raising exception")
                raise ValueError(f"Missing required parameter for tool {tool_name}: {error_message}") from e
            
            # For other errors, return an error result object
            return {
                "result": {
                    "content": [
                        {
                            "type": "text",
                            "text": f"Error invoking tool {tool_name}: {error_message}"
                        }
                    ],
                    "isError": True
                }
            }

    async def invoke_resource(self, resource_name: str, params: Dict[str, Any] = None, ctx: Optional[Context] = None, client=None) -> Any:
        """
        Invoke a dynamic resource by name.
        
        Args:
            resource_name: Name of the resource to access
            params: Parameters to pass to the resource (optional)
            ctx: Optional context to use
            client: Optional client for dependency injection
            
        Returns:
            Resource result
        """
        params = params or {}
        logger.info(f"Invoking dynamic resource {resource_name} with params: {json.dumps(params)}")
        
        # Get the current context or use the provided one
        context = ctx or ResourceContext.get_current_ctx()

        
        # Normalize parameter names if needed
        normalized_params = self._normalize_resource_parameters(resource_name, params)
        if normalized_params != params:
            logger.info(f"Normalized parameters for {resource_name}: {json.dumps(normalized_params)}")
        
        try:
            async def access_resource():
                return await self.connection_manager.client.send_command("access_resource", {
                    "resource_name": resource_name,
                    "parameters": normalized_params
                })
                
            # Execute with reconnection support
            result = await self.connection_manager.execute_with_reconnect(access_resource)
            
            # Check if the result indicates an error
            if isinstance(result, dict):
                # Check for error status
                if result.get("status") == "error":
                    error_message = result.get("error", "Unknown error")
                    logger.error(f"Error response from resource {resource_name}: {error_message}")
                    
                    # Check for missing parameter errors
                    if any(keyword in error_message.lower() for keyword in 
                        ["required parameter", "missing parameter", "not provided", "parameter required"]):
                        raise ValueError(f"Missing required parameter for resource {resource_name}: {error_message}")
                    
                    # For other errors, continue with the error result
                
                # Check for error result content
                result_obj = result.get("result", {})
                if result_obj.get("isError") is True:
                    # Get error message from content if available
                    content = result_obj.get("content", [])
                    error_message = "Unknown error"
                    for item in content:
                        if item.get("type") == "text":
                            error_message = item.get("text", "Unknown error")
                            break
                    
                    logger.error(f"Error content from resource {resource_name}: {error_message}")
                    
                    # Check for missing parameter errors
                    if any(keyword in error_message.lower() for keyword in 
                        ["required parameter", "missing parameter", "not provided", "parameter required"]):
                        raise ValueError(f"Missing required parameter for resource {resource_name}: {error_message}")
            
            # Log success and return result
            logger.info(f"Successfully invoked resource {resource_name}")
            return result
        except Exception as e:
            # Log error
            error_message = str(e)
            logger.error(f"Error invoking resource {resource_name}: {error_message}")
            
            # Check for missing parameter errors - these should be re-raised
            if any(keyword in error_message.lower() for keyword in 
                ["required parameter", "missing parameter", "not provided", "parameter required"]):
                logger.error(f"Missing required parameter for resource {resource_name} - re-raising exception")
                raise ValueError(f"Missing required parameter for resource {resource_name}: {error_message}") from e
            
            # For other errors, return an error result object
            return {
                "result": {
                    "content": [
                        {
                            "type": "text",
                            "text": f"Error invoking resource {resource_name}: {error_message}"
                        }
                    ],
                    "isError": True
                }
            }

    @staticmethod
    def _normalize_resource_parameters(resource_name: str, params: Dict[str, Any]) -> Dict[str, Any]:
        """
        Normalize parameter names from snake_case to camelCase to match Unity's expected format.
        
        Args:
            resource_name: The name of the resource (unused, kept for backward compatibility)
            params: The parameters to normalize
            
        Returns:
            Normalized parameters dictionary with camelCase keys
        """
        # Simple snake_case to camelCase conversion
        normalized = {}
        
        for key, value in params.items():
            # Skip keys that are already camelCase
            if "_" not in key:
                normalized[key] = value
                continue
                
            # Convert snake_case to camelCase
            parts = key.split('_')
            camel_key = parts[0] + ''.join(part.capitalize() for part in parts[1:])
            normalized[camel_key] = value
        
        return normalized