"""Dynamic tool registration from Unity schema"""

import logging
import inspect
import json
from typing import Any, Dict, List, Optional, Callable, Awaitable
from mcp.server.fastmcp import FastMCP, Context
from server.unity_websocket_client import UnityWebSocketClient, get_client
from server.mcp.unity_client_util import execute_unity_operation

logger = logging.getLogger("dynamic_tools")

class DynamicToolManager:
    """
    Manager for dynamically registering tools based on Unity schema.
    """
    
    def __init__(self, mcp: FastMCP):
        """
        Initialize the dynamic tool manager.
        
        Args:
            mcp: FastMCP instance
        """
        self.mcp = mcp
        self.client = get_client()
        self.registered_tools: Dict[str, str] = {}
        self.registered_resources: Dict[str, str] = {}
        
    async def register_from_schema(self) -> bool:
        """
        Register all tools and resources from the Unity schema.
        
        Returns:
            True if successful, False otherwise
        """
        logger.info("Fetching Unity schema for dynamic tool registration...")
        
        try:
            # Get schema from Unity
            schema_result = await self.client.get_schema()
            
            if not schema_result or not isinstance(schema_result, dict):
                logger.error(f"Invalid schema returned: {schema_result}")
                return False
                
            # Process tools
            tools = schema_result.get('tools', [])
            for tool in tools:
                await self._register_tool(tool)
                
            # Process resources
            resources = schema_result.get('resources', [])
            for resource in resources:
                await self._register_resource(resource)
                
            logger.info(f"Dynamic registration complete: {len(self.registered_tools)} tools, {len(self.registered_resources)} resources")
            return True
            
        except Exception as e:
            logger.error(f"Error registering from schema: {str(e)}")
            return False
            
    async def _register_tool(self, tool_schema: Dict[str, Any]) -> None:
        """
        Register a tool from schema.
        
        Args:
            tool_schema: Tool schema from Unity
        """
        tool_name = tool_schema.get('name')
        if not tool_name:
            logger.warning("Tool without name found in schema, skipping")
            return
            
        # Skip if already registered
        if tool_name in self.registered_tools:
            logger.debug(f"Tool {tool_name} already registered, skipping")
            return
            
        description = tool_schema.get('description', f"Unity tool: {tool_name}")
        
        # Create the dynamic tool function
        async def dynamic_tool(*args, **kwargs) -> Any:
            ctx = kwargs.get('ctx')
            if not ctx:
                for arg in args:
                    if isinstance(arg, Context):
                        ctx = arg
                        break
                        
            if not ctx:
                logger.error(f"No context found for {tool_name}")
                raise ValueError("No context provided")
                
            # Extract parameters based on the input schema
            params = {}
            sig = inspect.signature(dynamic_tool)
            param_names = [p for p in sig.parameters if p != 'ctx']
            
            # Map the positional args to named parameters
            for i, arg_name in enumerate(param_names):
                if i < len(args):
                    params[arg_name] = args[i]
                    
            # Add any keyword args
            for k, v in kwargs.items():
                if k != 'ctx':
                    params[k] = v
                    
            try:
                ctx.info(f"Executing dynamic tool {tool_name} with params: {json.dumps(params)}")
                result = await execute_unity_operation(
                    f"dynamic tool {tool_name}",
                    lambda client: client.send_command(tool_name, params),
                    ctx,
                    f"Error executing {tool_name}"
                )
                return result
            except Exception as e:
                ctx.error(f"Error in dynamic tool {tool_name}: {str(e)}")
                return {"error": str(e)}
                
        # Customize the function based on input schema
        input_schema = tool_schema.get('inputSchema', {})
        properties = input_schema.get('properties', {})
        required = input_schema.get('required', [])
        
        # Create parameter annotations
        parameters = []
        for param_name, param_info in properties.items():
            param_type = param_info.get('type', 'string')
            param_required = param_name in required
            parameters.append((param_name, param_info.get('description', ''), param_type, param_required))
            
        # Register the tool with FastMCP
        # We need to create a closure to capture the parameters
        def register_dynamic_tool(mcp: FastMCP, tool_func, tool_name: str, description: str, parameters: List) -> None:
            # Create decorated function with proper parameters
            decorated = mcp.tool(name=tool_name, description=description)
            
            # Apply the decoration
            final_func = decorated(tool_func)
            
            # Store reference to the registered tool
            self.registered_tools[tool_name] = description
            logger.info(f"Registered dynamic tool: {tool_name}")
            
        # Register the tool
        register_dynamic_tool(self.mcp, dynamic_tool, tool_name, description, parameters)
            
    async def _register_resource(self, resource_schema: Dict[str, Any]) -> None:
        """
        Register a resource from schema.
        
        Args:
            resource_schema: Resource schema from Unity
        """
        resource_name = resource_schema.get('name')
        if not resource_name:
            logger.warning("Resource without name found in schema, skipping")
            return
            
        # Skip if already registered
        if resource_name in self.registered_resources:
            logger.debug(f"Resource {resource_name} already registered, skipping")
            return
            
        url_pattern = resource_schema.get('urlPattern')
        if not url_pattern:
            logger.warning(f"Resource {resource_name} has no URL pattern, skipping")
            return
            
        description = resource_schema.get('description', f"Unity resource: {resource_name}")
        
        # Extract parameters from URL pattern
        param_names = []
        parts = url_pattern.split('/')
        for part in parts:
            if part.startswith('{') and part.endswith('}'):
                param_name = part[1:-1]
                param_names.append(param_name)
                
        # Create the dynamic resource function
        async def dynamic_resource(*args, **kwargs) -> Any:
            ctx = kwargs.get('ctx')
            if not ctx:
                for arg in args:
                    if isinstance(arg, Context):
                        ctx = arg
                        break
                        
            if not ctx:
                logger.error(f"No context found for {resource_name}")
                raise ValueError("No context provided")
                
            # Construct URL with parameters
            url = url_pattern
            params = {}
            
            # Map positional args to URL parameters
            for i, param_name in enumerate(param_names):
                if i < len(args):
                    value = args[i]
                    url = url.replace(f"{{{param_name}}}", str(value))
                    params[param_name] = value
                    
            # Add any keyword args
            for k, v in kwargs.items():
                if k != 'ctx' and k in param_names:
                    url = url.replace(f"{{{k}}}", str(v))
                    params[k] = v
                    
            try:
                ctx.info(f"Accessing dynamic resource {resource_name} with URL: {url}")
                
                # For now, we'll use a generic command to access the resource
                # In the future, this could be implemented as a proper REST resource
                result = await execute_unity_operation(
                    f"dynamic resource {resource_name}",
                    lambda client: client.send_command(f"access_resource", {
                        "resource_name": resource_name,
                        "parameters": params
                    }),
                    ctx,
                    f"Error accessing resource {resource_name}"
                )
                return result
            except Exception as e:
                ctx.error(f"Error in dynamic resource {resource_name}: {str(e)}")
                return {"error": str(e)}
                
        # Register the resource with FastMCP
        # We need to create a closure to capture the parameters
        def register_dynamic_resource(mcp: FastMCP, resource_func, resource_name: str, 
                                    url_pattern: str, description: str) -> None:
            # Register with mcp.resource
            decorated = mcp.resource(url_pattern, description=description)
            
            # Apply the decoration
            final_func = decorated(resource_func)
            
            # Store reference to the registered resource
            self.registered_resources[resource_name] = url_pattern
            logger.info(f"Registered dynamic resource: {resource_name} with URL pattern: {url_pattern}")
            
        # Register the resource
        register_dynamic_resource(self.mcp, dynamic_resource, resource_name, url_pattern, description)
            
# Singleton instance for easy access
_instance: Optional[DynamicToolManager] = None

def get_manager(mcp: FastMCP) -> DynamicToolManager:
    """
    Get the dynamic tool manager instance.
    
    Args:
        mcp: FastMCP instance
        
    Returns:
        Dynamic tool manager instance
    """
    global _instance
    if _instance is None:
        _instance = DynamicToolManager(mcp)
    return _instance