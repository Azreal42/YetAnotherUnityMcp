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
    /// Attribute to mark a class as an MCP Tool
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MCPToolAttribute : MCPDocumentedAttribute
    {
        /// <summary>
        /// Name of the tool
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Example usage of the tool
        /// </summary>
        public string Example { get; set; }
        
        /// <summary>
        /// Create a new MCP tool attribute
        /// </summary>
        /// <param name="name">Name of the tool</param>
        /// <param name="description">Description of the tool</param>
        /// <param name="example">Example usage of the tool</param>
        public MCPToolAttribute(string name, string description, string example = null) : base(description)
        {
            Name = name;
            Example = example;
        }
    }
    
    /// <summary>
    /// Attribute to mark a class as an MCP Resource
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MCPResourceAttribute : MCPDocumentedAttribute
    {
        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// URL pattern for accessing the resource
        /// </summary>
        public string UrlPattern { get; set; }
        
        /// <summary>
        /// Example usage of the resource
        /// </summary>
        public string Example { get; set; }
        
        /// <summary>
        /// Create a new MCP resource attribute
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="description">Description of the resource</param>
        /// <param name="urlPattern">URL pattern for accessing the resource</param>
        /// <param name="example">Example usage of the resource</param>
        public MCPResourceAttribute(string name, string description, string urlPattern, string example = null) : base(description)
        {
            Name = name;
            UrlPattern = urlPattern;
            Example = example;
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
        public bool Required { get; set; }
        
        /// <summary>
        /// Create a new MCP parameter attribute
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="type">Type of the parameter</param>
        /// <param name="required">Whether the parameter is required</param>
        public MCPParameterAttribute(string name, string description, string type = "string", bool required = false) : base(description)
        {
            Name = name;
            Type = type;
            Required = required;
        }
    }
    
    /// <summary>
    /// Utility class for working with MCP attributes
    /// </summary>
    public static class MCPAttributeUtil
    {
        /// <summary>
        /// Get schema descriptor from a type using reflection and attributes
        /// </summary>
        /// <param name="type">Type to get schema for</param>
        /// <returns>Schema object</returns>
        public static Schema GetSchemaFromType(Type type)
        {
            Schema schema = new Schema();
            
            // Get schema attributes
            var schemaAttr = type.GetCustomAttribute<MCPSchemaAttribute>();
            if (schemaAttr != null)
            {
                // Process properties for parameters
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    var paramAttr = prop.GetCustomAttribute<MCPParameterAttribute>();
                    if (paramAttr != null)
                    {
                        var paramName = paramAttr.Name ?? prop.Name;
                        
                        schema.Properties[paramName] = new ParameterDescriptor
                        {
                            Type = paramAttr.Type ?? GetTypeString(prop.PropertyType),
                            Description = paramAttr.Description,
                            Required = paramAttr.Required
                        };
                        
                        if (paramAttr.Required)
                        {
                            schema.Required.Add(paramName);
                        }
                    }
                }
            }
            
            return schema;
        }
        
        /// <summary>
        /// Create a tool descriptor from a type using reflection and attributes
        /// </summary>
        /// <param name="type">Type to create tool descriptor from</param>
        /// <returns>Tool descriptor</returns>
        public static ToolDescriptor CreateToolDescriptorFromType(Type type)
        {
            var toolAttr = type.GetCustomAttribute<MCPToolAttribute>();
            if (toolAttr == null)
            {
                return null;
            }
            
            // Find input and output schema types
            Type inputSchemaType = null;
            Type outputSchemaType = null;
            
            foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public))
            {
                var schemaAttr = nestedType.GetCustomAttribute<MCPSchemaAttribute>();
                if (schemaAttr != null)
                {
                    if (schemaAttr.SchemaType == "input")
                    {
                        inputSchemaType = nestedType;
                    }
                    else if (schemaAttr.SchemaType == "output")
                    {
                        outputSchemaType = nestedType;
                    }
                }
            }
            
            // Create the tool descriptor
            var descriptor = new ToolDescriptor
            {
                Name = toolAttr.Name,
                Description = toolAttr.Description,
                Example = toolAttr.Example
            };
            
            // Add input schema
            if (inputSchemaType != null)
            {
                descriptor.InputSchema = (InputSchema)GetSchemaFromType(inputSchemaType);
            }
            
            // Add output schema
            if (outputSchemaType != null)
            {
                descriptor.OutputSchema = GetSchemaFromType(outputSchemaType);
            }
            
            return descriptor;
        }
        
        /// <summary>
        /// Create a resource descriptor from a type using reflection and attributes
        /// </summary>
        /// <param name="type">Type to create resource descriptor from</param>
        /// <returns>Resource descriptor</returns>
        public static ResourceDescriptor CreateResourceDescriptorFromType(Type type)
        {
            var resourceAttr = type.GetCustomAttribute<MCPResourceAttribute>();
            if (resourceAttr == null)
            {
                return null;
            }
            
            // Find output schema type
            Type outputSchemaType = null;
            
            foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public))
            {
                var schemaAttr = nestedType.GetCustomAttribute<MCPSchemaAttribute>();
                if (schemaAttr != null && schemaAttr.SchemaType == "output")
                {
                    outputSchemaType = nestedType;
                    break;
                }
            }
            
            // Create the resource descriptor
            var descriptor = new ResourceDescriptor
            {
                Name = resourceAttr.Name,
                Description = resourceAttr.Description,
                UrlPattern = resourceAttr.UrlPattern,
                Example = resourceAttr.Example
            };
            
            // Get parameters from URL pattern
            var parameters = new Dictionary<string, ParameterDescriptor>();
            var urlParts = resourceAttr.UrlPattern.Split('/');
            
            foreach (var part in urlParts)
            {
                if (part.StartsWith("{") && part.EndsWith("}"))
                {
                    var paramName = part.Substring(1, part.Length - 2);
                    parameters[paramName] = new ParameterDescriptor
                    {
                        Type = "string",
                        Description = $"Parameter {paramName} for this resource",
                        Required = true
                    };
                }
            }
            
            descriptor.Parameters = parameters;
            
            // Add output schema
            if (outputSchemaType != null)
            {
                descriptor.OutputSchema = GetSchemaFromType(outputSchemaType);
            }
            
            return descriptor;
        }
        
        /// <summary>
        /// Get a string representation of a C# type
        /// </summary>
        /// <param name="type">C# type</param>
        /// <returns>Type string for schema</returns>
        private static string GetTypeString(Type type)
        {
            if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(int) || type == typeof(float) || type == typeof(double))
            {
                return "number";
            }
            else if (type == typeof(bool))
            {
                return "boolean";
            }
            else if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return "array";
            }
            else
            {
                return "object";
            }
        }
    }
}