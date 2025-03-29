"""Unit tests for resource parameter handling in dynamic_tools.py"""

import json
import pytest
import logging
import asyncio
import sys
from unittest.mock import AsyncMock, MagicMock, patch

from mcp.server.fastmcp import FastMCP, Context
from server.connection_manager import UnityConnectionManager
from server.dynamic_tools import DynamicToolManager, ResourceContext
from server.dynamic_tool_invoker import DynamicToolInvoker

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("test_resource_parameters")

# Test data
TEST_SCHEMA = {
    "tools": [
        {
            "name": "execute_code",
            "description": "Executes C# code in Unity",
            "inputSchema": {
                "type": "object",
                "properties": {
                    "code": {
                        "type": "string",
                        "description": "C# code to execute"
                    }
                },
                "required": ["code"]
            }
        }
    ],
    "resources": [
        {
            "name": "unity_info",
            "description": "Get Unity information",
            "uri": "unity://info"
        },
        {
            "name": "logs",
            "description": "Get Unity logs",
            "uri": "unity://logs/{max_logs}"
        },
        {
            "name": "object_properties",
            "description": "Get GameObject properties",
            "uri": "unity://gameobject/{id}/properties/{property_name}"
        },
        {
            "name": "scene",
            "description": "Get scene information with optional parameters",
            "uri": "unity://scene/{scene_name}/{detail_level}"
        }
    ]
}

# Create test fixtures

@pytest.fixture
def mock_client():
    """Create a mock Unity client"""
    client = AsyncMock()
    client.get_schema = AsyncMock(return_value=TEST_SCHEMA)
    client.send_command = AsyncMock(side_effect=lambda cmd, params: {
        "command": cmd,
        "params": params,
        "result": "success" 
    })
    client.connected = True
    client.has_command = AsyncMock(return_value=True)
    return client

@pytest.fixture
def mock_fastmcp():
    """Create a mock FastMCP instance"""
    mcp = MagicMock()
    
    # Make resource decorator track registered resources
    mcp.registered_resources = {}
    
    def resource_decorator(url_pattern, description=""):
        def decorator(func):
            # Store the registered resource
            resource_name = url_pattern.split('://')[-1].split('/')[0]
            mcp.registered_resources[resource_name] = {
                "url_pattern": url_pattern,
                "description": description,
                "func": func
            }
            return func
        return decorator
    
    mcp.resource = resource_decorator
    
    # Make tool decorator track registered tools
    mcp.registered_tools = {}
    
    def tool_decorator(name, description=""):
        def decorator(func):
            # Store the registered tool
            mcp.registered_tools[name] = {
                "description": description,
                "func": func
            }
            return func
        return decorator
    
    mcp.tool = tool_decorator
    
    return mcp

@pytest.fixture
def mock_context():
    """Create a mock Context object"""
    ctx = MagicMock(spec=Context)
    ctx.info = AsyncMock()
    ctx.error = AsyncMock()
    ctx.debug = AsyncMock()
    return ctx

@pytest.fixture
def dynamic_manager(mock_fastmcp, mock_client):
    """Create a DynamicToolManager with mocked dependencies"""
    # Directly pass the client rather than patching get_client
    connection_manager = UnityConnectionManager(mock_client)
    manager = DynamicToolManager(mock_fastmcp, connection_manager)
    return manager

# Tests for resource parameter handling

@pytest.mark.asyncio
async def test_register_from_schema(dynamic_manager, mock_client):
    """Test registering dynamic tools and resources from schema"""
    result = await dynamic_manager.register_from_schema()
    
    # Verify schema was retrieved
    mock_client.get_schema.assert_called_once()
    
    # Verify registration succeeded
    assert result is True
    
    # Check that resources were registered
    assert len(dynamic_manager.registered_resources) == 4
    assert "unity_info" in dynamic_manager.registered_resources
    assert "logs" in dynamic_manager.registered_resources
    assert "object_properties" in dynamic_manager.registered_resources
    assert "scene" in dynamic_manager.registered_resources
    
    # Check that tools were registered
    assert len(dynamic_manager.registered_tools) == 1
    assert "execute_code" in dynamic_manager.registered_tools

@pytest.mark.asyncio
async def test_no_parameter_resource(dynamic_manager, mock_client, mock_context):
    """Test registering and calling a resource with no parameters"""
    # Register schema
    await dynamic_manager.register_from_schema()
    
    # Test no-parameter resource (unity://info)
    resource_name = "unity_info"
    
    # Get the registered function
    registered_func = dynamic_manager.registered_resources[resource_name]["func"]
    
    # Call the function with the context
    with ResourceContext.with_context(mock_context):
        result = await registered_func(mock_context)
    
    # Verify the client was called correctly
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": resource_name,
        "parameters": {}
    })
    
    # Check result
    assert result["command"] == "access_resource"
    assert result["result"] == "success"

