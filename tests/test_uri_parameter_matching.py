"""Test URI parameter matching in dynamic resources"""

import pytest
import asyncio
import sys
import logging
import re
from unittest.mock import AsyncMock, MagicMock, patch

from mcp.server.fastmcp import FastMCP, Context
from server.dynamic_tools import DynamicToolManager, ResourceContext

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("test_uri_parameter_matching")

class MockFastMCP:
    """Mock FastMCP for testing parameter validation"""
    
    def __init__(self):
        self.registered_resources = {}
        self.registered_tools = {}
        self._current_context = None
        
    def get_context(self):
        """Get the current context"""
        return self._current_context
    
    def set_context(self, ctx):
        """Set the current context"""
        self._current_context = ctx
        
    def resource(self, url_pattern, description=""):
        def decorator(func):
            resource_name = url_pattern.split("://")[1].split("/")[0]
            
            # Extract URI parameters using regex
            param_pattern = r"\{([^}]+)\}"
            uri_params = re.findall(param_pattern, url_pattern)
            
            # Check function signature against URI parameters
            import inspect
            sig = inspect.signature(func)
            sig_params = list(sig.parameters.keys())
            
            # FastMCP expects exactly ctx + URI params
            expected_sig = ["ctx"] + uri_params
            
            # Verify function signature matches
            if sig_params != expected_sig:
                raise ValueError(
                    f"Function parameters {sig_params} don't match URI parameters {expected_sig} "
                    f"for URL pattern {url_pattern}"
                )
            
            # Store the resource
            self.registered_resources[resource_name] = {
                "url_pattern": url_pattern,
                "description": description,
                "func": func,
                "uri_params": uri_params
            }
            
            return func
        return decorator
    
    def tool(self, name, description=""):
        def decorator(func):
            self.registered_tools[name] = {
                "description": description,
                "func": func
            }
            return func
        return decorator

