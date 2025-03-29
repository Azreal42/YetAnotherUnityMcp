using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Describes a parameter for a tool or resource
    /// </summary>
    [Serializable]
    public class ParameterDescriptor
    {
        /// <summary>
        /// The type of the parameter (string, number, boolean, etc.)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Description of the parameter
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the parameter is required (not serialized, used internally)
        /// </summary>
        [JsonIgnore]
        public bool IsRequired { get; set; }
    }
    
    /// <summary>
    /// Describes a schema for input or output data
    /// </summary>
    [Serializable]
    public class Schema
    {
        /// <summary>
        /// The type of the schema (usually "object")
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "object";
        
        /// <summary>
        /// Properties of the schema, mapping parameter names to their descriptors
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, ParameterDescriptor> Properties { get; set; } = new Dictionary<string, ParameterDescriptor>();
        
        /// <summary>
        /// List of required properties
        /// </summary>
        [JsonProperty("required")]
        public List<string> Required { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Schema specifically for tool inputs (alias for Schema for backward compatibility)
    /// </summary>
    [Serializable]
    public class InputSchema : Schema
    {
    }
    
    /// <summary>
    /// Content types for MCP responses
    /// </summary>
    public enum ContentType
    {
        Text,
        Image,
        Embedded
    }
    
    /// <summary>
    /// Represents a content item in an MCP response
    /// </summary>
    [Serializable]
    public class ContentItem
    {
        /// <summary>
        /// Type of content in this item
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Text content (for type = "text")
        /// </summary>
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
        
        /// <summary>
        /// Image content (for type = "image")
        /// </summary>
        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public ImageContent Image { get; set; }
        
        /// <summary>
        /// Embedded content (for type = "embedded")
        /// </summary>
        [JsonProperty("embedded", NullValueHandling = NullValueHandling.Ignore)]
        public EmbeddedContent Embedded { get; set; }
    }
    
    /// <summary>
    /// Represents image content in an MCP response
    /// </summary>
    [Serializable]
    public class ImageContent
    {
        /// <summary>
        /// URL of the image
        /// </summary>
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }
        
        /// <summary>
        /// Base64 encoded image data
        /// </summary>
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data { get; set; }
        
        /// <summary>
        /// MIME type of the image
        /// </summary>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }
    
    /// <summary>
    /// Represents embedded content in an MCP response
    /// </summary>
    [Serializable]
    public class EmbeddedContent
    {
        /// <summary>
        /// Resource URI for the embedded content
        /// </summary>
        [JsonProperty("resourceUri")]
        public string ResourceUri { get; set; }
    }
    
    /// <summary>
    /// Represents an MCP response
    /// </summary>
    [Serializable]
    public class MCPResponse
    {
        /// <summary>
        /// Content items in the response
        /// </summary>
        [JsonProperty("content")]
        public List<ContentItem> Content { get; set; } = new List<ContentItem>();
        
        /// <summary>
        /// Whether this response represents an error
        /// </summary>
        [JsonProperty("isError")]
        public bool IsError { get; set; }
        
        /// <summary>
        /// Create a simple text response
        /// </summary>
        /// <param name="text">Text content</param>
        /// <param name="isError">Whether this is an error response</param>
        /// <returns>A new MCP response with text content</returns>
        public static MCPResponse CreateTextResponse(string text, bool isError = false)
        {
            return new MCPResponse
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = text
                    }
                },
                IsError = isError
            };
        }
        
        /// <summary>
        /// Create an image response
        /// </summary>
        /// <param name="imageUrl">URL of the image</param>
        /// <param name="mimeType">MIME type of the image</param>
        /// <returns>A new MCP response with image content</returns>
        public static MCPResponse CreateImageResponse(string imageUrl, string mimeType = "image/png")
        {
            return new MCPResponse
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "image",
                        Image = new ImageContent
                        {
                            Url = imageUrl,
                            MimeType = mimeType
                        }
                    }
                },
                IsError = false
            };
        }
        
        /// <summary>
        /// Create an image response with base64 encoded data
        /// </summary>
        /// <param name="base64Data">Base64 encoded image data</param>
        /// <param name="mimeType">MIME type of the image</param>
        /// <param name="caption">Optional caption text to include</param>
        /// <returns>A new MCP response with image content</returns>
        public static MCPResponse CreateBase64ImageResponse(string base64Data, string mimeType = "image/jpeg", string caption = null)
        {
            var response = new MCPResponse
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "image",
                        Image = new ImageContent
                        {
                            Data = base64Data,
                            MimeType = mimeType
                        }
                    }
                },
                IsError = false
            };
            
            // Add caption if provided
            if (!string.IsNullOrEmpty(caption))
            {
                response.Content.Add(new ContentItem
                {
                    Type = "text",
                    Text = caption
                });
            }
            
            return response;
        }
        
        /// <summary>
        /// Create an embedded resource response
        /// </summary>
        /// <param name="resourceUri">URI of the embedded resource</param>
        /// <returns>A new MCP response with embedded content</returns>
        public static MCPResponse CreateEmbeddedResponse(string resourceUri)
        {
            return new MCPResponse
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "embedded",
                        Embedded = new EmbeddedContent
                        {
                            ResourceUri = resourceUri
                        }
                    }
                },
                IsError = false
            };
        }
        
        /// <summary>
        /// Create an error response
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>A new MCP response with error content</returns>
        public static MCPResponse CreateErrorResponse(string errorMessage)
        {
            return new MCPResponse
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = errorMessage
                    }
                },
                IsError = true
            };
        }
    }
    
    /// <summary>
    /// Describes a tool that can be invoked by a client
    /// </summary>
    [Serializable]
    public class ToolDescriptor
    {
        /// <summary>
        /// Name of the tool
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the tool
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }
        
        /// <summary>
        /// Input schema for the tool's parameters
        /// </summary>
        [JsonProperty("inputSchema")]
        public InputSchema InputSchema { get; set; } = new InputSchema();
        
        /// <summary>
        /// Example code showing how to use this tool (not in official spec, kept for backward compatibility)
        /// </summary>
        [JsonProperty("example", NullValueHandling = NullValueHandling.Ignore)]
        public string Example { get; set; }
        
        /// <summary>
        /// Method info for direct method invocation (used with container-based tools)
        /// </summary>
        [JsonIgnore]
        public MethodInfo MethodInfo { get; set; }
        
        /// <summary>
        /// Container type where the method is defined (used with container-based tools)
        /// </summary>
        [JsonIgnore]
        public Type ContainerType { get; set; }

        /// <summary>
        /// Whether the tool should run in a separate thread
        /// </summary>
        [JsonIgnore]
        public bool RunInSeparateThread { get; set; } = false;
    }
    
    /// <summary>
    /// Describes a resource that can be accessed by a client
    /// </summary>
    [Serializable]
    public class ResourceDescriptor
    {
        /// <summary>
        /// Name of the resource
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the resource
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }
        
        /// <summary>
        /// URI for accessing the resource (renamed from urlPattern to match spec)
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }
        
        /// <summary>
        /// URL pattern for backward compatibility (not serialized)
        /// </summary>
        [JsonIgnore]
        public string UrlPattern 
        { 
            get { return Uri; }
            set { Uri = value; }
        }
        
        /// <summary>
        /// MIME type of the resource (optional)
        /// </summary>
        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; } = "application/json";
        
        /// <summary>
        /// Size of the resource in bytes (optional)
        /// </summary>
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public long? Size { get; set; }
        
        /// <summary>
        /// Parameters that can be used in the URL pattern
        /// </summary>
        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ParameterDescriptor> Parameters { get; set; } = new Dictionary<string, ParameterDescriptor>();
        
        /// <summary>
        /// Example URL showing how to access this resource (not in official spec, kept for backward compatibility)
        /// </summary>
        [JsonProperty("example", NullValueHandling = NullValueHandling.Ignore)]
        public string Example { get; set; }
        
        /// <summary>
        /// Method info for direct method invocation (used with container-based resources)
        /// </summary>
        [JsonIgnore]
        public MethodInfo MethodInfo { get; set; }
        
        /// <summary>
        /// Container type where the method is defined (used with container-based resources)
        /// </summary>
        [JsonIgnore]
        public Type ContainerType { get; set; }
    }
    
    /// <summary>
    /// Collection of tool and resource descriptors
    /// </summary>
    [Serializable]
    public class MCPSchemaCollection
    {
        /// <summary>
        /// List of available tools
        /// </summary>
        [JsonProperty("tools")]
        public List<ToolDescriptor> Tools { get; set; } = new List<ToolDescriptor>();
        
        /// <summary>
        /// List of available resources
        /// </summary>
        [JsonProperty("resources")]
        public List<ResourceDescriptor> Resources { get; set; } = new List<ResourceDescriptor>();
    }
}