@pytest.mark.asyncio
async def test_single_parameter_resource(dynamic_manager, mock_client, mock_context):
    """Test registering and calling a resource with a single parameter"""
    # Register schema
    await dynamic_manager.register_from_schema()
    
    # Test single-parameter resource (unity://logs/{max_logs})
    resource_name = "logs"
    
    # Get the registered function
    registered_func = dynamic_manager.registered_resources[resource_name]["func"]
    
    # Call the function with the context and parameter
    max_logs = 10
    with ResourceContext.with_context(mock_context):
        result = await registered_func(mock_context, max_logs)
    
    # Verify the client was called correctly
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": resource_name,
        "parameters": {"max_logs": max_logs}
    })
    
    # Check result
    assert result["command"] == "access_resource"
    assert result["result"] == "success"
    assert result["params"]["parameters"]["max_logs"] == max_logs

@pytest.mark.asyncio
async def test_multi_parameter_resource(dynamic_manager, mock_client, mock_context):
    """Test registering and calling a resource with multiple parameters"""
    # Register schema
    await dynamic_manager.register_from_schema()
    
    # Test multi-parameter resource (unity://gameobject/{id}/properties/{property_name})
    resource_name = "object_properties"
    
    # Get the registered function
    registered_func = dynamic_manager.registered_resources[resource_name]["func"]
    
    # Call the function with the context and parameters
    id_value = "cube01"
    property_name = "position"
    with ResourceContext.with_context(mock_context):
        result = await registered_func(mock_context, id_value, property_name)
    
    # Verify the client was called correctly
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": resource_name,
        "parameters": {"id": id_value, "property_name": property_name}
    })
    
    # Check result
    assert result["command"] == "access_resource"
    assert result["result"] == "success"
    assert result["params"]["parameters"]["id"] == id_value
    assert result["params"]["parameters"]["property_name"] == property_name

@pytest.mark.asyncio
async def test_invoke_dynamic_resource(mock_client):
    """Test invoking dynamic resources through the invoker"""
    # Mock the connection manager
    manager = AsyncMock()
    manager.reconnect = AsyncMock(return_value=True)
    manager.execute_with_reconnect = AsyncMock(side_effect=lambda func: func())

    connection_manager = UnityConnectionManager(mock_client)
    
    # Test different parameter counts
    
    # No parameters
    result = await DynamicToolInvoker(connection_manager).invoke_dynamic_resource("unity_info")
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": "unity_info",
        "parameters": {}
    })
    
    # Single parameter (parameters are normalized to camelCase)
    result = await DynamicToolInvoker(connection_manager).invoke_dynamic_resource("logs", {"max_logs": 5})
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": "logs",
        "parameters": {"maxLogs": 5}
    })
    
    # Multiple parameters
    result = await DynamicToolInvoker(connection_manager).invoke_dynamic_resource("object_properties", {
        "id": "cube01", 
        "property_name": "position"
    })
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": "object_properties",
        "parameters": {"id": "cube01", "propertyName": "position"}
    })

@pytest.mark.asyncio
async def test_resource_context_manager():
    """Test the ResourceContext context manager"""
    # Create a context object
    ctx = MagicMock()
    
    # Verify context is initially None
    assert ResourceContext.get_current_ctx() is None
    
    # Use context manager
    with ResourceContext.with_context(ctx):
        # Verify context is set
        assert ResourceContext.get_current_ctx() is ctx
        
        # Test nested context
        ctx2 = MagicMock()
        with ResourceContext.with_context(ctx2):
            # Verify inner context
            assert ResourceContext.get_current_ctx() is ctx2
            
        # Verify outer context is restored
        assert ResourceContext.get_current_ctx() is ctx
    
    # Verify context is cleared after exiting
    assert ResourceContext.get_current_ctx() is None
    
    # Test context with exception
    try:
        with ResourceContext.with_context(ctx):
            assert ResourceContext.get_current_ctx() is ctx
            raise RuntimeError("Test exception")
    except RuntimeError:
        pass
    
    # Verify context is still cleared after exception
    assert ResourceContext.get_current_ctx() is None

@pytest.mark.asyncio
async def test_parameter_mismatch_handling(dynamic_manager, mock_client, mock_context):
    """Test handling of parameter mismatches between URI and actual parameters"""
    # Register schema
    await dynamic_manager.register_from_schema()
    
    # Test multi-parameter resource
    resource_name = "scene"
    
    # Get the registered function
    registered_func = dynamic_manager.registered_resources[resource_name]["func"]
    
    # Call with all parameters
    with ResourceContext.with_context(mock_context):
        result = await registered_func(mock_context, "main", "high")
    
    # Verify correct call
    mock_client.send_command.assert_called_with("access_resource", {
        "resource_name": resource_name,
        "parameters": {"scene_name": "main", "detail_level": "high"}
    })
    
    # Reset mock
    mock_client.send_command.reset_mock()
    
    # Try calling with missing parameters
    with pytest.raises(TypeError):
        with ResourceContext.with_context(mock_context):
            result = await registered_func(mock_context, "main")
    
    # Verify the client was not called
    mock_client.send_command.assert_not_called()
    
# Run tests if executed directly
if __name__ == "__main__":
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    
    # Run the tests
    pytest.main(["-xvs", __file__])