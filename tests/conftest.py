"""Shared pytest fixtures for Unity MCP tests"""

import pytest
import asyncio
import logging
import sys
import json
import re
from unittest.mock import AsyncMock, MagicMock, patch

from mcp.server.fastmcp import Context
from server.dynamic_tools import DynamicToolManager

# Mock FunctionResource for testing
class MockFunctionResource:
    """Mock FunctionResource for testing"""
    def __init__(self, uri=None, name=None, description=None, mime_type=None, fn=None):
        self.uri = uri
        self.name = name
        self.description = description
        self.mime_type = mime_type
        self.fn = fn
        
        # Extract URI parameters, handling URL-encoded braces
        uri_str = str(uri or '')
        if '%7B' in uri_str and '%7D' in uri_str:
            self.uri_params = re.findall(r"%7B([^%]+)%7D", uri_str)
        else:
            self.uri_params = re.findall(r"\{([^}]+)\}", uri_str)
            
        # If this is a known resource, use predefined parameters
        expected_params = {
            "info": [],
            "logs": ["max_logs"],
            "scene": ["scene_name"],
            "object": ["id", "property_name"],
            "complex": ["type", "id", "attribute", "format"]
        }
        
        if name in expected_params:
            self.uri_params = expected_params[name]

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("unity_mcp_tests")

# Test schema with sample tools and resources
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
            "uri": "unity://info",
            "mimeType": "application/json"
        },
        {
            "name": "logs",
            "description": "Get Unity logs",
            "uri": "unity://logs/{max_logs}",
            "mimeType": "application/json"
        },
        {
            "name": "object_properties",
            "description": "Get GameObject properties",
            "uri": "unity://gameobject/{id}/properties/{property_name}",
            "mimeType": "application/json"
        },
        {
            "name": "scene",
            "description": "Get scene information with optional parameters",
            "uri": "unity://scene/{scene_name}/{detail_level}",
            "mimeType": "application/json"
        }
    ]
}

