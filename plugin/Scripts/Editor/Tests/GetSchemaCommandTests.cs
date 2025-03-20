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
            string result = GetSchemaCommand.Execute();
            
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
            string result = GetSchemaCommand.Execute();
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
            string result = GetSchemaCommand.Execute();
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
        public void GetSchemaCommand_SchemaAttributesWereGenerated()
        {
            // Get the type to check its attributes were processed
            Type commandType = typeof(GetSchemaCommand);
            
            // Use MCPAttributeUtil to generate schema
            ToolDescriptor descriptor = MCPAttributeUtil.CreateToolDescriptorFromCommandType(commandType);
            
            // Assert
            Assert.IsNotNull(descriptor, "Should be able to generate descriptor");
            Assert.AreEqual("get_schema", descriptor.Name, "Name should match");
            Assert.AreEqual("Get information about available tools and resources", descriptor.Description, "Description should match");
            
            // Check input schema
            Assert.IsNotNull(descriptor.InputSchema, "Input schema should not be null");
            Assert.AreEqual(0, descriptor.InputSchema.Properties.Count, "Input schema should have no properties");
            
            // Check output schema
            Assert.IsNotNull(descriptor.OutputSchema, "Output schema should not be null");
            Assert.IsTrue(descriptor.OutputSchema.Properties.ContainsKey("result"), "Output schema should have 'result' property");
            Assert.AreEqual("string", descriptor.OutputSchema.Properties["result"].Type, "Output type should be 'string'");
        }
    }
}