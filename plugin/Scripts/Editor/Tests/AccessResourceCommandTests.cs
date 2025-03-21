using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Commands;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Tests for the AccessResourceCommand
    /// </summary>
    public class AccessResourceCommandTests
    {
        /// <summary>
        /// Test resource type used for testing
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
        /// Test resource that only has Execute method
        /// </summary>
        [MCPResource("test_execute_only", "Test resource with only Execute method", "unity://test/execute", "unity://test/execute")]
        public static class TestExecuteOnlyResource
        {
            /// <summary>
            /// Execute the test resource
            /// </summary>
            public static string Execute()
            {
                return "{\"result\": \"execute_success\"}";
            }
        }
        
        /// <summary>
        /// Setup for tests - register test resources in the registry
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Register test resources in the registry
            RegisterTestResources();
        }
        
        /// <summary>
        /// Register test resources in the registry
        /// </summary>
        private void RegisterTestResources()
        {
            // Register the test resources
            var registry = MCPRegistry.Instance;
            
            // TestResource
            var testResourceDescriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(typeof(TestResource));
            registry.RegisterResource(testResourceDescriptor);
            
            // TestResourceWithParams
            var testResourceWithParamsDescriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(typeof(TestResourceWithParams));
            registry.RegisterResource(testResourceWithParamsDescriptor);
            
            // TestExecuteOnlyResource
            var testExecuteOnlyDescriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(typeof(TestExecuteOnlyResource));
            registry.RegisterResource(testExecuteOnlyDescriptor);
        }
        
        /// <summary>
        /// Test finding resource in registry
        /// </summary>
        [Test]
        public void FindResourceInRegistry_WithExistingResource_ReturnsResourceDescriptor()
        {
            // Act
            var result = AccessResourceCommand.FindResourceInRegistry("test_resource");
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test_resource", result.Name);
            Assert.AreEqual("unity://test", result.UrlPattern);
        }
        
        /// <summary>
        /// Test finding resource in registry with non-existent resource
        /// </summary>
        [Test]
        public void FindResourceInRegistry_WithNonExistentResource_ReturnsNull()
        {
            // Act
            var result = AccessResourceCommand.FindResourceInRegistry("non_existent_resource");
            
            // Assert
            Assert.IsNull(result);
        }
        
        /// <summary>
        /// Test finding resource handler type
        /// </summary>
        [Test]
        public void FindResourceHandlerType_WithExistingResource_ReturnsHandlerType()
        {
            // Arrange
            var descriptor = AccessResourceCommand.FindResourceInRegistry("test_resource");
            
            // Act
            var result = AccessResourceCommand.FindResourceHandlerType("test_resource", descriptor);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(TestResource), result);
        }
        
        /// <summary>
        /// Test accessing a resource with no parameters
        /// </summary>
        [Test]
        public void Execute_WithExistingResource_ReturnsResourceResult()
        {
            // Act
            var result = AccessResourceCommand.Execute("test_resource", new Dictionary<string, object>());
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"test_success\"}", result);
        }
        
        /// <summary>
        /// Test accessing a resource with parameters
        /// </summary>
        [Test]
        public void Execute_WithResourceParameters_PassesParametersCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };
            
            // Act
            var result = AccessResourceCommand.Execute("test_resource_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test accessing a resource that only has Execute method
        /// </summary>
        [Test]
        public void Execute_WithExecuteOnlyResource_UsesExecuteMethod()
        {
            // Act
            var result = AccessResourceCommand.Execute("test_execute_only", new Dictionary<string, object>());
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"execute_success\"}", result);
        }
        
        /// <summary>
        /// Test accessing a non-existent resource
        /// </summary>
        [Test]
        public void Execute_WithNonExistentResource_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                AccessResourceCommand.Execute("non_existent_resource", new Dictionary<string, object>())
            );
        }
        
        /// <summary>
        /// Test type conversion for parameters
        /// </summary>
        [Test]
        public void Execute_WithParameterTypeConversion_ConvertsParametersCorrectly()
        {
            // Arrange - provide param2 as string instead of int
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", "123" }  // String instead of int
            };
            
            // Act - should automatically convert "123" to int
            var result = AccessResourceCommand.Execute("test_resource_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 123}", result);
        }
    }
}