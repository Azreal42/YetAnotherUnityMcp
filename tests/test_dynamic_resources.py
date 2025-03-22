"""Integration tests for dynamic resource implementation"""

import asyncio
import logging
import sys
import json
from mcp.server.fastmcp import FastMCP, Context
from server.dynamic_tool_invoker import invoke_dynamic_resource, invoke_dynamic_tool
from server.dynamic_tools import DynamicToolManager, get_manager
from server.unity_socket_client import get_client

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("test_dynamic_resources")

# Create FastMCP instance
mcp = FastMCP("Test Dynamic Resources", description="Test dynamic resource registration")

async def test_resource_registration_and_invocation():
    """Test resource registration and invocation with all parameter types"""
    client = get_client()
    
    # Connect to Unity
    logger.info("Connecting to Unity...")
    connected = await client.connect()
    if not connected:
        logger.error("Failed to connect to Unity")
        return
        
    logger.info("Connected to Unity")
    
    try:
        # Get schema
        logger.info("Getting schema...")
        schema = await client.get_schema()
        
        # Parse schema if needed
        if isinstance(schema, str):
            logger.info("Schema returned as string, parsing JSON...")
            try:
                schema = json.loads(schema)
            except json.JSONDecodeError:
                logger.error("Failed to parse schema JSON")
                return
        
        # Get dynamic tool manager
        logger.info("Creating dynamic tool manager...")
        manager = get_manager(mcp)
        
        # Register resources from schema
        logger.info("Registering resources from schema...")
        result = await manager.register_from_schema()
        
        if not result:
            logger.error("Failed to register resources from schema")
            return
            
        # Log registered resources
        logger.info(f"Successfully registered {len(manager.registered_resources)} resources")
        for name, url_pattern in manager.registered_resources.items():
            logger.info(f"  - {name}: {url_pattern}")
            
        # Test resources with different parameter patterns
        
        # Group resources by parameter count
        no_param_resources = []
        single_param_resources = {}  # param_name -> resource_name
        multi_param_resources = {}   # resource_name -> [param_names]
        
        for name, url_pattern in manager.registered_resources.items():
            # Extract parameters from URL pattern
            param_names = []
            parts = url_pattern.split('/')
            for part in parts:
                if part.startswith('{') and part.endswith('}'):
                    param_name = part[1:-1]
                    param_names.append(param_name)
                    
            if not param_names:
                no_param_resources.append(name)
            elif len(param_names) == 1:
                single_param_resources[param_names[0]] = name
            else:
                multi_param_resources[name] = param_names
                
        # Test no-parameter resources
        if no_param_resources:
            logger.info(f"Testing {len(no_param_resources)} no-parameter resources")
            for resource_name in no_param_resources:
                logger.info(f"  - Invoking {resource_name}")
                try:
                    result = await invoke_dynamic_resource(resource_name)
                    logger.info(f"    Result: {truncate_result(result)}")
                except Exception as e:
                    logger.error(f"    Error: {str(e)}")
        else:
            logger.info("No parameter-less resources available for testing")
                
        # Test single-parameter resources
        if single_param_resources:
            logger.info(f"Testing {len(single_param_resources)} single-parameter resources")
            for param_name, resource_name in single_param_resources.items():
                logger.info(f"  - Invoking {resource_name} with parameter {param_name}")
                
                # Generate test value based on parameter name
                param_value = generate_test_value(param_name)
                
                try:
                    result = await invoke_dynamic_resource(resource_name, {param_name: param_value})
                    logger.info(f"    Result with {param_name}={param_value}: {truncate_result(result)}")
                except Exception as e:
                    logger.error(f"    Error: {str(e)}")
        else:
            logger.info("No single-parameter resources available for testing")
                    
        # Test multi-parameter resources
        if multi_param_resources:
            logger.info(f"Testing {len(multi_param_resources)} multi-parameter resources")
            for resource_name, param_names in multi_param_resources.items():
                logger.info(f"  - Invoking {resource_name} with parameters {param_names}")
                
                # Generate test parameters
                params = {}
                for param_name in param_names:
                    params[param_name] = generate_test_value(param_name)
                
                # Full parameters test
                try:
                    logger.info(f"    Testing with all parameters: {json.dumps(params)}")
                    result = await invoke_dynamic_resource(resource_name, params)
                    logger.info(f"    Result: {truncate_result(result)}")
                except Exception as e:
                    logger.error(f"    Error: {str(e)}")
                    
                # Missing parameter test (if we have at least 2 parameters)
                if len(param_names) >= 2:
                    missing_params = params.copy()
                    missing_key = param_names[-1]
                    del missing_params[missing_key]
                    
                    logger.info(f"    Testing with missing parameter {missing_key}: {json.dumps(missing_params)}")
                    try:
                        result = await invoke_dynamic_resource(resource_name, missing_params)
                        logger.info(f"    Result with missing parameter: {truncate_result(result)}")
                    except Exception as e:
                        logger.info(f"    Error with missing parameter (expected): {str(e)}")
        else:
            logger.info("No multi-parameter resources available for testing")
            
        logger.info("All resource tests completed")
        
    except Exception as e:
        logger.error(f"Test failed: {str(e)}")
        import traceback
        logger.error(traceback.format_exc())
    finally:
        # Disconnect
        logger.info("Disconnecting from Unity...")
        await client.disconnect()
        logger.info("Disconnected from Unity")

