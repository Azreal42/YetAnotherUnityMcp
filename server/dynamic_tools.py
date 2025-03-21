"""Dynamic tool registration from Unity schema"""

import logging
import inspect
import json
import re
from typing import Any, Dict, List, Optional, Callable, Awaitable, Set

from mcp.server.fastmcp import FastMCP, Context
from server.unity_socket_client import UnitySocketClient, get_client
from server.unity_client_util import execute_unity_operation

logger = logging.getLogger("dynamic_tools")

class DynamicToolManager:
    """
    Manager for dynamically registering tools and resources based on Unity schema.
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
            
            # Handle string schema (JSON string)
            if isinstance(schema_result, str):
                logger.info("Schema returned as string, parsing JSON...")
                try:
                    schema_result = json.loads(schema_result)
                except json.JSONDecodeError as e:
                    logger.error(f"Failed to parse schema JSON: {e}")
                    return False
            
            # Validate schema format
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
        async def dynamic_tool(ctx: Context, *args, **kwargs) -> Any:
            """Dynamic tool execution function"""
            # Extract parameters based on the input schema
            params = {}
            
            # Map positional args to parameters based on input schema
            input_schema = tool_schema.get('inputSchema', {})
            properties = input_schema.get('properties', {})
            param_names = list(properties.keys())
            
            # Map the positional args to named parameters
            for i, param_name in enumerate(param_names):
                if i < len(args):
                    params[param_name] = args[i]
                    
            # Add any keyword args
            for k, v in kwargs.items():
                if k != 'ctx':
                    params[k] = v
                    
            try:
                await ctx.info(f"Executing dynamic tool {tool_name} with params: {json.dumps(params)}")
                result = await execute_unity_operation(
                    f"dynamic tool {tool_name}",
                    lambda client: client.send_command(tool_name, params),
                    ctx,
                    f"Error executing {tool_name}"
                )
                return result
            except Exception as e:
                await ctx.error(f"Error in dynamic tool {tool_name}: {str(e)}")
                raise
                
        # Register the tool with FastMCP
        decorated = self.mcp.tool(name=tool_name, description=description)
        final_func = decorated(dynamic_tool)
        
        # Store reference to the registered tool
        self.registered_tools[tool_name] = description
        logger.info(f"Registered dynamic tool: {tool_name}")
            
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
        
        # Extract parameters from URL pattern using regex
        param_pattern = r"\{([^}]+)\}"
        uri_params = re.findall(param_pattern, url_pattern)
        
        # Create a wrapper to handle execution, since we can't directly use the context
        # in resource handlers (due to FastMCP's validation)
        async def execute_resource_handler(resource_name: str, parameters: Dict[str, Any] = None):
            """Generic resource handler that will be wrapped"""
            if parameters is None:
                parameters = {}
                
            # Get access to the current FastMCP context at execution time
            # Important: This only works during request processing
            current_ctx = self.mcp.get_context()
            
            try:
                # Log access
                await current_ctx.info(f"Accessing resource {resource_name} with parameters: {json.dumps(parameters)}")
                
                # Execute the command via our utility
                result = await execute_unity_operation(
                    f"dynamic resource {resource_name}",
                    lambda client: client.send_command("access_resource", {
                        "resource_name": resource_name,
                        "parameters": parameters
                    }),
                    current_ctx,
                    f"Error accessing resource {resource_name}"
                )
                return result
            except Exception as e:
                await current_ctx.error(f"Error in resource {resource_name}: {str(e)}")
                raise
        
        # Register the resource based on parameter count
        if not uri_params:
            # No parameters resource (e.g., unity://info)
            @self.mcp.resource(url_pattern, description=description)
            async def no_params_handler():
                return await execute_resource_handler(resource_name)
                
            self.registered_resources[resource_name] = url_pattern
            logger.info(f"Registered parameterless resource: {resource_name} with URL: {url_pattern}")
            
        elif len(uri_params) == 1:
            # Single parameter resource (e.g., unity://logs/{max_logs})
            param_name = uri_params[0]
            
            # Create a function dynamically with the correct parameter name
            exec_globals = {'execute_resource_handler': execute_resource_handler, 'resource_name': resource_name}
            
            exec(f"""
async def single_param_handler({param_name}):
    return await execute_resource_handler(resource_name, {{{param_name!r}: {param_name}}})
""", exec_globals)
            
            # Register the handler with FastMCP
            self.mcp.resource(url_pattern, description=description)(exec_globals['single_param_handler'])
            self.registered_resources[resource_name] = url_pattern
            logger.info(f"Registered single-parameter resource: {resource_name} with URL: {url_pattern}")
            
        else:
            # Multi-parameter resource (e.g., unity://object/{id}/property/{property_name})
            param_list = ", ".join(uri_params)
            param_dict = ", ".join([f"{p!r}: {p}" for p in uri_params])
            
            # Create a function dynamically with exactly the right parameter names
            exec_globals = {'execute_resource_handler': execute_resource_handler, 'resource_name': resource_name}
            
            exec(f"""
async def multi_param_handler({param_list}):
    return await execute_resource_handler(resource_name, {{{param_dict}}})
""", exec_globals)
            
            # Register the handler with FastMCP
            self.mcp.resource(url_pattern, description=description)(exec_globals['multi_param_handler'])
            self.registered_resources[resource_name] = url_pattern
            logger.info(f"Registered multi-parameter resource: {resource_name} with URL: {url_pattern}")
                
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