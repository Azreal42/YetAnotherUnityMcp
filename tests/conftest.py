"""Shared pytest fixtures for Unity MCP tests"""

import pytest
import asyncio
import logging
import sys
import json
from unittest.mock import AsyncMock, MagicMock, patch

from mcp.server.fastmcp import FastMCP, Context
from server.dynamic_tools import DynamicToolManager
from server.unity_tcp_client import get_client

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

# Fixture for FastMCP instance
@pytest.fixture
def mcp_instance():
    """Create a FastMCP instance for testing"""
    return FastMCP("Test FastMCP", description="MCP instance for testing")

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
    ctx.info = MagicMock()
    ctx.error = MagicMock()
    ctx.debug = MagicMock()
    return ctx

@pytest.fixture
def dynamic_manager(mock_fastmcp, mock_client):
    """Create a DynamicToolManager with mocked dependencies"""
    with patch('server.dynamic_tools.get_client', return_value=mock_client):
        manager = DynamicToolManager(mock_fastmcp)
        return manager

@pytest.fixture
def real_client():
    """Get a real Unity client - only used for integration tests
    
    Note: Assumes Unity is running with MCP plugin loaded
    """
    return get_client()

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

# Configure async test support
def pytest_configure(config):
    """Configure pytest for async tests"""
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())