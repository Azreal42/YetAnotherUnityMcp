using System;
using System.Collections.Generic;
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
        /// Test resource for unit tests
        /// </summary>
        [MCPResource("test_resource", "Test resource for unit tests", "unity://test", "unity://test")]
        public static class TestResource
        {
            /// <summary>
            /// Get the test resource
            /// </summary>
            public static string GetResource()
            {
                return "{\"result\": \"test_success\"}";
            }
        }
        
        /// <summary>
        /// Test resource with parameters
        /// </summary>
        [MCPResource("test_resource_with_params", "Test resource with parameters", "unity://test/{param1}/{param2}", "unity://test/value1/value2")]
        public static class TestResourceWithParams
        {
            /// <summary>
            /// Get the test resource with parameters
            /// </summary>
            public static string GetResource(string param1, int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }
        
        /// <summary>
        /// Test resource with default parameter
        /// </summary>
        [MCPResource("test_resource_with_default", "Test resource with default parameter", "unity://test/default", "unity://test/default")]
        public static class TestResourceWithDefault
        {
            /// <summary>
            /// Get the test resource with a default parameter
            /// </summary>
            public static string GetResource(string param1 = "default_value")
            {
                return $"{{\"param1\": \"{param1}\"}}";
            }
        }
        
        /// <summary>
        /// Test tool for unit tests
        /// </summary>
        [MCPTool("test_tool", "Test tool for unit tests", "test_tool()")]
        public static class TestTool
        {
            /// <summary>
            /// Execute the test tool
            /// </summary>
            public static string Execute()
            {
                return "{\"result\": \"tool_success\"}";
            }
        }
        
        /// <summary>
        /// Test tool with parameters
        /// </summary>
        [MCPTool("test_tool_with_params", "Test tool with parameters", "test_tool_with_params(\"value\", 42)")]
        public static class TestToolWithParams
        {
            /// <summary>
            /// Execute the test tool with parameters
            /// </summary>
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
            
            // Register test resources
            var testResourceDescriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(typeof(TestResource));
            registry.RegisterResource(testResourceDescriptor);
            
            var testResourceWithParamsDescriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(typeof(TestResourceWithParams));
            registry.RegisterResource(testResourceWithParamsDescriptor);
            
            var testResourceWithDefaultDescriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(typeof(TestResourceWithDefault));
            registry.RegisterResource(testResourceWithDefaultDescriptor);
            
            // Register test tools
            var testToolDescriptor = MCPAttributeUtil.CreateToolDescriptorFromCommandType(typeof(TestTool));
            registry.RegisterTool(testToolDescriptor);
            
            var testToolWithParamsDescriptor = MCPAttributeUtil.CreateToolDescriptorFromCommandType(typeof(TestToolWithParams));
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