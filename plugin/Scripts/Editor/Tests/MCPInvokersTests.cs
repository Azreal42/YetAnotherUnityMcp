using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Tests for ResourceInvoker and ToolInvoker
    /// </summary>
    public class MCPInvokersTests
    {
        /// <summary>
        /// Test container for unit tests
        /// </summary>
        [MCPContainer("test", "Test container for unit tests")]
        public static class TestContainer
        {
            /// <summary>
            /// Test resource for unit tests
            /// </summary>
            [MCPResource("resource", "Test resource for unit tests", "unity://test", "unity://test")]
            public static string GetResource()
            {
                return "{\"result\": \"test_success\"}";
            }
            
            /// <summary>
            /// Test resource with parameters
            /// </summary>
            [MCPResource("resource_with_params", "Test resource with parameters", "unity://test/{param1}/{param2}", "unity://test/value1/value2")]
            public static string GetResourceWithParams(
                [MCPParameter("param1", "First parameter", "string", true)] string param1,
                [MCPParameter("param2", "Second parameter", "number", true)] int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
            
            /// <summary>
            /// Test resource with default parameter
            /// </summary>
            [MCPResource("resource_with_default", "Test resource with default parameter", "unity://test/default", "unity://test/default")]
            public static string GetResourceWithDefault(
                [MCPParameter("param1", "Parameter with default value", "string", false)] string param1 = "default_value")
            {
                return $"{{\"param1\": \"{param1}\"}}";
            }
            
            /// <summary>
            /// Test tool for unit tests
            /// </summary>
            [MCPTool("tool", "Test tool for unit tests", "test_tool()")]
            public static string ExecuteTool()
            {
                return "{\"result\": \"tool_success\"}";
            }
            
            /// <summary>
            /// Test tool with parameters
            /// </summary>
            [MCPTool("tool_with_params", "Test tool with parameters", "test_tool_with_params(\"value\", 42)")]
            public static string ExecuteToolWithParams(
                [MCPParameter("param1", "First parameter", "string", true)] string param1,
                [MCPParameter("param2", "Second parameter", "number", true)] int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }
        
        // Legacy classes to support backward compatibility tests
        public static class TestResource
        {
            public static string GetResource()
            {
                return "{\"result\": \"test_success\"}";
            }
        }
        
        public static class TestResourceWithParams
        {
            public static string GetResource(string param1, int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }
        
        public static class TestResourceWithDefault
        {
            public static string GetResource(string param1 = "default_value")
            {
                return $"{{\"param1\": \"{param1}\"}}";
            }
        }
        
        public static class TestTool
        {
            public static string Execute()
            {
                return "{\"result\": \"tool_success\"}";
            }
        }
        
        public static class TestToolWithParams
        {
            public static string Execute(string param1, int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }
        
        /// <summary>
        /// Register test resources and tools in the registry
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var registry = MCPRegistry.Instance;
            
            // Clear the registry first to ensure clean state
            MethodInfo clearMethod = registry.GetType().GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clearMethod != null)
            {
                clearMethod.Invoke(registry, null);
            }
            
            // Register container methods
            registry.RegisterMethodsFromContainer(typeof(TestContainer));
            
            // Create and register legacy descriptors for backward compatibility
            var testResourceDescriptor = new ResourceDescriptor
            {
                Name = "test_resource",
                Description = "Test resource for unit tests",
                UrlPattern = "unity://test",
                Example = "unity://test",
                ContainerType = typeof(TestResource),
                OutputSchema = new Schema()
            };
            registry.RegisterResource(testResourceDescriptor);
            
            var testResourceWithParamsDescriptor = new ResourceDescriptor
            {
                Name = "test_resource_with_params",
                Description = "Test resource with parameters",
                UrlPattern = "unity://test/{param1}/{param2}",
                Example = "unity://test/value1/value2",
                ContainerType = typeof(TestResourceWithParams),
                Parameters = new Dictionary<string, ParameterDescriptor>
                {
                    { "param1", new ParameterDescriptor { Description = "First parameter", Type = "string", Required = true } },
                    { "param2", new ParameterDescriptor { Description = "Second parameter", Type = "number", Required = true } }
                },
                OutputSchema = new Schema()
            };
            registry.RegisterResource(testResourceWithParamsDescriptor);
            
            var testResourceWithDefaultDescriptor = new ResourceDescriptor
            {
                Name = "test_resource_with_default",
                Description = "Test resource with default parameter",
                UrlPattern = "unity://test/default",
                Example = "unity://test/default",
                ContainerType = typeof(TestResourceWithDefault),
                Parameters = new Dictionary<string, ParameterDescriptor>
                {
                    { "param1", new ParameterDescriptor { Description = "Parameter with default value", Type = "string", Required = false } }
                },
                OutputSchema = new Schema()
            };
            registry.RegisterResource(testResourceWithDefaultDescriptor);
            
            var testToolDescriptor = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Test tool for unit tests",
                Example = "test_tool()",
                ContainerType = typeof(TestTool),
                InputSchema = new InputSchema(),
                OutputSchema = new Schema()
            };
            registry.RegisterTool(testToolDescriptor);
            
            var testToolWithParamsDescriptor = new ToolDescriptor
            {
                Name = "test_tool_with_params",
                Description = "Test tool with parameters",
                Example = "test_tool_with_params(\"value\", 42)",
                ContainerType = typeof(TestToolWithParams),
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, ParameterDescriptor>
                    {
                        { "param1", new ParameterDescriptor { Description = "First parameter", Type = "string", Required = true } },
                        { "param2", new ParameterDescriptor { Description = "Second parameter", Type = "number", Required = true } }
                    },
                    Required = new List<string> { "param1", "param2" }
                },
                OutputSchema = new Schema()
            };
            registry.RegisterTool(testToolWithParamsDescriptor);
        }
        
        /// <summary>
        /// Test ResourceInvoker with a simple resource
        /// </summary>
        [Test]
        public void ResourceInvoker_WithSimpleResource_ReturnsResult()
        {
            // Act
            var result = ResourceInvoker.InvokeResource("test_resource", null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"test_success\"}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with parameters
        /// </summary>
        [Test]
        public void ResourceInvoker_WithParameters_PassesParametersCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };
            
            // Act
            var result = ResourceInvoker.InvokeResource("test_resource_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with default parameter
        /// </summary>
        [Test]
        public void ResourceInvoker_WithDefaultParameter_UsesDefaultValue()
        {
            // Act - don't provide the parameter
            var result = ResourceInvoker.InvokeResource("test_resource_with_default", null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"default_value\"}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with type conversion
        /// </summary>
        [Test]
        public void ResourceInvoker_WithTypeConversion_ConvertsParametersCorrectly()
        {
            // Arrange - provide param2 as string instead of int
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", "42" }  // String instead of int
            };
            
            // Act
            var result = ResourceInvoker.InvokeResource("test_resource_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with nonexistent resource
        /// </summary>
        [Test]
        public void ResourceInvoker_WithNonexistentResource_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ResourceInvoker.InvokeResource("nonexistent_resource", null)
            );
        }
        
        /// <summary>
        /// Test ResourceInvoker with missing required parameter
        /// </summary>
        [Test]
        public void ResourceInvoker_WithMissingRequiredParameter_ThrowsArgumentException()
        {
            // Arrange - missing required param2
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" }
                // param2 is missing
            };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ResourceInvoker.InvokeResource("test_resource_with_params", parameters)
            );
        }
        
        /// <summary>
        /// Test ToolInvoker with a simple tool
        /// </summary>
        [Test]
        public void ToolInvoker_WithSimpleTool_ReturnsResult()
        {
            // Act
            var result = ToolInvoker.InvokeTool("test_tool", null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"tool_success\"}", result);
        }
        
        /// <summary>
        /// Test ToolInvoker with parameters
        /// </summary>
        [Test]
        public void ToolInvoker_WithParameters_PassesParametersCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("test_tool_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
    }
}