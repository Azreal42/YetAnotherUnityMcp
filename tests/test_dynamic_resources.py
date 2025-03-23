"""Integration tests for dynamic resource implementation"""

import pytest
import asyncio
import logging
import json
from unittest.mock import AsyncMock, MagicMock, patch
from typing import Dict, Any, List, Optional

from mcp.server.fastmcp import FastMCP
from server.dynamic_tools import DynamicToolManager, get_manager
from server.unity_tcp_client import get_client

# Helper function for invoking dynamic resources
async def invoke_dynamic_resource(resource_name: str, parameters: Optional[Dict[str, Any]] = None):
    """
    Invoke a dynamic resource.
    
    Args:
        resource_name: Name of the resource to invoke
        parameters: Resource parameters
        
    Returns:
        Resource result
    """
    if parameters is None:
        parameters = {}
        
    client = get_client()
    return await client.send_command("access_resource", {
        "resource_name": resource_name,
        "parameters": parameters
    })

# Import or define the necessary functions
# This should match the implementation in test_dynamic_tools.py
async def invoke_dynamic_resource(resource_name: str, parameters: Dict[str, Any] = None):
    """Helper function to invoke a dynamic resource"""
    if parameters is None:
        parameters = {}
        
    client = get_client()
    return await client.send_command("access_resource", {
        "resource_name": resource_name,
        "parameters": parameters
    })

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("test_dynamic_resources")

# Mark all tests with asyncio
pytestmark = pytest.mark.asyncio

# Helper function for resource invocation
async def invoke_dynamic_resource(resource_name, parameters=None):
    """Invoke a dynamic resource with the given parameters"""
    if parameters is None:
        parameters = {}
    client = get_client()
    return await client.send_command("access_resource", {
        "resource_name": resource_name, 
        "parameters": parameters
    })

@pytest.fixture
def mcp_test_instance():
    """Create a FastMCP instance for testing"""
    return FastMCP("Test Dynamic Resources", description="Test dynamic resource registration")

