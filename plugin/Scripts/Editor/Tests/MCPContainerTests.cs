using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Tests for container-based MCP implementation
    /// </summary>
    public class MCPContainerTests
    {
        private MCPRegistry registry;

        [SetUp]
        public void Setup()
        {
            // Get the singleton registry and clear it
            registry = MCPRegistry.Instance;
            
            // Clear the registry using reflection
            MethodInfo clearMethod = registry.GetType().GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clearMethod != null)
            {
                clearMethod.Invoke(registry, null);
            }
        }

        // Mock container class for testing
        [MCPContainer("test_container", "Test container for unit tests")]
        public static class MockContainer
        {
            // Test resource method
            [MCPResource("container_resource", "Test container resource", "unity://test/resource", "unity://test/resource")]
            public static string GetTestResource()
            {
                return "{\"result\": \"container_resource_success\"}";
            }

            // Test resource method with parameters
            [MCPResource("container_resource_with_params", "Test container resource with parameters", 
                "unity://test/{param1}/{param2}", "unity://test/value1/42")]
            public static string GetTestResourceWithParams(
                [MCPParameter("param1", "First parameter", "string", true)] string param1,
                [MCPParameter("param2", "Second parameter", "number", true)] int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }

            // Test tool method
            [MCPTool("container_tool", "Test container tool", "test_container_tool()")]
            public static string ExecuteTestTool()
            {
                return "{\"result\": \"container_tool_success\"}";
            }

            // Test tool method with parameters
            [MCPTool("container_tool_with_params", "Test container tool with parameters", 
                "test_container_tool_with_params(\"value\", 42)")]
            public static string ExecuteTestToolWithParams(
                [MCPParameter("param1", "First parameter", "string", true)] string param1,
                [MCPParameter("param2", "Second parameter", "number", true)] int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }

        [Test]
        public void RegisterMethodsFromContainer_RegistersAllResourcesAndTools()
        {
            // Act
            registry.RegisterMethodsFromContainer(typeof(MockContainer));

            // Assert
            Assert.AreEqual(4, registry.Schema.Resources.Count, "Should have 2 registered resources");
            Assert.AreEqual(6, registry.Schema.Tools.Count, "Should have 2 registered tools");
            
            // Check resources
            var resourceNames = registry.Schema.Resources.Select(r => r.Name).ToList();
            Assert.Contains("test_container_container_resource", resourceNames);
            Assert.Contains("test_container_container_resource_with_params", resourceNames);
            
            // Check tools
            var toolNames = registry.Schema.Tools.Select(t => t.Name).ToList();
            Assert.Contains("test_container_container_tool", toolNames);
            Assert.Contains("test_container_container_tool_with_params", toolNames);
        }

        [Test]
        public void ResourceInvoker_WithContainerResource_ReturnsCorrectResult()
        {
            // Arrange
            registry.RegisterMethodsFromContainer(typeof(MockContainer));

            // Act
            var result = ResourceInvoker.InvokeResource("test_container_container_resource", null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"container_resource_success\"}", result);
        }

        [Test]
        public void ResourceInvoker_WithContainerResourceWithParams_PassesParametersCorrectly()
        {
            // Arrange
            registry.RegisterMethodsFromContainer(typeof(MockContainer));
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };

            // Act
            var result = ResourceInvoker.InvokeResource("test_container_container_resource_with_params", parameters);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }

        [Test]
        public void ToolInvoker_WithContainerTool_ReturnsCorrectResult()
        {
            // Arrange
            registry.RegisterMethodsFromContainer(typeof(MockContainer));

            // Act
            var toolDescriptor = registry.GetToolByName("test_container_container_tool");
            Assert.IsNotNull(toolDescriptor, "Tool descriptor should not be null");
            var result = ToolInvoker.InvokeTool(toolDescriptor, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"container_tool_success\"}", result);
        }

        [Test]
        public void ToolInvoker_WithContainerToolWithParams_PassesParametersCorrectly()
        {
            // Arrange
            registry.RegisterMethodsFromContainer(typeof(MockContainer));
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };

            // Act
            var toolDescriptor = registry.GetToolByName("test_container_container_tool_with_params");
            Assert.IsNotNull(toolDescriptor, "Tool descriptor should not be null");
            var result = ToolInvoker.InvokeTool(toolDescriptor, parameters);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }

        [Test]
        public void ResourceInvoker_WithTypeConversion_ConvertsParametersCorrectly()
        {
            // Arrange
            registry.RegisterMethodsFromContainer(typeof(MockContainer));
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", "42" }  // String instead of int
            };

            // Act
            var result = ResourceInvoker.InvokeResource("test_container_container_resource_with_params", parameters);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }

        [Test]
        public void ToolInvoker_WithMissingRequiredParameter_ThrowsArgumentException()
        {
            // Arrange
            registry.RegisterMethodsFromContainer(typeof(MockContainer));
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" }
                // param2 is missing
            };
            var toolDescriptor = registry.GetToolByName("test_container_container_tool_with_params");
            Assert.IsNotNull(toolDescriptor, "Tool descriptor should not be null");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ToolInvoker.InvokeTool(toolDescriptor, parameters)
            );
        }
    }
}