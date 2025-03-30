using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YetAnotherUnityMcp.Editor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Tests for the MCPResponse class and content array functionality
    /// </summary>
    public class MCPResponseTests
    {
        [Test]
        public void MCPResponse_CreateTextResponse_ValidContent()
        {
            // Arrange
            string testMessage = "Test message";
            
            // Act
            MCPResponse response = MCPResponse.CreateTextResponse(testMessage);
            
            // Assert
            Assert.AreEqual(1, response.Content.Count, "Should have one content item");
            Assert.AreEqual("text", response.Content[0].Type, "Content type should be text");

            var textContent = response.Content[0] as TextContentItem;
            Assert.IsNotNull(textContent, "Content should be of type TextContentItem");
            Assert.AreEqual(testMessage, textContent.Text, "Text content should match");
            Assert.IsFalse(response.IsError, "Should not be an error response");
        }
        
        [Test]
        public void MCPResponse_CreateErrorResponse_ValidContent()
        {
            // Arrange
            string errorMessage = "Test error";
            
            // Act
            MCPResponse response = MCPResponse.CreateErrorResponse(errorMessage);
            
            // Assert
            Assert.AreEqual(1, response.Content.Count, "Should have one content item");
            Assert.AreEqual("text", response.Content[0].Type, "Content type should be text");

            var textContent = response.Content[0] as TextContentItem;
            Assert.AreEqual(errorMessage, textContent.Text, "Text content should match");
            Assert.IsTrue(response.IsError, "Should be an error response");
        }
        
        [Test]
        public void MCPResponse_CreateImageResponse_ValidContent()
        {
            // Arrange
            string imageUrl = "https://example.com/image.png";
            string mimeType = "image/png";
            
            // Act
            MCPResponse response = MCPResponse.CreateImageResponse(imageUrl, mimeType);
            
            // Assert
            Assert.AreEqual(1, response.Content.Count, "Should have one content item");
            Assert.AreEqual("image", response.Content[0].Type, "Content type should be image");
            var imageContent = response.Content[0] as ImageContent;
            Assert.AreEqual(imageUrl, imageContent.Url, "Image URL should match");
            Assert.AreEqual(mimeType, imageContent.MimeType, "MIME type should match");
            Assert.IsFalse(response.IsError, "Should not be an error response");
        }
        
        [Test]
        public void MCPResponse_CreateEmbeddedResponse_ValidContent()
        {
            // Arrange
            string resourceUri = "unity://resource/test";
            
            // Act
            MCPResponse response = MCPResponse.CreateEmbeddedResponse(resourceUri);
            
            // Assert
            Assert.AreEqual(1, response.Content.Count, "Should have one content item");
            Assert.AreEqual("embedded", response.Content[0].Type, "Content type should be embedded");
            var embeddedContent = response.Content[0] as EmbeddedContent;
            Assert.IsNotNull(embeddedContent, "Embedded should not be null");
            Assert.AreEqual(resourceUri, embeddedContent.ResourceUri, "Resource URI should match");
            Assert.IsFalse(response.IsError, "Should not be an error response");
        }
        
        [Test]
        public void MCPResponse_Json_Serialization()
        {
            // Arrange
            string testMessage = "Test message";
            MCPResponse response = MCPResponse.CreateTextResponse(testMessage);
            
            // Act
            string json = JsonConvert.SerializeObject(response);
            JObject parsed = JObject.Parse(json);
            
            // Assert
            Assert.IsTrue(parsed.ContainsKey("content"), "JSON should have content field");
            Assert.IsTrue(parsed["content"].Type == JTokenType.Array, "Content should be an array");
            Assert.AreEqual(1, ((JArray)parsed["content"]).Count, "Content should have one item");
            Assert.AreEqual("text", parsed["content"][0]["type"].ToString(), "Content type should be text");
            Assert.AreEqual(testMessage, parsed["content"][0]["text"].ToString(), "Text content should match");
            Assert.AreEqual(false, parsed["isError"].Value<bool>(), "isError should be false");
        }
        
        [Test]
        public void MCPResponse_Json_Deserialization()
        {
            // Arrange
            string json = @"{
                ""content"": [
                    {
                        ""type"": ""text"",
                        ""text"": ""Test message""
                    }
                ],
                ""isError"": false
            }";
            
            // Act
            MCPResponse response = JsonConvert.DeserializeObject<MCPResponse>(json);
            
            // Assert
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(1, response.Content.Count, "Should have one content item");
            Assert.AreEqual("text", response.Content[0].Type, "Content type should be text");

            var textContent = response.Content[0] as TextContentItem;
            Assert.AreEqual("Test message", textContent.Text, "Text content should match");
            Assert.IsFalse(response.IsError, "Should not be an error response");
        }
        
        [Test]
        public void MCPResponse_MultipleContentItems_Serialization()
        {
            // Arrange
            MCPResponse response = new MCPResponse();
            
            // Add text content
            response.Content.Add(new TextContentItem 
            { 
                Type = "text", 
                Text = "Text content" 
            });
            
            // Add image content
            response.Content.Add(new ImageContent 
            { 
                Type = "image", 
    
                Url = "https://example.com/image.jpg", 
                MimeType = "image/jpeg" 
            });
            
            // Act
            string json = JsonConvert.SerializeObject(response);
            JObject parsed = JObject.Parse(json);
            
            // Assert
            Assert.AreEqual(2, ((JArray)parsed["content"]).Count, "Content should have two items");
            Assert.AreEqual("text", parsed["content"][0]["type"].ToString(), "First item type should be text");
            Assert.AreEqual("image", parsed["content"][1]["type"].ToString(), "Second item type should be image");
            Assert.IsNull(parsed["content"][1]["text"], "Second item should not have text property");
            Assert.IsNotNull(parsed["content"][1]["image"], "Second item should have image property");
        }
        
        [Test]
        public void ContentItem_NullHandling_Serialization()
        {
            // Arrange
            TextContentItem textItem = new TextContentItem { Type = "text", Text = "Text content" };
            ImageContent imageItem = new ImageContent { Type = "image", Url = "http://example.com/img.png", MimeType = "image/png" };
            
            // Act
            string textJson = JsonConvert.SerializeObject(textItem);
            string imageJson = JsonConvert.SerializeObject(imageItem);
            JObject textParsed = JObject.Parse(textJson);
            JObject imageParsed = JObject.Parse(imageJson);
            
            // Assert
            Assert.IsTrue(textParsed.ContainsKey("text"), "Text item should have text property");
            Assert.IsFalse(textParsed.ContainsKey("image"), "Text item should not have image property");
            Assert.IsFalse(textParsed.ContainsKey("embedded"), "Text item should not have embedded property");
            
            Assert.IsFalse(imageParsed.ContainsKey("text"), "Image item should not have text property");
            Assert.IsTrue(imageParsed.ContainsKey("image"), "Image item should have image property");
            Assert.IsFalse(imageParsed.ContainsKey("embedded"), "Image item should not have embedded property");
        }
        
        [Test]
        public void MCPTcpServer_ShouldFormatToolResponsesWithContentArray()
        {
            // This is a simple test to ensure that tool responses are correctly formatted
            // with the content array structure when sent through the MCPTcpServer
            
            // Create a simple text response
            string textContent = "Test response from tool";
            MCPResponse response = MCPResponse.CreateTextResponse(textContent);
            
            // Serialize to JSON
            string jsonResponse = JsonConvert.SerializeObject(response);
            
            // Verify it has the expected structure
            JObject parsed = JObject.Parse(jsonResponse);
            Assert.IsTrue(parsed.ContainsKey("content"), "Response should have content field");
            Assert.AreEqual(1, ((JArray)parsed["content"]).Count, "Content should have one item");
            Assert.AreEqual("text", parsed["content"][0]["type"].ToString(), "Content type should be text");
            Assert.AreEqual(textContent, parsed["content"][0]["text"].ToString(), "Content text should match");
            Assert.IsFalse(parsed["isError"].Value<bool>(), "Response should not be marked as error");
        }
        
        [Test]
        public void MCPResponse_ErrorResponse_HasCorrectIsErrorFlag()
        {
            // Create an error response
            string errorMessage = "Test error message";
            MCPResponse response = MCPResponse.CreateErrorResponse(errorMessage);
            
            // Serialize to JSON
            string jsonResponse = JsonConvert.SerializeObject(response);
            
            // Verify it has the correct error flag
            JObject parsed = JObject.Parse(jsonResponse);
            Assert.IsTrue(parsed.ContainsKey("content"), "Response should have content field");
            Assert.AreEqual(1, ((JArray)parsed["content"]).Count, "Content should have one item");
            Assert.AreEqual("text", parsed["content"][0]["type"].ToString(), "Content type should be text");
            Assert.AreEqual(errorMessage, parsed["content"][0]["text"].ToString(), "Error message should match");
            Assert.IsTrue(parsed["isError"].Value<bool>(), "Response should be marked as error");
        }
        
        [Test]
        public void MCPResponse_MixedContentTypes_SerializesCorrectly()
        {
            // Create a response with multiple content types
            MCPResponse response = new MCPResponse();
            
            // Add text content
            response.Content.Add(new TextContentItem 
            { 
                Type = "text", 
                Text = "Text portion of the response" 
            });
            
            // Add image content
            response.Content.Add(new ImageContent
            { 
                Type = "image", 
                 
                Url = "https://example.com/image.png", 
                MimeType = "image/png" 
            });
            
            // Serialize to JSON
            string jsonResponse = JsonConvert.SerializeObject(response);
            
            // Verify it has the expected structure
            JObject parsed = JObject.Parse(jsonResponse);
            Assert.IsTrue(parsed.ContainsKey("content"), "Response should have content field");
            Assert.AreEqual(2, ((JArray)parsed["content"]).Count, "Content should have two items");
            
            // Check first item (text)
            Assert.AreEqual("text", parsed["content"][0]["type"].ToString(), "First item type should be text");
            Assert.AreEqual("Text portion of the response", parsed["content"][0]["text"].ToString(), "Text content should match");
            
            // Check second item (image)
            Assert.AreEqual("image", parsed["content"][1]["type"].ToString(), "Second item type should be image");
            Assert.IsNull(parsed["content"][1]["text"], "Image item should not have text property");
            Assert.IsNotNull(parsed["content"][1]["image"], "Image item should have image property");
            Assert.AreEqual("https://example.com/image.png", parsed["content"][1]["image"]["url"].ToString(), "Image URL should match");
            Assert.AreEqual("image/png", parsed["content"][1]["image"]["mimeType"].ToString(), "Image MIME type should match");
        }
    }
}