class TestDynamicResources:
    """Test suite for dynamic resource implementation"""
    
    async def test_resource_registration_and_invocation(self, connected_client, mcp_test_instance):
        """Test resource registration and invocation with all parameter types"""
        # Use the connected client directly - it's already a UnityTcpClient, not an async generator
        logger.info("Using connected client...")
        client = connected_client
        logger.info(f"Client type: {type(client)}")
        
        # Get dynamic tool manager with patched client
        logger.info("Creating dynamic tool manager...")
        with patch('server.dynamic_tools.get_client', return_value=client):
            manager = get_manager(mcp_test_instance)
            
            # Register resources from schema with timeout
            logger.info("Registering resources from schema...")
            result = await asyncio.wait_for(manager.register_from_schema(), timeout=10.0)
        
        assert result is True, "Failed to register resources from schema"
        
        # Log registered resources
        logger.info(f"Successfully registered {len(manager.registered_resources)} resources")
        for name, url_pattern in manager.registered_resources.items():
            logger.info(f"  - {name}: {url_pattern}")
        
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
                    logger.info(f"    Result: {self._truncate_result(result)}")
                    assert result is not None, f"Resource {resource_name} returned None"
                except Exception as e:
                    pytest.fail(f"Resource {resource_name} raised an exception: {str(e)}")
        else:
            logger.info("No parameter-less resources available for testing")
            
        # Test single-parameter resources
        if single_param_resources:
            logger.info(f"Testing {len(single_param_resources)} single-parameter resources")
            for param_name, resource_name in single_param_resources.items():
                logger.info(f"  - Invoking {resource_name} with parameter {param_name}")
                
                # Generate test value based on parameter name
                param_value = self._generate_test_value(param_name)
                
                try:
                    result = await invoke_dynamic_resource(resource_name, {param_name: param_value})
                    logger.info(f"    Result with {param_name}={param_value}: {self._truncate_result(result)}")
                    assert result is not None, f"Resource {resource_name} returned None"
                except Exception as e:
                    pytest.fail(f"Resource {resource_name} raised an exception: {str(e)}")
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
                    params[param_name] = self._generate_test_value(param_name)
                
                # Full parameters test
                try:
                    logger.info(f"    Testing with all parameters: {json.dumps(params)}")
                    result = await invoke_dynamic_resource(resource_name, params)
                    logger.info(f"    Result: {self._truncate_result(result)}")
                    assert result is not None, f"Resource {resource_name} returned None"
                except Exception as e:
                    pytest.fail(f"Resource {resource_name} raised an exception: {str(e)}")
                    
                # Missing parameter test (if we have at least 2 parameters)
                if len(param_names) >= 2:
                    missing_params = params.copy()
                    missing_key = param_names[-1]
                    del missing_params[missing_key]
                    
                    logger.info(f"    Testing with missing parameter {missing_key}: {json.dumps(missing_params)}")
                    with pytest.raises(Exception):
                        await invoke_dynamic_resource(resource_name, missing_params)
        else:
            logger.info("No multi-parameter resources available for testing")
        
        logger.info("All resource tests completed")
    
    async def test_custom_resource_parameters(self, connected_client, mcp_test_instance):
        """Test custom resource parameter patterns
        
        This test manually registers resources with various parameter patterns and tests them.
        """
        # Use the connected client directly - it's already a UnityTcpClient
        logger.info("Using connected client...")
        client = connected_client
        
        # Create dynamic tool manager with patched client
        logger.info("Creating tool manager for custom resource tests...")
        with patch('server.dynamic_tools.get_client', return_value=client):
            manager = DynamicToolManager(mcp_test_instance)
        
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
                "uri": url_pattern,
                "mimeType": "application/json"
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
            
            # Verify resource was registered
            assert resource_name in manager.registered_resources, f"Resource {resource_name} was not registered"
            
            # Create test parameters
            params = {}
            for param in param_names:
                params[param] = self._generate_test_value(param)
                
            # Skip invocation for custom resources as they don't exist in Unity
            # We're just testing the registration and parameter extraction

    def _generate_test_value(self, param_name: str) -> Any:
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

    def _truncate_result(self, result: Any, max_length: int = 100) -> str:
        """Truncate result for logging"""
        result_str = str(result)
        if len(result_str) > max_length:
            return result_str[:max_length] + "..."
        return result_str