def generate_test_value(param_name):
    """Generate a test value based on parameter name"""
    if param_name in ["max_logs", "count", "limit", "size", "max"]:
        return 5
    elif param_name in ["id", "object_id", "entity_id"]:
        return "test_object_01"
    elif param_name in ["name", "object_name", "scene_name"]:
        return "TestScene"
    elif param_name in ["property", "property_name", "attribute"]:
        return "position"
    elif param_name in ["detail_level", "quality", "level"]:
        return "high"
    else:
        return f"test_value_for_{param_name}"

def truncate_result(result, max_length=100):
    """Truncate result for logging"""
    result_str = str(result)
    if len(result_str) > max_length:
        return result_str[:max_length] + "..."
    return result_str

async def test_custom_resource_parameters():
    """
    Test custom resource parameter patterns
    
    This test manually registers resources with various parameter patterns and tests them.
    """
    # Create dynamic tool manager
    logger.info("Creating tool manager for custom resource tests...")
    manager = DynamicToolManager(mcp)
    
    # Mock client for testing
    client = get_client()
    if not client.connected:
        logger.info("Connecting client for custom resource tests...")
        connected = await client.connect()
        if not connected:
            logger.error("Failed to connect to Unity")
            return
    
    try:
        # Define test cases - map of URL patterns to expected parameter sets
        test_cases = {
            "unity://test/simple": [],
            "unity://test/single/{param}": ["param"],
            "unity://test/two/{first}/{second}": ["first", "second"],
            "unity://test/optional/{name}?detail={detail}": ["name", "detail"],
            "unity://test/complex/{id}/properties/{property}": ["id", "property"],
            "unity://test/mixed/{a}/{b}/{c}": ["a", "b", "c"]
        }
        
        # Mock schema for registration
        schema = {
            "resources": []
        }
        
        # Add resources to schema
        for url_pattern, params in test_cases.items():
            resource_name = f"test_{'_'.join(params) if params else 'no_params'}"
            schema["resources"].append({
                "name": resource_name,
                "description": f"Test resource with {len(params)} parameters",
                "urlPattern": url_pattern
            })
            
        # Register resources
        for resource in schema["resources"]:
            await manager._register_resource(resource)
            
        # Test each resource
        for url_pattern, param_names in test_cases.items():
            resource_name = f"test_{'_'.join(param_names) if param_names else 'no_params'}"
            logger.info(f"Testing custom resource: {resource_name}")
            logger.info(f"  URL Pattern: {url_pattern}")
            logger.info(f"  Parameters: {param_names}")
            
            # Create test parameters
            params = {}
            for param in param_names:
                params[param] = generate_test_value(param)
                
            # Invoke the resource
            try:
                logger.info(f"  Invoking with parameters: {json.dumps(params)}")
                result = await invoke_dynamic_resource(resource_name, params)
                logger.info(f"  Result: {truncate_result(str(result))}")
            except Exception as e:
                logger.error(f"  Error: {str(e)}")
                
    except Exception as e:
        logger.error(f"Custom resource test failed: {str(e)}")
        import traceback
        logger.error(traceback.format_exc())
    finally:
        # Disconnect
        logger.info("Disconnecting from Unity...")
        await client.disconnect()
        logger.info("Disconnected from Unity")

if __name__ == "__main__":
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        
    # Choose which test to run
    if len(sys.argv) > 1 and sys.argv[1] == "--custom":
        asyncio.run(test_custom_resource_parameters())
    else:
        asyncio.run(test_resource_registration_and_invocation())