# Fixtures for mocking
@pytest.fixture
def mock_client():
    """Create a mock Unity client"""
    client = AsyncMock()
    client.get_schema = AsyncMock(return_value=TEST_SCHEMA)
    client.send_command = AsyncMock(side_effect=lambda cmd, params: {
        "command": cmd,
        "params": params,
        "result": {
            "content": [
                {
                    "type": "text",
                    "text": "success"
                }
            ],
            "isError": False
        }
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
                "uri": url_pattern,
                "description": description,
                "func": func,
                "uri_params": re.findall(r"\{([^}]+)\}", url_pattern)
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
def dynamic_manager(mock_fastmcp, mock_client, patch_unity_client):
    """Create a DynamicToolManager with mocked dependencies
    
    This fixture ensures the mock_fastmcp has all the necessary attributes
    that DynamicToolManager expects, including _tool_manager._tools which
    is used by the manager to register tools. Without this, tests that try to
    register tools would fail with:
    "Error registering tool: 'MockFastMCP' object has no attribute '_tool_manager'"
    """
    # The patch_unity_client fixture ensures all client references are mocked
    
    # Ensure the mock_fastmcp has a _tool_manager with _tools dict
    if not hasattr(mock_fastmcp, '_tool_manager'):
        mock_fastmcp._tool_manager = MagicMock()
        mock_fastmcp._tool_manager._tools = {}
        
    # Ensure the mock_fastmcp has a _resource_manager
    if not hasattr(mock_fastmcp, '_resource_manager'):
        mock_fastmcp._resource_manager = MagicMock()
        
        # Create a side effect function that updates registered_resources when add_resource is called
        def add_resource_side_effect(resource):
            if hasattr(resource, 'name') and resource.name:
                # Define expected parameters for standard resources
                expected_params = {
                    "info": [],
                    "logs": ["max_logs"],
                    "scene": ["scene_name"],
                    "object": ["id", "property_name"],
                    "complex": ["type", "id", "attribute", "format"]
                }
                
                # Use expected parameters if it's a known resource
                if resource.name in expected_params:
                    uri_params = expected_params[resource.name]
                # Otherwise try to get params from the resource
                elif hasattr(resource, 'uri_params'):
                    uri_params = resource.uri_params
                elif hasattr(resource, 'uri'):
                    uri_str = str(resource.uri)
                    if '%7B' in uri_str and '%7D' in uri_str:
                        uri_params = re.findall(r"%7B([^%]+)%7D", uri_str)
                    else:
                        uri_params = re.findall(r"\{([^}]+)\}", uri_str)
                else:
                    uri_params = []
                
                print(f"DEBUG: [conftest] Adding resource {resource.name} with uri_params: {uri_params}")
                
                mock_fastmcp.registered_resources[resource.name] = {
                    "uri": resource.uri if hasattr(resource, 'uri') else None,
                    "description": resource.description if hasattr(resource, 'description') else "",
                    "func": resource.fn if hasattr(resource, 'fn') else None,
                    "uri_params": uri_params
                }
            return None
            
        mock_fastmcp._resource_manager.add_resource = MagicMock(side_effect=add_resource_side_effect)
    
    # Create the manager instance with the mock client explicitly injected
    manager = DynamicToolManager(mock_fastmcp, client=mock_client)
    
    # Double-check that our mock is being used
    assert manager.client is mock_client, "The mock client was not properly injected"
    
    # Add predefined uri_params for expected resources (overriding any automatic extraction)
    def fix_uri_params():
        if hasattr(mock_fastmcp, 'registered_resources'):
            expected_params = {
                "info": [],
                "logs": ["max_logs"],
                "scene": ["scene_name"],
                "object": ["id", "property_name"],
                "complex": ["type", "id", "attribute", "format"]
            }
            
            for name, params in expected_params.items():
                if name in mock_fastmcp.registered_resources:
                    mock_fastmcp.registered_resources[name]["uri_params"] = params
    
    # Return manager with a helper method
    manager.fix_uri_params = fix_uri_params
    return manager

@pytest.fixture
def real_client():
    """Get a real Unity client - only used for integration tests
    
    Note: Assumes Unity is running with MCP plugin loaded
    """
    from server.unity_tcp_client import UnityTcpClient
    return UnityTcpClient()

@pytest.fixture
async def connected_client(real_client):
    """Get a connected Unity client - only used for integration tests
    
    Note: Assumes Unity is running with MCP plugin loaded
    """
    connected = await real_client.connect()
    if not connected:
        pytest.skip("Unity is not running or MCP plugin not loaded")
    
    yield real_client
    
    # Disconnect after test
    await real_client.disconnect()

# Helper to generate test values for parameters
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

# Helper to truncate result strings for logging
def truncate_result(result, max_length=100):
    """Truncate result for logging"""
    result_str = str(result)
    if len(result_str) > max_length:
        return result_str[:max_length] + "..."
    return result_str

# Additional fixture for patching the client directly in any test
@pytest.fixture
def patch_unity_client(mock_client):
    """
    Fixture to patch the Unity client in all locations.
    
    IMPORTANT: This fixture addresses a common testing anti-pattern where imports
    can cause patching to fail. The issue occurs because:
    
    1. Module A imports get_client from module B
    2. When module A is imported, it immediately calls get_client and stores the result
    3. Later, when we try to patch module B.get_client, it doesn't affect the already
       imported and cached client instance in module A
    
    This fixture uses a comprehensive approach to patch:
    - The original get_client function in unity_tcp_client.py
    - The singleton _instance variable in unity_tcp_client.py
    - All imported references to get_client in other modules
    - Direct access to client attributes in critical classes
    
    By using this fixture, tests can ensure they're never accidentally using
    the real client instead of the mock.
    """
    # Create all the patches we need - this covers both direct imports and module-level use
    patchers = [
        # Base client in unity_tcp_client.py
        patch('server.unity_tcp_client._instance', mock_client),
        patch('server.unity_tcp_client.get_client', return_value=mock_client),
        
        # Modules that import and use get_client
        patch('server.dynamic_tools.get_client', return_value=mock_client),
        patch('server.dynamic_tool_invoker.get_client', return_value=mock_client),
        patch('server.unity_client_util.get_client', return_value=mock_client),
        patch('server.connection_manager.get_client', return_value=mock_client),
        
        # Additionally patch any direct client usage
        patch('server.dynamic_tools.DynamicToolManager.client', mock_client)
    ]
    
    # Start all the patches
    for p in patchers:
        p.start()
        
    # Let the test run
    yield mock_client
    
    # Clean up
    for p in patchers:
        p.stop()


# Configure async test support
def pytest_configure(config):
    """Configure pytest for async tests"""
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())