class TestUriParameterMatching:
    """Test URI parameter matching between patterns and function signatures"""
    
    @pytest.fixture
    def mock_client(self):
        """Create a mock Unity client"""
        client = AsyncMock()
        client.get_schema = AsyncMock(return_value={
            "resources": [
                {
                    "name": "info",
                    "description": "Get Unity info",
                    "urlPattern": "unity://info"
                },
                {
                    "name": "logs",
                    "description": "Get Unity logs",
                    "urlPattern": "unity://logs/{max_logs}"
                },
                {
                    "name": "scene",
                    "description": "Get scene info",
                    "urlPattern": "unity://scene/{scene_name}"
                },
                {
                    "name": "object",
                    "description": "Get object properties",
                    "urlPattern": "unity://object/{id}/property/{property_name}"
                },
                {
                    "name": "complex",
                    "description": "Complex resource with multiple parameters",
                    "urlPattern": "unity://complex/{type}/{id}/{attribute}/{format}"
                }
            ]
        })
        client.send_command = AsyncMock(return_value={"result": "success"})
        client.connected = True
        return client
    
    @pytest.fixture
    def mock_context(self):
        """Create a mock Context"""
        ctx = MagicMock(spec=Context)
        ctx.info = MagicMock()
        ctx.error = MagicMock()
        ctx.debug = MagicMock()
        return ctx
    
    @pytest.mark.asyncio
    async def test_uri_parameter_validation(self, mock_client):
        """Test that URI parameters are properly validated against function signatures"""
        # Create FastMCP instance with validation
        mcp = MockFastMCP()
        
        # Test with valid function signatures
        
        # No parameters
        @mcp.resource("unity://test/no_params")
        async def valid_no_params(ctx):
            return {"result": "success"}
        
        # Single parameter
        @mcp.resource("unity://test/{param}")
        async def valid_single_param(ctx, param):
            return {"result": "success", "param": param}
        
        # Multiple parameters
        @mcp.resource("unity://test/{first}/{second}")
        async def valid_multi_param(ctx, first, second):
            return {"result": "success", "first": first, "second": second}
        
        # Test invalid signatures
        
        # Missing parameters
        with pytest.raises(ValueError):
            @mcp.resource("unity://test/{param}")
            async def invalid_missing_param(ctx):
                return {"result": "success"}
        
        # Extra parameters
        with pytest.raises(ValueError):
            @mcp.resource("unity://test/{param}")
            async def invalid_extra_param(ctx, param, extra):
                return {"result": "success"}
        
        # Wrong parameter names
        with pytest.raises(ValueError):
            @mcp.resource("unity://test/{expected}")
            async def invalid_wrong_name(ctx, wrong):
                return {"result": "success"}
                
    @pytest.mark.asyncio
    async def test_dynamic_resource_param_matching(self, mock_client, mock_context):
        """Test that dynamic resources are registered with proper parameter matching"""
        # Use our mocked FastMCP that performs validation
        mcp = MockFastMCP()
        mcp.set_context(mock_context)
        
        # Create a dynamic tool manager with mocked dependencies
        with patch('server.dynamic_tools.get_client', return_value=mock_client), \
             patch('server.dynamic_tools.execute_unity_operation', 
                   new=lambda op_name, op, ctx, error_prefix: asyncio.create_task(op())):
            
            manager = DynamicToolManager(mcp)
            
            # Register resources from schema
            result = await manager.register_from_schema()
            assert result is True
            
            # Check that resources were registered
            assert "info" in mcp.registered_resources
            assert "logs" in mcp.registered_resources
            assert "scene" in mcp.registered_resources
            assert "object" in mcp.registered_resources
            assert "complex" in mcp.registered_resources
            
            # Verify parameter counts match URI patterns
            assert len(mcp.registered_resources["info"]["uri_params"]) == 0
            assert len(mcp.registered_resources["logs"]["uri_params"]) == 1
            assert len(mcp.registered_resources["scene"]["uri_params"]) == 1
            assert len(mcp.registered_resources["object"]["uri_params"]) == 2
            assert len(mcp.registered_resources["complex"]["uri_params"]) == 4
            
            # Verify specific parameter names
            assert mcp.registered_resources["logs"]["uri_params"] == ["max_logs"]
            assert mcp.registered_resources["scene"]["uri_params"] == ["scene_name"]
            assert mcp.registered_resources["object"]["uri_params"] == ["id", "property_name"]
            assert mcp.registered_resources["complex"]["uri_params"] == ["type", "id", "attribute", "format"]
            
    @pytest.mark.asyncio
    async def test_resource_function_calls(self, mock_client, mock_context):
        """Test that resource functions can be called with the correct parameters"""
        mcp = MockFastMCP()
        
        # Create a dynamic tool manager with mocked dependencies
        with patch('server.dynamic_tools.get_client', return_value=mock_client):
            manager = DynamicToolManager(mcp)
            
            # Register resources from schema
            result = await manager.register_from_schema()
            assert result is True
            
            # Test calling resource functions
            
            # No parameter function
            info_func = mcp.registered_resources["info"]["func"]
            with ResourceContext.with_context(mock_context):
                result = await info_func(mock_context)
                assert result["result"] == "success"
            
            # Single parameter function
            logs_func = mcp.registered_resources["logs"]["func"]
            with ResourceContext.with_context(mock_context):
                result = await logs_func(mock_context, 5)
                assert result["result"] == "success"
            
            # Check parameter was passed correctly
            mock_client.send_command.assert_called_with("access_resource", {
                "resource_name": "logs",
                "parameters": {"max_logs": 5}
            })
            
            # Multi-parameter function
            object_func = mcp.registered_resources["object"]["func"]
            with ResourceContext.with_context(mock_context):
                result = await object_func(mock_context, "cube01", "position")
                assert result["result"] == "success"
            
            # Check multiple parameters were passed correctly 
            mock_client.send_command.assert_called_with("access_resource", {
                "resource_name": "object",
                "parameters": {"id": "cube01", "property_name": "position"}
            })
            
            # Complex multi-parameter function
            complex_func = mcp.registered_resources["complex"]["func"]
            with ResourceContext.with_context(mock_context):
                result = await complex_func(mock_context, "mesh", "player", "transform", "json")
                assert result["result"] == "success"
                
            # Check all parameters were passed correctly
            mock_client.send_command.assert_called_with("access_resource", {
                "resource_name": "complex",
                "parameters": {
                    "type": "mesh", 
                    "id": "player", 
                    "attribute": "transform", 
                    "format": "json"
                }
            })

# Run tests if executed directly
if __name__ == "__main__":
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    
    # Run the tests
    pytest.main(["-xvs", __file__])