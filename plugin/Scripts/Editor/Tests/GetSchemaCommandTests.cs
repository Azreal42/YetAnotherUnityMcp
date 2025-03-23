using System;
using System.Linq;
using NUnit.Framework;
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
            string result = MCPRegistry.Instance.GetSchemaAsJson();
            
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
        }
        
        [Test]
        public void Execute_ToolsHaveRequiredProperties()
        {
            // Act
            string result = MCPRegistry.Instance.GetSchemaAsJson();
            JObject schemaJson = JObject.Parse(result);
            JArray toolsArray = schemaJson["tools"] as JArray;
            
            // Assert
            foreach (var tool in toolsArray)
            {
                string toolName = tool["name"]?.ToString();
                Assert.IsNotNull(toolName, $"Tool should have a name");
                Assert.IsNotNull(tool["description"], $"Tool {toolName} should have a description");
                Assert.IsNotNull(tool["inputSchema"], $"Tool {toolName} should have an inputSchema");
                
                // OutputSchema has been removed in compliance with MCP spec
                Assert.IsFalse(((JObject)tool).ContainsKey("outputSchema"), $"Tool {toolName} should not have an outputSchema according to MCP spec");
                
                // Check input schema has required properties
                JObject inputSchema = tool["inputSchema"] as JObject;
                Assert.IsNotNull(inputSchema["properties"], $"Tool {toolName} inputSchema should have properties");
                Assert.IsNotNull(inputSchema["required"], $"Tool {toolName} inputSchema should have required list");
                Assert.IsTrue(inputSchema["required"].Type == JTokenType.Array, $"Tool {toolName} inputSchema required should be an array");
                
                // Per MCP schema, inputSchema must have type: "object"
                Assert.AreEqual("object", inputSchema["type"]?.ToString(), 
                    $"Tool {toolName} inputSchema must have 'type': 'object' according to MCP spec");
                
                // If example is present, it should be a string
                if (((JObject)tool).ContainsKey("example"))
                {
                    Assert.IsTrue(tool["example"].Type == JTokenType.String, 
                        $"Tool {toolName} example should be a string if present");
                }
            }
        }
        
        [Test]
        public void Execute_ResourcesHaveRequiredProperties()
        {
            // Act
            string result = MCPRegistry.Instance.GetSchemaAsJson();
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
                
                // Check that the resource has a uri property (renamed from urlPattern)
                Assert.IsNotNull(resource["uri"], $"Resource {resourceName} should have a uri");
                
                // URI must be a valid URI format according to MCP spec
                string uri = resource["uri"].ToString();
                Assert.IsTrue(uri.StartsWith("unity://") || Uri.IsWellFormedUriString(uri, UriKind.Absolute), 
                    $"Resource {resourceName} uri '{uri}' must be a valid URI format");
                
                Assert.IsNotNull(resource["parameters"], $"Resource {resourceName} should have parameters");
                
                // OutputSchema has been removed in compliance with MCP spec
                Assert.IsFalse(((JObject)resource).ContainsKey("outputSchema"), 
                    $"Resource {resourceName} should not have an outputSchema according to MCP spec");
                
                // Check mimeType property is present (might be null or have a value)
                Assert.IsTrue(((JObject)resource).ContainsKey("mimeType"), 
                    $"Resource {resourceName} should have a mimeType property");
                
                // If example is present, it should be a string
                if (((JObject)resource).ContainsKey("example"))
                {
                    Assert.IsTrue(resource["example"].Type == JTokenType.String, 
                        $"Resource {resourceName} example should be a string if present");
                }
                
                // Check annotations if present
                if (((JObject)resource).ContainsKey("annotations"))
                {
                    var annotations = resource["annotations"] as JObject;
                    Assert.IsNotNull(annotations, $"Resource {resourceName} annotations should be an object");
                    
                    // If audience exists, it should be a valid value
                    if (annotations.ContainsKey("audience"))
                    {
                        string audience = annotations["audience"].ToString();
                        Assert.IsTrue(audience == "user" || audience == "assistant" || audience == "both", 
                            $"Resource {resourceName} audience should be 'user', 'assistant', or 'both'");
                    }
                    
                    // If priority exists, it should be a number between 0 and 1
                    if (annotations.ContainsKey("priority"))
                    {
                        double priority = annotations["priority"].Value<double>();
                        Assert.IsTrue(priority >= 0 && priority <= 1, 
                            $"Resource {resourceName} priority should be between 0 and 1");
                    }
                }
            }
        }
        
        [Test]
        public void MCPResponse_CreateTextResponse_ValidateStructure()
        {
            // Arrange
            string testMessage = "Test message";
            
            // Act
            MCPResponse response = MCPResponse.CreateTextResponse(testMessage);
            string jsonResponse = JsonConvert.SerializeObject(response);
            JObject parsedResponse = JObject.Parse(jsonResponse);
            
            // Assert
            Assert.IsNotNull(parsedResponse["content"], "Response should have content array");
            Assert.IsTrue(parsedResponse["content"].Type == JTokenType.Array, "Content should be an array");
            Assert.AreEqual(1, ((JArray)parsedResponse["content"]).Count, "Content array should have 1 item");
            
            var contentItem = parsedResponse["content"][0];
            Assert.AreEqual("text", contentItem["type"].ToString(), "Content type should be 'text'");
            Assert.AreEqual(testMessage, contentItem["text"].ToString(), "Content text should match input");
            Assert.AreEqual(false, parsedResponse["isError"].Value<bool>(), "isError should be false");
        }
        
        [Test]
        public void MCPResponse_CreateErrorResponse_ValidateStructure()
        {
            // Arrange
            string errorMessage = "Test error";
            
            // Act
            MCPResponse response = MCPResponse.CreateErrorResponse(errorMessage);
            string jsonResponse = JsonConvert.SerializeObject(response);
            JObject parsedResponse = JObject.Parse(jsonResponse);
            
            // Assert
            Assert.IsNotNull(parsedResponse["content"], "Response should have content array");
            Assert.IsTrue(parsedResponse["content"].Type == JTokenType.Array, "Content should be an array");
            Assert.AreEqual(1, ((JArray)parsedResponse["content"]).Count, "Content array should have 1 item");
            
            var contentItem = parsedResponse["content"][0];
            Assert.AreEqual("text", contentItem["type"].ToString(), "Content type should be 'text'");
            Assert.AreEqual(errorMessage, contentItem["text"].ToString(), "Content text should match input");
            Assert.AreEqual(true, parsedResponse["isError"].Value<bool>(), "isError should be true");
        }
        
        [Test]
        public void MCPResponse_CreateImageResponse_ValidateStructure()
        {
            // Arrange
            string testUrl = "https://example.com/image.png";
            string testMimeType = "image/png";
            
            // Act
            MCPResponse response = MCPResponse.CreateImageResponse(testUrl, testMimeType);
            string jsonResponse = JsonConvert.SerializeObject(response);
            JObject parsedResponse = JObject.Parse(jsonResponse);
            
            // Assert
            Assert.IsNotNull(parsedResponse["content"], "Response should have content array");
            Assert.IsTrue(parsedResponse["content"].Type == JTokenType.Array, "Content should be an array");
            Assert.AreEqual(1, ((JArray)parsedResponse["content"]).Count, "Content array should have 1 item");
            
            var contentItem = parsedResponse["content"][0];
            Assert.AreEqual("image", contentItem["type"].ToString(), "Content type should be 'image'");
            Assert.IsNull(contentItem["text"], "Text property should not be present");
            
            var imageContent = contentItem["image"];
            Assert.IsNotNull(imageContent, "Image content should be present");
            Assert.AreEqual(testUrl, imageContent["url"].ToString(), "Image URL should match input");
            Assert.AreEqual(testMimeType, imageContent["mimeType"].ToString(), "Image MIME type should match input");
            
            Assert.AreEqual(false, parsedResponse["isError"].Value<bool>(), "isError should be false");
        }
        
        [Test]
        public void MCPResponse_CreateEmbeddedResponse_ValidateStructure()
        {
            // Arrange
            string testResourceUri = "unity://resource/test";
            
            // Act
            MCPResponse response = MCPResponse.CreateEmbeddedResponse(testResourceUri);
            string jsonResponse = JsonConvert.SerializeObject(response);
            JObject parsedResponse = JObject.Parse(jsonResponse);
            
            // Assert
            Assert.IsNotNull(parsedResponse["content"], "Response should have content array");
            Assert.IsTrue(parsedResponse["content"].Type == JTokenType.Array, "Content should be an array");
            Assert.AreEqual(1, ((JArray)parsedResponse["content"]).Count, "Content array should have 1 item");
            
            var contentItem = parsedResponse["content"][0];
            Assert.AreEqual("embedded", contentItem["type"].ToString(), "Content type should be 'embedded'");
            Assert.IsNull(contentItem["text"], "Text property should not be present");
            Assert.IsNull(contentItem["image"], "Image property should not be present");
            
            var embeddedContent = contentItem["embedded"];
            Assert.IsNotNull(embeddedContent, "Embedded content should be present");
            Assert.AreEqual(testResourceUri, embeddedContent["resourceUri"].ToString(), "Resource URI should match input");
            
            Assert.AreEqual(false, parsedResponse["isError"].Value<bool>(), "isError should be false");
        }
        
        [Test]
        public void Schema_ConformsToMCPSpecification()
        {
            // Act
            string result = MCPRegistry.Instance.GetSchemaAsJson();
            JObject schemaJson = JObject.Parse(result);
            
            // Assert basic structure required by MCP spec
            Assert.IsTrue(schemaJson.ContainsKey("tools"), "Schema must contain 'tools' array");
            Assert.IsTrue(schemaJson.ContainsKey("resources"), "Schema must contain 'resources' array");
            
            // The schema should only have tools and resources at the top level
            // No deprecated fields should be present
            var propertyCount = schemaJson.Properties().Count();
            Assert.AreEqual(2, propertyCount, "Schema should only contain 'tools' and 'resources' top-level properties");
            
            // Check tools array - each tool must have mandatory fields
            var tools = schemaJson["tools"] as JArray;
            foreach (var tool in tools)
            {
                var toolObj = tool as JObject;
                
                // Required fields
                Assert.IsTrue(toolObj.ContainsKey("name"), "Tool must have 'name' property");
                Assert.IsTrue(toolObj.ContainsKey("description"), "Tool must have 'description' property");
                Assert.IsTrue(toolObj.ContainsKey("inputSchema"), "Tool must have 'inputSchema' property");
                
                // Optional but common fields can be present
                // Example is optional
                
                // Make sure no deprecated fields exist
                Assert.IsFalse(toolObj.ContainsKey("outputSchema"), "Tool must not have deprecated 'outputSchema' property");
                Assert.IsFalse(toolObj.ContainsKey("parameters"), "Tool must not have deprecated 'parameters' property");
                
                // Check inputSchema structure
                var inputSchema = toolObj["inputSchema"] as JObject;
                Assert.AreEqual("object", inputSchema["type"]?.ToString(), "InputSchema type must be 'object'");
                Assert.IsTrue(inputSchema.ContainsKey("properties"), "InputSchema must have 'properties' property");
                Assert.IsTrue(inputSchema.ContainsKey("required"), "InputSchema must have 'required' property");
                Assert.IsTrue(inputSchema["required"].Type == JTokenType.Array, "InputSchema required must be an array");
            }
            
            // Check resources array - each resource must have mandatory fields
            var resources = schemaJson["resources"] as JArray;
            foreach (var resource in resources)
            {
                var resourceObj = resource as JObject;
                
                // Required fields
                Assert.IsTrue(resourceObj.ContainsKey("name"), "Resource must have 'name' property");
                Assert.IsTrue(resourceObj.ContainsKey("uri"), "Resource must have 'uri' property");
                
                // Should have other common fields
                Assert.IsTrue(resourceObj.ContainsKey("description"), "Resource should have 'description' property");
                Assert.IsTrue(resourceObj.ContainsKey("mimeType"), "Resource should have 'mimeType' property");
                Assert.IsTrue(resourceObj.ContainsKey("parameters"), "Resource should have 'parameters' property");
                
                // Make sure no deprecated fields exist
                Assert.IsFalse(resourceObj.ContainsKey("outputSchema"), "Resource must not have deprecated 'outputSchema' property");
                Assert.IsFalse(resourceObj.ContainsKey("urlPattern"), "Resource must not have deprecated 'urlPattern' property");
                
                // If annotations are present, they should be valid
                if (resourceObj.ContainsKey("annotations"))
                {
                    var annotations = resourceObj["annotations"] as JObject;
                    Assert.IsNotNull(annotations, "Annotations must be an object");
                }
            }
        }
    }
}