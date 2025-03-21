"""Test script for dynamic tool and resource registration"""

import asyncio
import logging
import sys
import json
from server.dynamic_tool_invoker import invoke_dynamic_tool, invoke_dynamic_resource
from server.dynamic_tools import DynamicToolManager
from server.unity_socket_client import get_client
from mcp.server.fastmcp import FastMCP

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("test_dynamic_tools")

# Create FastMCP instance
mcp = FastMCP("Test Dynamic Tools", description="Test dynamic tool registration")

async def main():
    """Main test function"""
    client = get_client()
    
    # Connect to Unity
    logger.info("Connecting to Unity...")
    connected = await client.connect()
    if not connected:
        logger.error("Failed to connect to Unity")
        return
        
    logger.info("Connected to Unity")
    
    # Get Unity info
    logger.info("Getting Unity info...")
    info = await client.get_unity_info()
    logger.info(f"Unity info: {json.dumps(info, indent=2)}")
    
    # Get schema
    logger.info("Getting schema...")
    schema = await client.get_schema()
    
    # Check if schema is a string (possibly JSON) and parse it if needed
    if isinstance(schema, str):
        logger.info("Schema returned as string, parsing JSON...")
        try:
            schema = json.loads(schema)
        except json.JSONDecodeError:
            logger.error("Failed to parse schema JSON")
            return
    
    # Save schema to file for debugging
    with open('schema_debug.json', 'w') as f:
        json.dump(schema, f, indent=2)
    logger.info("Schema saved to schema_debug.json for inspection")
    
    # Print tools from schema
    tools = schema.get('tools', [])
    logger.info(f"Found {len(tools)} tools in schema:")
    for tool in tools:
        logger.info(f"  - {tool.get('name')}: {tool.get('description')}")
        
    # Print resources from schema
    resources = schema.get('resources', [])
    logger.info(f"Found {len(resources)} resources in schema:")
    for resource in resources:
        logger.info(f"  - {resource.get('name')} ({resource.get('urlPattern')}): {resource.get('description')}")
        
    # Create dynamic tool manager
    logger.info("Creating dynamic tool manager...")
    manager = DynamicToolManager(mcp)
    
    # Register tools from schema
    logger.info("Registering tools from schema...")
    result = await manager.register_from_schema()
    
    if result:
        logger.info(f"Successfully registered {len(manager.registered_tools)} tools and {len(manager.registered_resources)} resources")
        
        # List registered tools
        logger.info("Registered tools:")
        for name, desc in manager.registered_tools.items():
            logger.info(f"  - {name}: {desc}")
            
        # List registered resources
        logger.info("Registered resources:")
        for name, url_pattern in manager.registered_resources.items():
            logger.info(f"  - {name}: {url_pattern}")
            
        # Test invoking various components
        await test_tools_and_resources(manager)
    else:
        logger.error("Failed to register tools from schema")
        
    # Disconnect
    logger.info("Disconnecting from Unity...")
    await client.disconnect()
    logger.info("Disconnected from Unity")

async def test_tools_and_resources(manager):
    """Test tools and resources"""
    
    # Test a dynamic tool
    if "execute_code" in manager.registered_tools:
        logger.info("TESTING TOOL: execute_code")
        code = "Debug.Log(\\\"Hello from dynamic tool\\\"); return 42;"
        result = await invoke_dynamic_tool("execute_code", {"code": code})
        logger.info(f"Tool result: {json.dumps(result, indent=2)}")
    
    # Test a no-parameter resource
    if "get_unity_info" in manager.registered_resources:
        logger.info("TESTING RESOURCE: get_unity_info (no parameters)")
        result = await invoke_dynamic_resource("get_unity_info")
        logger.info(f"Resource result: {json.dumps(result, indent=2)}")
    
    # Test a single-parameter resource
    if "get_logs" in manager.registered_resources:
        logger.info("TESTING RESOURCE: get_logs (single parameter)")
        result = await invoke_dynamic_resource("get_logs", {"max_logs": 5})
        logger.info(f"Resource result (max_logs=5): {json.dumps(result, indent=2)}")
    
    # Create a synthetic multi-parameter resource for testing
    logger.info("TESTING: Creating synthetic multi-parameter resource")
    
    # Create a custom two-parameter resource
    # For this test, we'll create a fake resource that would require multiple parameters
    # This helps test our multi-parameter code even if the actual Unity schema doesn't have any
    
    multi_param_resource_name = "test_multi_param"
    url_pattern = "unity://test/{param1}/{param2}"
    
    # Check if we have a real multi-parameter resource
    has_multi_param = False
    for name, url_pattern in manager.registered_resources.items():
        if url_pattern.count('{') > 1:
            logger.info(f"TESTING RESOURCE: {name} (multi-parameter)")
            has_multi_param = True
            
            # Extract parameter names from URL pattern
            param_names = []
            parts = url_pattern.split('/')
            for part in parts:
                if part.startswith('{') and part.endswith('}'):
                    param_name = part[1:-1]
                    param_names.append(param_name)
            
            # Create test parameters
            params = {}
            for i, param in enumerate(param_names):
                params[param] = f"test_value_{i}"
                
            logger.info(f"Invoking multi-param resource with parameters: {json.dumps(params)}")
            result = await invoke_dynamic_resource(name, params)
            logger.info(f"Multi-param resource result: {json.dumps(result, indent=2)}")
            break
    
    if not has_multi_param:
        # Let's tell the user we're testing with a simulated resource since
        # we didn't find a real multi-parameter resource
        logger.info("No actual multi-parameter resources found in Unity schema")
        
        # Test the resource context passing with a simple pass-through function 
        logger.info("TESTING: Resource context passing with a multi-parameter simulated resource")
        
        # Create parameters that would be used with a multi-parameter resource
        test_params = {
            "param1": "test_value_1",
            "param2": "test_value_2",
            "param3": "test_value_3"  # Extra parameter to test handling of mismatched parameters
        }
        
        # Call with our test parameters
        logger.info(f"Invoking simulated multi-param resource with parameters: {json.dumps(test_params)}")
        simulate_result = await invoke_dynamic_resource("test_multi_param", test_params)
        logger.info(f"Simulated multi-param result: {json.dumps(simulate_result, indent=2)}")
    
    # Try invoking with missing parameters
    if has_multi_param and len(param_names) > 1:
        # Remove the last parameter
        missing_params = params.copy()
        missing_key = list(missing_params.keys())[-1]
        del missing_params[missing_key]
        
        logger.info(f"TESTING: Multi-param resource with missing parameter {missing_key}")
        logger.info(f"Invoking with incomplete parameters: {json.dumps(missing_params)}")
        try:
            result = await invoke_dynamic_resource(name, missing_params)
            logger.info(f"Result with missing parameter: {json.dumps(result, indent=2)}")
        except Exception as e:
            logger.error(f"Error with missing parameter (expected): {str(e)}")
    
    # Try invoking an unknown tool/resource
    logger.info("TESTING: non-existent tool")
    result = await invoke_dynamic_tool("non_existent_tool", {})
    logger.info(f"Non-existent tool result: {json.dumps(result, indent=2)}")
    
    logger.info("TESTING: non-existent resource")
    result = await invoke_dynamic_resource("non_existent_resource", {})
    logger.info(f"Non-existent resource result: {json.dumps(result, indent=2)}")

if __name__ == "__main__":
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        
    # Run the test
    asyncio.run(main())