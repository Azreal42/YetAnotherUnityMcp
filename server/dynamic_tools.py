"""Dynamic tool registration from Unity schema"""

import logging
import inspect
import json
import re
import threading
from typing import Any, Dict, List, Optional, Callable, Awaitable, Set, Union

from mcp.server.fastmcp.resources import FunctionResource
from mcp.server.fastmcp.tools import Tool
from mcp.server.fastmcp import FastMCP, Context
from pydantic import AnyUrl

from server.connection_manager import UnityConnectionManager
from server.unity_client_util import UnityClientUtil

# Add ResourceContext class to store context in thread-local storage
class ResourceContext:
    """Thread-local and task-local storage for resource context"""
    _thread_local = threading.local()
    _task_contexts = {}  # Dictionary to store task_id -> context mapping
    
    @classmethod
    def get_current_ctx(cls) -> Optional[Context]:
        """Get the current context from thread-local or task-local storage"""
        # First try to get task-specific context if in an asyncio task
        import asyncio
        try:
            current_task = asyncio.current_task()
            if current_task and id(current_task) in cls._task_contexts:
                return cls._task_contexts[id(current_task)]
        except (RuntimeError, ImportError):
            # If we're not in an asyncio event loop or asyncio is not available
            pass
            
        # Fall back to thread-local storage
        return getattr(cls._thread_local, "ctx", None)
    
    @classmethod
    def set_current_ctx(cls, ctx: Optional[Context]) -> None:
        """Set the current context in both thread-local and task-local storage"""
        # Store in thread-local storage
        cls._thread_local.ctx = ctx
        
        # Also store in task-specific storage if in an asyncio task
        import asyncio
        try:
            current_task = asyncio.current_task()
            if current_task:
                if ctx is None and id(current_task) in cls._task_contexts:
                    # Remove task from contexts when setting to None
                    del cls._task_contexts[id(current_task)]
                else:
                    cls._task_contexts[id(current_task)] = ctx
        except (RuntimeError, ImportError):
            # If we're not in an asyncio event loop or asyncio is not available
            pass
    
    @classmethod
    def with_context(cls, ctx: Context):
        """Context manager for setting and restoring context"""
        class ContextManager:
            def __init__(self, ctx):
                self.ctx = ctx
                self.prev_ctx = None
                
            def __enter__(self):
                self.prev_ctx = cls.get_current_ctx()
                cls.set_current_ctx(self.ctx)
                return self.ctx
                
            def __exit__(self, exc_type, exc_val, exc_tb):
                cls.set_current_ctx(self.prev_ctx)
                
        return ContextManager(ctx)
        
    @classmethod
    def clear_all_contexts(cls):
        """Clear all stored contexts - useful for testing and cleanup"""
        # Clear thread-local storage
        if hasattr(cls._thread_local, "ctx"):
            delattr(cls._thread_local, "ctx")
        
        # Clear task contexts dictionary
        cls._task_contexts.clear()

logger = logging.getLogger("dynamic_tools")

