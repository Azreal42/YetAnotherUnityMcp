using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Describes a parameter for a tool
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
        /// Whether the parameter is required
        /// </summary>
        [JsonProperty("required")]
        public bool Required { get; set; }
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
        /// Output schema describing the return value of the tool
        /// </summary>
        [JsonProperty("outputSchema")]
        public Schema OutputSchema { get; set; } = new Schema();
        
        /// <summary>
        /// Example code showing how to use this tool
        /// </summary>
        [JsonProperty("example")]
        public string Example { get; set; }
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
        /// URL pattern for accessing the resource
        /// </summary>
        [JsonProperty("urlPattern")]
        public string UrlPattern { get; set; }
        
        /// <summary>
        /// Parameters that can be used in the URL pattern
        /// </summary>
        [JsonProperty("parameters")]
        public Dictionary<string, ParameterDescriptor> Parameters { get; set; } = new Dictionary<string, ParameterDescriptor>();
        
        /// <summary>
        /// Output schema describing the return value of the resource
        /// </summary>
        [JsonProperty("outputSchema")]
        public Schema OutputSchema { get; set; } = new Schema();
        
        /// <summary>
        /// Example URL showing how to access this resource
        /// </summary>
        [JsonProperty("example")]
        public string Example { get; set; }
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