class TestDynamicResourcesMocked:
    """Test suite for dynamic resources using mocked Unity client"""
    
    @pytest.fixture
    def mock_unity_client(self):
        """Create a mocked Unity client for testing without Unity"""
        client = AsyncMock()
        
        # Mock schema response - exactly matching the actual response format
        client.get_schema = AsyncMock(return_value={
            "id": "req_3f104d03fe1f42dd9af957826f17b98f",
            "type": "response",
            "status": "success",
            "result": {
                "content": [
                    {
                        "type": "text",
                        "text": json.dumps({
                            "tools": [
                                {
                                    "name": "scene_load_scene",
                                    "description": "Load a scene by name",
                                    "inputSchema": {
                                        "type": "object",
                                        "properties": {
                                            "scene_name": {
                                                "type": "string",
                                                "description": "Name of the scene to load"
                                            },
                                            "mode": {
                                                "type": "string",
                                                "description": "Load mode (Single, Additive)"
                                            }
                                        },
                                        "required": ["scene_name"]
                                    },
                                    "example": "scene_load_scene(\"MainScene\", \"Additive\")"
                                },
                                {
                                    "name": "editor_execute_code",
                                    "description": "Execute code in editor",
                                    "inputSchema": {
                                        "type": "object",
                                        "properties": {
                                            "param1": {
                                                "type": "string",
                                                "description": "Code to execute"
                                            }
                                        },
                                        "required": []
                                    },
                                    "example": "editor_execute_code(\"Debug.Log('Hello')\")"
                                },
                                {
                                    "name": "editor_take_screenshot",
                                    "description": "Take a screenshot of the Unity Editor",
                                    "inputSchema": {
                                        "type": "object",
                                        "properties": {
                                            "output_path": {
                                                "type": "string",
                                                "description": "Path where to save the screenshot"
                                            }
                                        },
                                        "required": []
                                    }
                                }
                            ],
                            "resources": [
                                {
                                    "name": "editor_info",
                                    "description": "Get information about the Unity Editor",
                                    "uri": "unity://editor/info",
                                    "mimeType": "application/json",
                                    "parameters": {}
                                },
                                {
                                    "name": "scene_active_scene",
                                    "description": "Get information about the active scene",
                                    "uri": "unity://scene/active",
                                    "mimeType": "application/json",
                                    "parameters": {}
                                },
                                {
                                    "name": "object_info",
                                    "description": "Get information about a specific GameObject",
                                    "uri": "unity://object/{object_id}",
                                    "mimeType": "application/json",
                                    "parameters": {}
                                }
                            ]
                        })
                    }
                ]
            }
        })
        
        # Mock send_command response
        client.send_command = AsyncMock(side_effect=self._mock_send_command)
        client.connected = True
        client.has_command = AsyncMock(return_value=True)
        client.connect = AsyncMock(return_value=True)
        client.disconnect = AsyncMock(return_value=None)
        
        return client
    
    def _mock_send_command(self, command: str, params: Dict[str, Any]):
        """Mock the send_command method based on the command and parameters"""
        if command == "access_resource":
            resource_name = params.get("resource_name", "")
            parameters = params.get("parameters", {})
            
            if resource_name == "editor_info":
                return {
                    "result": {
                        "content": [
                            {
                                "type": "text",
                                "text": json.dumps({
                                    "unityVersion": "2022.3.10f1",
                                    "platform": "Windows",
                                    "editorMode": True
                                })
                            }
                        ],
                        "isError": False
                    }
                }
            elif resource_name == "scene_active_scene":
                return {
                    "result": {
                        "content": [
                            {
                                "type": "text",
                                "text": json.dumps({
                                    "name": "SampleScene",
                                    "path": "Assets/Scenes/SampleScene.unity",
                                    "objects": ["Main Camera", "Directional Light", "Cube"]
                                })
                            }
                        ],
                        "isError": False
                    }
                }
            elif resource_name == "object_info":
                obj_id = parameters.get("object_id", "")
                
                return {
                    "result": {
                        "content": [
                            {
                                "type": "text",
                                "text": json.dumps({
                                    "id": obj_id,
                                    "name": obj_id,
                                    "position": {"x": 0, "y": 0, "z": 0},
                                    "rotation": {"x": 0, "y": 0, "z": 0, "w": 1}
                                })
                            }
                        ],
                        "isError": False
                    }
                }
            elif resource_name == "scene_by_name":
                scene_name = parameters.get("scene_name", "")
                
                return {
                    "result": {
                        "content": [
                            {
                                "type": "text",
                                "text": json.dumps({
                                    "scene": scene_name,
                                    "detail": detail_level,
                                    "objects": [f"Object_{i}" for i in range(3)]
                                })
                            }
                        ],
                        "isError": False
                    }
                }
            else:
                return {
                    "result": {
                        "content": [
                            {
                                "type": "text",
                                "text": f"Unknown resource: {resource_name}"
                            }
                        ],
                        "isError": True
                    }
                }
        else:
            return {
                "result": {
                    "content": [
                        {
                            "type": "text",
                            "text": f"Unknown command: {command}"
                        }
                    ],
                    "isError": True
                }
            }
    
    @pytest.mark.asyncio
    async def test_resource_parameter_extraction(self, mock_unity_client, mcp_test_instance):
        """Test extracting parameters from resource URL patterns"""
        # Create dynamic tool manager
        with patch('server.dynamic_tools.get_client', return_value=mock_unity_client):
            manager = DynamicToolManager(mcp_test_instance)
            
            # Register resources from schema
            result = await manager.register_from_schema()
            
            assert result is True, "Failed to register resources from schema"
            assert len(manager.registered_resources) > 0, "No resources were registered"
            
            # Test parameter extraction from different patterns
            resource_patterns = {
                "unity://info": 0,
                "unity://logs/{max_logs}": 1,
                "unity://gameobject/{id}/properties/{property_name}": 2,
                "unity://scene/{scene_name}/{detail_level}": 2
            }
            
            for pattern, expected_param_count in resource_patterns.items():
                # Find resource with this pattern
                matching_resources = [name for name, url in manager.registered_resources.items() 
                                     if url == pattern]
                
                if not matching_resources:
                    continue
                    
                resource_name = matching_resources[0]
                
                # Extract parameters 
                param_names = []
                parts = pattern.split('/')
                for part in parts:
                    if part.startswith('{') and part.endswith('}'):
                        param_name = part[1:-1]
                        param_names.append(param_name)
                
                # Check parameter count matches expected
                assert len(param_names) == expected_param_count, \
                    f"Resource {resource_name} has {len(param_names)} parameters, expected {expected_param_count}"
    
    @pytest.mark.asyncio
    async def test_mocked_resource_invocation(self, mock_unity_client, mcp_test_instance):
        """Test invoking resources with mocked client"""
        # Create dynamic tool manager
        with patch('server.dynamic_tools.get_client', return_value=mock_unity_client),\
             patch('server.dynamic_tool_invoker.get_client', return_value=mock_unity_client),\
             patch('server.dynamic_tool_invoker.get_unity_connection_manager') as mock_connection_manager:
            
            # Mock the connection manager
            manager_mock = AsyncMock()
            manager_mock.reconnect = AsyncMock(return_value=True)
            manager_mock.execute_with_reconnect = AsyncMock(side_effect=lambda func: func())
            mock_connection_manager.return_value = manager_mock
            
            # Register tools
            tool_manager = DynamicToolManager(mcp_test_instance)
            await tool_manager.register_from_schema()
            
            # Test invoking resources with different parameter counts
            
            # No parameters
            result = await invoke_dynamic_resource("unity_info")
            assert result is not None, "Resource invocation returned None"
            if isinstance(result, dict) and isinstance(result.get("result"), dict):
                content = result.get("result", {}).get("content", [])
                if content and isinstance(content, list) and content[0].get("type") == "text":
                    text_content = content[0].get("text", "")
                    assert "unityVersion" in text_content, "unity_info resource did not return expected content"
            
            # Single parameter
            result = await invoke_dynamic_resource("logs", {"max_logs": 3})
            assert result is not None, "Resource invocation returned None"
            if isinstance(result, dict) and isinstance(result.get("result"), dict):
                content = result.get("result", {}).get("content", [])
                if content and isinstance(content, list) and content[0].get("type") == "text":
                    text_content = content[0].get("text", "")
                    assert "Log message" in text_content, "logs resource did not return expected content"
            
            # Multiple parameters
            result = await invoke_dynamic_resource("object_properties", {
                "id": "test_cube", 
                "property_name": "position"
            })
            assert result is not None, "Resource invocation returned None"
            if isinstance(result, dict) and isinstance(result.get("result"), dict):
                content = result.get("result", {}).get("content", [])
                if content and isinstance(content, list) and content[0].get("type") == "text":
                    text_content = content[0].get("text", "")
                    assert "test_cube" in text_content, "object_properties resource did not return expected id"
                    assert "position" in text_content, "object_properties resource did not return expected property"
            
            # Multiple parameters (scene)
            result = await invoke_dynamic_resource("scene", {
                "scene_name": "TestScene", 
                "detail_level": "high"
            })
            assert result is not None, "Resource invocation returned None"
            if isinstance(result, dict) and isinstance(result.get("result"), dict):
                content = result.get("result", {}).get("content", [])
                if content and isinstance(content, list) and content[0].get("type") == "text":
                    text_content = content[0].get("text", "")
                    assert "TestScene" in text_content, "scene resource did not return expected scene name"
                    assert "high" in text_content, "scene resource did not return expected detail level"
    
    def _generate_test_value(self, param_name: str) -> Any:
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
        
if __name__ == "__main__":
    # Run the tests
    pytest.main(["-xvs", __file__])