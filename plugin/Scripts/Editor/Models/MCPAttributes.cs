using System;
using System.Collections.Generic;
using System.Reflection;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Base attribute for MCP schema documentation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class MCPDocumentedAttribute : Attribute
    {
        /// <summary>
        /// Description of the element
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Create a new documented attribute
        /// </summary>
        /// <param name="description">Description of the element</param>
        public MCPDocumentedAttribute(string description)
        {
            Description = description;
        }
    }
    
    /// <summary>
    /// Attribute to mark a class as an MCP Container
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MCPContainerAttribute : Attribute
    {
        /// <summary>
        /// Name prefix for tools and resources in this container
        /// </summary>
        public string NamePrefix { get; set; }
        
        /// <summary>
        /// Description of the container
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Create a new MCP container attribute
        /// </summary>
        /// <param name="namePrefix">Name prefix for tools and resources in this container</param>
        /// <param name="description">Description of the container</param>
        public MCPContainerAttribute(string namePrefix = null, string description = null)
        {
            NamePrefix = namePrefix;
            Description = description;
        }
    }
    
    /// <summary>
    /// Attribute to mark a method or class as an MCP Tool
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class MCPToolAttribute : MCPDocumentedAttribute
    {
        /// <summary>
        /// Name of the tool. If null, will be inferred from the method name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Example usage of the tool
        /// </summary>
        public string Example { get; set; }
        
        /// <summary>
        /// Create a new MCP tool attribute
        /// </summary>
        /// <param name="name">Name of the tool. If null, will be inferred from the method name.</param>
        /// <param name="description">Description of the tool</param>
        /// <param name="example">Example usage of the tool</param>
        public MCPToolAttribute(string name = null, string description = null, string example = null) : base(description)
        {
            Name = name;
            Example = example;
        }
    }
    
    /// <summary>
    /// Attribute to mark a method or class as an MCP Resource
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class MCPResourceAttribute : MCPDocumentedAttribute
    {
        /// <summary>
        /// Name of the resource. If null, will be inferred from the method name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// URI pattern for accessing the resource (formerly UrlPattern)
        /// </summary>
        public string UrlPattern { get; set; }
        
        /// <summary>
        /// MIME type of the resource
        /// </summary>
        public string MimeType { get; set; } = "application/json";
        
        /// <summary>
        /// Example usage of the resource
        /// </summary>
        public string Example { get; set; }
        
        /// <summary>
        /// Create a new MCP resource attribute
        /// </summary>
        /// <param name="name">Name of the resource. If null, will be inferred from the method name.</param>
        /// <param name="description">Description of the resource</param>
        /// <param name="urlPattern">URI pattern for accessing the resource (formerly UrlPattern)</param>
        /// <param name="example">Example usage of the resource</param>
        /// <param name="mimeType">MIME type of the resource</param>
        public MCPResourceAttribute(string name = null, string description = null, string urlPattern = null, string example = null, string mimeType = "application/json") : base(description)
        {
            Name = name;
            UrlPattern = urlPattern;
            Example = example;
            MimeType = mimeType;
        }
    }
    
    /// <summary>
    /// Attribute to mark a class as an input or output schema
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MCPSchemaAttribute : MCPDocumentedAttribute
    {
        /// <summary>
        /// Type of the schema (input or output)
        /// </summary>
        public string SchemaType { get; set; }
        
        /// <summary>
        /// Create a new MCP schema attribute
        /// </summary>
        /// <param name="description">Description of the schema</param>
        /// <param name="schemaType">Type of the schema (input or output)</param>
        public MCPSchemaAttribute(string description, string schemaType = "input") : base(description)
        {
            SchemaType = schemaType;
        }
    }
    
    /// <summary>
    /// Attribute to mark a property as a parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class MCPParameterAttribute : MCPDocumentedAttribute
    {
        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the parameter (string, number, boolean, etc.)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Whether the parameter is required
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Legacy property for backwards compatibility (forwards to IsRequired)
        /// </summary>
        public bool Required 
        { 
            get { return IsRequired; }
            set { IsRequired = value; }
        }
        
        /// <summary>
        /// Create a new MCP parameter attribute
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="type">Type of the parameter</param>
        /// <param name="required">Whether the parameter is required</param>
        public MCPParameterAttribute(string name = null, string description = null, string type = "string", bool required = false) : base(description)
        {
            Name = name;
            Type = type;
            IsRequired = required;
        }
    }
}