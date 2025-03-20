using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Test class for MCPRegistry and its integration with MCPAttributeUtil
    /// </summary>
    public class MCPRegistryTests
    {
        private MCPRegistry registry;

        [SetUp]
        public void Setup()
        {
            // Create a new registry for each test to avoid polluting tests with state
            registry = new MCPRegistry();
        }

        // Mock command class for testing registration
        [MCPTool("test_registry_tool", "Test registry tool", "test_registry_tool()")]
        public static class MockRegistryToolCommand
        {
            public static void Execute()
            {
                // Empty method for testing
            }
        }

        // Mock resource class for testing registration
        [MCPResource("test_registry_resource", "Test registry resource", "resource/{id}", "resource/123")]
        public static class MockRegistryResource
        {
            public static string GetResource(string id)
            {
                return $"Resource {id}";
            }
        }

        [Test]
        public void RegisterTool_AddsToolToSchema()
        {
            // Arrange
            var tool = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Test tool",
                Example = "test_tool()",
                InputSchema = new InputSchema(),
                OutputSchema = new Schema()
            };

            // Act
            registry.RegisterTool(tool);

            // Assert
            Assert.AreEqual(1, registry.Schema.Tools.Count, "Should have 1 registered tool");
            Assert.AreEqual("test_tool", registry.Schema.Tools[0].Name, "Tool name should match");
            Assert.AreEqual("Test tool", registry.Schema.Tools[0].Description, "Tool description should match");
        }

        [Test]
        public void RegisterTool_WithSameName_UpdatesExistingTool()
        {
            // Arrange
            var tool1 = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Original description",
                Example = "test_tool()",
                InputSchema = new InputSchema(),
                OutputSchema = new Schema()
            };

            var tool2 = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Updated description",
                Example = "test_tool(param: 123)",
                InputSchema = new InputSchema(),
                OutputSchema = new Schema()
            };

            // Act
            registry.RegisterTool(tool1);
            registry.RegisterTool(tool2);

            // Assert
            Assert.AreEqual(1, registry.Schema.Tools.Count, "Should still have 1 registered tool");
            Assert.AreEqual("test_tool", registry.Schema.Tools[0].Name, "Tool name should match");
            Assert.AreEqual("Updated description", registry.Schema.Tools[0].Description, "Tool description should be updated");
            Assert.AreEqual("test_tool(param: 123)", registry.Schema.Tools[0].Example, "Tool example should be updated");
        }

        [Test]
        public void RegisterResource_AddsResourceToSchema()
        {
            // Arrange
            var resource = new ResourceDescriptor
            {
                Name = "test_resource",
                Description = "Test resource",
                UrlPattern = "test/{id}",
                Example = "test/123",
                Parameters = new Dictionary<string, ParameterDescriptor>(),
                OutputSchema = new Schema()
            };

            // Act
            registry.RegisterResource(resource);

            // Assert
            Assert.AreEqual(1, registry.Schema.Resources.Count, "Should have 1 registered resource");
            Assert.AreEqual("test_resource", registry.Schema.Resources[0].Name, "Resource name should match");
            Assert.AreEqual("Test resource", registry.Schema.Resources[0].Description, "Resource description should match");
            Assert.AreEqual("test/{id}", registry.Schema.Resources[0].UrlPattern, "Resource URL pattern should match");
        }

        [Test]
        public void RegisterResource_WithSameName_UpdatesExistingResource()
        {
            // Arrange
            var resource1 = new ResourceDescriptor
            {
                Name = "test_resource",
                Description = "Original description",
                UrlPattern = "test/{id}",
                Example = "test/123",
                Parameters = new Dictionary<string, ParameterDescriptor>(),
                OutputSchema = new Schema()
            };

            var resource2 = new ResourceDescriptor
            {
                Name = "test_resource",
                Description = "Updated description",
                UrlPattern = "test/{id}/detail",
                Example = "test/123/detail",
                Parameters = new Dictionary<string, ParameterDescriptor>(),
                OutputSchema = new Schema()
            };

            // Act
            registry.RegisterResource(resource1);
            registry.RegisterResource(resource2);

            // Assert
            Assert.AreEqual(1, registry.Schema.Resources.Count, "Should still have 1 registered resource");
            Assert.AreEqual("test_resource", registry.Schema.Resources[0].Name, "Resource name should match");
            Assert.AreEqual("Updated description", registry.Schema.Resources[0].Description, "Resource description should be updated");
            Assert.AreEqual("test/{id}/detail", registry.Schema.Resources[0].UrlPattern, "Resource URL pattern should be updated");
        }

        [Test]
        public void GetSchemaAsJson_ReturnsValidJson()
        {
            // Arrange
            var tool = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Test tool",
                Example = "test_tool()",
                InputSchema = new InputSchema(),
                OutputSchema = new Schema()
            };

            var resource = new ResourceDescriptor
            {
                Name = "test_resource",
                Description = "Test resource",
                UrlPattern = "test/{id}",
                Example = "test/123",
                Parameters = new Dictionary<string, ParameterDescriptor>(),
                OutputSchema = new Schema()
            };

            registry.RegisterTool(tool);
            registry.RegisterResource(resource);

            // Act
            string json = registry.GetSchemaAsJson();

            // Assert
            Assert.IsNotNull(json, "JSON should not be null");
            Assert.IsTrue(json.Contains("\"tools\""), "JSON should contain tools array");
            Assert.IsTrue(json.Contains("\"resources\""), "JSON should contain resources array");
            Assert.IsTrue(json.Contains("\"test_tool\""), "JSON should contain tool name");
            Assert.IsTrue(json.Contains("\"test_resource\""), "JSON should contain resource name");
        }

        [Test]
        public void RegisterCommandFromType_CreatesAndRegistersToolDescriptor()
        {
            // This test requires reflection to call the private method
            Type registryType = typeof(MCPRegistry);
            MethodInfo registerCommandMethod = registryType.GetMethod("RegisterCommandFromType", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            registerCommandMethod.Invoke(registry, new object[] { typeof(MockRegistryToolCommand) });

            // Assert
            Assert.AreEqual(1, registry.Schema.Tools.Count, "Should have 1 registered tool");
            Assert.AreEqual("test_registry_tool", registry.Schema.Tools[0].Name, "Tool name should match");
            Assert.AreEqual("Test registry tool", registry.Schema.Tools[0].Description, "Tool description should match");
        }

        [Test]
        public void RegisterResourceHandlerFromType_CreatesAndRegistersResourceDescriptor()
        {
            // This test requires reflection to call the private method
            Type registryType = typeof(MCPRegistry);
            MethodInfo registerResourceMethod = registryType.GetMethod("RegisterResourceHandlerFromType", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            registerResourceMethod.Invoke(registry, new object[] { typeof(MockRegistryResource) });

            // Assert
            Assert.AreEqual(1, registry.Schema.Resources.Count, "Should have 1 registered resource");
            Assert.AreEqual("test_registry_resource", registry.Schema.Resources[0].Name, "Resource name should match");
            Assert.AreEqual("Test registry resource", registry.Schema.Resources[0].Description, "Resource description should match");
            Assert.AreEqual("resource/{id}", registry.Schema.Resources[0].UrlPattern, "Resource URL pattern should match");
        }
    }
}