class DynamicToolManager:
    """
    Manager for dynamically registering tools and resources based on Unity schema.
    """
    
    def __init__(self, mcp: FastMCP, connection_manager: UnityConnectionManager):
        self.mcp = mcp
        self.connection_manager = connection_manager
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
            schema_result = await self.connection_manager.client.get_schema()
            
            # Debug the schema structure
            logger.debug(f"Schema result type: {type(schema_result)}")
            if isinstance(schema_result, dict):
                logger.debug(f"Schema result keys: {schema_result.keys()}")
            
            # Process the schema - handle various formats
            processed_schema = await self._process_schema(schema_result)
            
            if not processed_schema or not isinstance(processed_schema, dict):
                logger.error(f"Failed to process schema: {processed_schema}")
                return False
                
            if 'tools' not in processed_schema or 'resources' not in processed_schema:
                logger.error(f"Processed schema is missing tools or resources")
                return False
                
            # Process tools
            tools = processed_schema.get('tools', [])
            for tool in tools:
                try:
                    await self._register_tool(tool)
                except Exception as e:
                    logger.error(f"Error registering tool: {str(e)}")
                    # Continue with other tools
                    
            # Process resources
            resources = processed_schema.get('resources', [])
            for resource in resources:
                try:
                    await self._register_resource(resource)
                except Exception as e:
                    logger.error(f"Error registering resource: {str(e)}")
                    # Continue with other resources
                    
            logger.info(f"Dynamic registration complete: {len(self.registered_tools)} tools, {len(self.registered_resources)} resources")
            return True
            
        except Exception as e:
            logger.error(f"Error registering from schema: {str(e)}")
            return False
    
    async def _process_schema(self, schema_result: Any) -> Dict[str, Any]:
        """Process the schema result and extract the actual schema structure"""
        schema = schema_result
        
        # If schema is a string, try to parse it as JSON
        if isinstance(schema, str):
            try:
                schema = json.loads(schema)
            except json.JSONDecodeError as e:
                logger.error(f"Failed to parse schema JSON: {e}")
                return {}
        
        # Handle different schema structures
        if isinstance(schema, dict):
            # Case 1: Schema already has tools and resources at top level
            if 'tools' in schema or 'resources' in schema:
                logger.info("Schema has tools and resources directly at the top level")
                return schema
                
            # Case 2: Schema in a result wrapper
            if 'result' in schema:
                result = schema.get('result', {})
                
                # Case 2.1: Result directly contains tools and resources
                if isinstance(result, dict) and 'tools' in result and 'resources' in result:
                    logger.info("Schema in result field with tools and resources")
                    return result
                
                # Case 2.2: Result contains content array
                if isinstance(result, dict) and 'content' in result:
                    content = result.get('content', [])
                    
                    # Look for text content that contains tools and resources
                    for item in content:
                        if item.get('type') == 'text':
                            text = item.get('text', '')
                            
                            try:
                                parsed = json.loads(text)
                                if isinstance(parsed, dict) and 'tools' in parsed:
                                    logger.info("Found schema in content text")
                                    return parsed
                            except json.JSONDecodeError:
                                pass
            
            # Case 3: Schema directly in content array
            if 'content' in schema:
                content = schema.get('content', [])
                
                for item in content:
                    if item.get('type') == 'text':
                        text = item.get('text', '')
                        
                        try:
                            parsed = json.loads(text)
                            if isinstance(parsed, dict) and 'tools' in parsed:
                                logger.info("Found schema in top-level content text")
                                return parsed
                        except json.JSONDecodeError:
                            pass
        
        logger.error("Could not find valid schema structure")
        return {}
            
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
            
            # Get required parameters from schema (new MCP format uses required array)
            required_params = input_schema.get('required', [])
            logger.debug(f"Tool {tool_name} required parameters: {required_params}")
            
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
                result = await UnityClientUtil.execute_unity_operation(
                    self.connection_manager,
                    f"dynamic tool {tool_name}",
                    lambda: self.connection_manager.client.send_command(tool_name, params),
                    ctx,
                    f"Error executing {tool_name}"
                )
                return result
            except Exception as e:
                await ctx.error(f"Error in dynamic tool {tool_name}: {str(e)}")
                raise
                
        # Register the tool with FastMCP
        # Use the default Tool.from_function for simplicity
        # FastMCP will internally handle the required parameters
        mcp_tool = Tool.from_function(
            dynamic_tool, 
            name=tool_name, 
            description=description
        )
        
        # Get input schema for logging
        input_schema = tool_schema.get('inputSchema', {})
        
        # Log registration details
        logger.info(f"Registering dynamic tool {tool_name} with required parameters: {input_schema.get('required', [])}")
        self.mcp._tool_manager._tools[tool_name] = mcp_tool
        
        # Store reference to the registered tool
        self.registered_tools[tool_name] = description
        logger.info(f"Successfully registered dynamic tool: {tool_name}")
            
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
            
        # Check for both uri
        uri = resource_schema.get('uri')
        if not uri:
            logger.warning(f"Resource {resource_name} has no URI, skipping")
            return
            
        description = resource_schema.get('description', f"Unity resource: {resource_name}")
        mime_type = resource_schema.get('mimeType', 'application/json')
        
        # Extract parameter information from URI
        # This is useful for debugging and parameter mapping
        parameters = []
        parts = uri.split('/')
        for part in parts:
            if part.startswith('{') and part.endswith('}'):
                # Extract the parameter name without the braces
                param_name = part[1:-1]
                parameters.append(param_name)
                
        # Log the detected parameters
        if parameters:
            logger.info(f"Resource {resource_name} requires parameters: {parameters}")
            
            # Check for camelCase parameters that might cause issues
            camel_case_params = [p for p in parameters if any(c.isupper() for c in p)]
            if camel_case_params:
                # Log information about parameter name conversion
                logger.info(f"Resource {resource_name} uses camelCase parameters: {camel_case_params}")
                snake_case_examples = [self._camel_to_snake(p) for p in camel_case_params]
                logger.info(f"When accessing this resource, use snake_case in Python: {snake_case_examples}")
                logger.info(f"Parameters will be automatically converted back to camelCase when sent to Unity")
        
    
        # Store the resource in our registry with all relevant info
        self.registered_resources[resource_name] = {
            "uri": uri,
            "description": description,
            "uri_params": parameters
        }
        
        # Create a dynamic resource handler function
        async def dynamic_resource_handler(ctx: Context, *args, **kwargs):
            """Dynamic resource handler function"""
            # Convert positional args to named parameters
            param_dict = {}
            
            # Map positional args to parameters from URI
            for i, param_name in enumerate(parameters):
                if i < len(args):
                    param_dict[param_name] = args[i]
            
            # Add any keyword args
            param_dict.update(kwargs)
            
            # Check if all required parameters are provided
            if len(parameters) > 0 and len(param_dict) < len(parameters):
                missing_params = set(parameters) - set(param_dict.keys())
                raise TypeError(f"Missing required parameters for resource {resource_name}: {', '.join(missing_params)}")
            
            logger.info(f"Accessing dynamic resource {resource_name} with params: {param_dict}")
            
            try:
                # Execute the resource access
                result = await UnityClientUtil.execute_unity_operation(
                    self.connection_manager,
                    f"dynamic resource {resource_name}",
                    lambda: self.connection_manager.client.send_command("access_resource", {
                        "resource_name": resource_name,
                        "parameters": param_dict
                    }),
                    ctx,
                    f"Error accessing {resource_name}"
                )
                return result
            except Exception as e:
                logger.error(f"Error in dynamic resource {resource_name}: {str(e)}")
                raise
        
        # Create the FunctionResource with the required fn parameter
        try:
            resource = FunctionResource(
                uri=AnyUrl(uri),  # FastMCP uses uri even for parameterized URIs
                name=resource_name,
                description=description,
                mime_type=mime_type or "text/plain",
                fn=dynamic_resource_handler,  # Add the required fn parameter
            )
            self.mcp._resource_manager.add_resource(resource)
            
            # Add the function to our registry
            self.registered_resources[resource_name]["func"] = dynamic_resource_handler
            
            logger.info(f"Successfully registered dynamic resource: {resource_name}")
        except Exception as e:
            logger.error(f"Error registering resource: {e}")
        
        return
    
    @staticmethod
    def _camel_to_snake(name):
        """Convert a camelCase string to snake_case"""
        # Replace common patterns first (like 'Id' to '_id')
        name = name.replace("Id", "_id").replace("Name", "_name")
        # Handle the general case
        return ''.join(['_' + c.lower() if c.isupper() else c.lower() for c in name]).lstrip('_')

                