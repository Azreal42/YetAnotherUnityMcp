using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YetAnotherUnityMcp.Editor.Commands;
using YetAnotherUnityMcp.Editor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Integration tests for GetSchemaCommand
    /// </summary>
    public class GetSchemaCommandTests
    {
        [Test]
        public void Execute_ReturnsValidSchemaJson()
        {
            // Act
            string result = YetAnotherUnityMcp.Editor.Commands.EditorMcpContainer.GetSchema();
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            
            // Try parsing the JSON
            JObject schemaJson = null;
            bool isValidJson = true;
            
            try
            {
                schemaJson = JObject.Parse(result);
            }
            catch (Exception)
            {
                isValidJson = false;
            }
            
            Assert.IsTrue(isValidJson, "Result should be valid JSON");
            Assert.IsNotNull(schemaJson, "Parsed JSON should not be null");
            
            // Check for basic structure
            Assert.IsTrue(schemaJson.ContainsKey("tools"), "JSON should contain 'tools' property");
            Assert.IsTrue(schemaJson.ContainsKey("resources"), "JSON should contain 'resources' property");
            
            // Check that tools array exists and has items
            JArray toolsArray = schemaJson["tools"] as JArray;
            Assert.IsNotNull(toolsArray, "Tools array should not be null");
            
            // The get_schema tool should be in the list
            bool foundGetSchemaTool = false;
            foreach (var tool in toolsArray)
            {
                if (tool["name"]?.ToString() == "get_schema")
                {
                    foundGetSchemaTool = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundGetSchemaTool, "Schema should include the get_schema tool");
        }
        
        [Test]
        public void Execute_ToolsHaveRequiredProperties()
        {
            // Act
            string result = YetAnotherUnityMcp.Editor.Commands.EditorMcpContainer.GetSchema();
            JObject schemaJson = JObject.Parse(result);
            JArray toolsArray = schemaJson["tools"] as JArray;
            
            // Assert
            foreach (var tool in toolsArray)
            {
                string toolName = tool["name"]?.ToString();
                Assert.IsNotNull(toolName, $"Tool should have a name");
                Assert.IsNotNull(tool["description"], $"Tool {toolName} should have a description");
                Assert.IsNotNull(tool["inputSchema"], $"Tool {toolName} should have an inputSchema");
                Assert.IsNotNull(tool["outputSchema"], $"Tool {toolName} should have an outputSchema");
                
                // Check input schema has required properties
                JObject inputSchema = tool["inputSchema"] as JObject;
                Assert.IsNotNull(inputSchema["properties"], $"Tool {toolName} inputSchema should have properties");
                Assert.IsNotNull(inputSchema["required"], $"Tool {toolName} inputSchema should have required list");
                
                // Check output schema has required properties
                JObject outputSchema = tool["outputSchema"] as JObject;
                Assert.IsNotNull(outputSchema["properties"], $"Tool {toolName} outputSchema should have properties");
            }
        }
        
        [Test]
        public void Execute_ResourcesHaveRequiredProperties()
        {
            // Act
            string result = YetAnotherUnityMcp.Editor.Commands.EditorMcpContainer.GetSchema();
            JObject schemaJson = JObject.Parse(result);
            
            // Resources might be an empty array if no resources are registered, which is valid
            if (!schemaJson.ContainsKey("resources"))
            {
                Assert.Inconclusive("No resources registered to test");
                return;
            }
            
            JArray resourcesArray = schemaJson["resources"] as JArray;
            if (resourcesArray.Count == 0)
            {
                Assert.Inconclusive("No resources registered to test");
                return;
            }
            
            // Assert
            foreach (var resource in resourcesArray)
            {
                string resourceName = resource["name"]?.ToString();
                Assert.IsNotNull(resourceName, $"Resource should have a name");
                Assert.IsNotNull(resource["description"], $"Resource {resourceName} should have a description");
                Assert.IsNotNull(resource["urlPattern"], $"Resource {resourceName} should have a urlPattern");
                Assert.IsNotNull(resource["parameters"], $"Resource {resourceName} should have parameters");
                Assert.IsNotNull(resource["outputSchema"], $"Resource {resourceName} should have an outputSchema");
                
                // Check output schema has required properties
                JObject outputSchema = resource["outputSchema"] as JObject;
                Assert.IsNotNull(outputSchema["properties"], $"Resource {resourceName} outputSchema should have properties");
            }
        }
        
        [Test]
        public void EditorMcpContainer_SchemaMethodExists()
        {
            // Get the method info to check existence
            Type containerType = typeof(YetAnotherUnityMcp.Editor.Commands.EditorMcpContainer);
            var methodInfo = containerType.GetMethod("GetSchema");
            
            // Assert
            Assert.IsNotNull(methodInfo, "GetSchema method should exist");
            Assert.AreEqual("String", methodInfo.ReturnType.Name, "Return type should be String");
            
            // Check if it has the MCPResource attribute
            var resourceAttr = methodInfo.GetCustomAttributes(typeof(MCPResourceAttribute), false);
            Assert.IsTrue(resourceAttr.Length > 0, "Method should have MCPResource attribute");
            
            // Check the resource is properly registered in the registry
            var registry = MCPRegistry.Instance;
            var resources = registry.Schema.Resources;
            bool foundResource = false;
            
            foreach (var resource in resources)
            {
                if (resource.Name.Contains("schema"))
                {
                    foundResource = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundResource, "Schema resource should be registered");
        }
    }
}