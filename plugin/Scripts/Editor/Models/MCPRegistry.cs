using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Reflection;
using YetAnotherUnityMcp.Editor.Commands;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Registry for MCP tools and resources
    /// </summary>
    public class MCPRegistry
    {
        private static MCPRegistry _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static MCPRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MCPRegistry();
                    _instance.RegisterBuiltInTools();
                    _instance.RegisterBuiltInResources();
                }
                return _instance;
            }
        }
        
        private readonly MCPSchemaCollection _schema = new MCPSchemaCollection();
        
        /// <summary>
        /// Get all registered tools and resources
        /// </summary>
        public MCPSchemaCollection Schema => _schema;
        
        /// <summary>
        /// Get all tools and resources as JSON
        /// </summary>
        public string GetSchemaAsJson()
        {
            return JsonConvert.SerializeObject(_schema, Formatting.Indented);
        }
        
        /// <summary>
        /// Get a resource descriptor by name
        /// </summary>
        /// <param name="resourceName">Name of the resource to find</param>
        /// <returns>Resource descriptor or null if not found</returns>
        public ResourceDescriptor GetResourceByName(string resourceName)
        {
            return _schema.Resources.Find(r => r.Name == resourceName);
        }
        
        /// <summary>
        /// Get a tool descriptor by name
        /// </summary>
        /// <param name="toolName">Name of the tool to find</param>
        /// <returns>Tool descriptor or null if not found</returns>
        public ToolDescriptor GetToolByName(string toolName)
        {
            return _schema.Tools.Find(t => t.Name == toolName);
        }
        
        /// <summary>
        /// Get the type with MCPResource attribute matching a resource name
        /// </summary>
        /// <param name="resourceName">Name of the resource</param>
        /// <returns>Type of the resource handler or null if not found</returns>
        public Type GetResourceHandlerType(string resourceName)
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t => {
                    var attr = t.GetCustomAttribute<MCPResourceAttribute>();
                    return attr != null && attr.Name == resourceName;
                });
        }
        
        /// <summary>
        /// Get the type with MCPTool attribute matching a tool name
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <returns>Type of the tool handler or null if not found</returns>
        public Type GetToolHandlerType(string toolName)
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t => {
                    var attr = t.GetCustomAttribute<MCPToolAttribute>();
                    return attr != null && attr.Name == toolName;
                });
        }
        
        /// <summary>
        /// Register a tool
        /// </summary>
        public void RegisterTool(ToolDescriptor tool)
        {
            // Check if tool with same name already exists
            int existingIndex = _schema.Tools.FindIndex(t => t.Name == tool.Name);
            if (existingIndex >= 0)
            {
                // Replace existing tool
                _schema.Tools[existingIndex] = tool;
                Debug.Log($"[MCP Registry] Updated tool: {tool.Name}");
            }
            else
            {
                // Add new tool
                _schema.Tools.Add(tool);
                Debug.Log($"[MCP Registry] Registered tool: {tool.Name}");
            }
        }
        
        /// <summary>
        /// Register a resource
        /// </summary>
        public void RegisterResource(ResourceDescriptor resource)
        {
            // Check if resource with same name already exists
            int existingIndex = _schema.Resources.FindIndex(r => r.Name == resource.Name);
            if (existingIndex >= 0)
            {
                // Replace existing resource
                _schema.Resources[existingIndex] = resource;
                Debug.Log($"[MCP Registry] Updated resource: {resource.Name}");
            }
            else
            {
                // Add new resource
                _schema.Resources.Add(resource);
                Debug.Log($"[MCP Registry] Registered resource: {resource.Name}");
            }
        }
        
        /// <summary>
        /// Register built-in tools
        /// </summary>
        private void RegisterBuiltInTools()
        {
            RegisterCommandsFromAssembly(Assembly.GetExecutingAssembly());
        }
        
        /// <summary>
        /// Register all command classes with MCPTool attributes from an assembly
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        private void RegisterCommandsFromAssembly(Assembly assembly)
        {
            try
            {
                // Get all types from the assembly
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    // Check if the type has the MCPTool attribute
                    if (type.GetCustomAttribute<MCPToolAttribute>() != null)
                    {
                        // Check if it's a command class (ends with "Command")
                        if (type.Name.EndsWith("Command"))
                        {
                            RegisterCommandFromType(type);
                        }
                        else
                        {
                            // Otherwise, it's a model-based tool
                            RegisterToolFromType(type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPRegistry] Error scanning assembly for commands: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register a command class as a tool using reflection and introspection
        /// </summary>
        /// <param name="type">Command class type</param>
        private void RegisterCommandFromType(Type type)
        {
            try
            {
                ToolDescriptor descriptor = MCPAttributeUtil.CreateToolDescriptorFromCommandType(type);
                if (descriptor != null)
                {
                    RegisterTool(descriptor);
                    Debug.Log($"[MCPRegistry] Registered command class {type.Name} as tool {descriptor.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPRegistry] Error registering command {type.Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register a tool descriptor from a model type using reflection and attributes
        /// </summary>
        /// <param name="type">Type to create tool descriptor from</param>
        private void RegisterToolFromType(Type type)
        {
            ToolDescriptor descriptor = MCPAttributeUtil.CreateToolDescriptorFromType(type);
            if (descriptor != null)
            {
                RegisterTool(descriptor);
            }
        }

        /// <summary>
        /// Register built-in resources
        /// </summary>
        private void RegisterBuiltInResources() 
            => RegisterResourceHandlersFromAssembly(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Register all resource handler classes with MCPResource attributes from an assembly
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        private void RegisterResourceHandlersFromAssembly(Assembly assembly)
        {
            try
            {
                // Get all types from the assembly
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    // Check if the type has the MCPResource attribute
                    if (type.GetCustomAttribute<MCPResourceAttribute>() != null)
                    {
                        // Check if it's a command class (ends with "Command")
                        if (type.Name.EndsWith("Command"))
                        {
                            // Command class marked as a resource
                            RegisterResourceHandlerFromType(type);
                        }
                        // Check if it's a resource handler class (ends with "Resource")
                        else if (type.Name.EndsWith("Resource") && !type.Name.EndsWith("ModelResource"))
                        {
                            RegisterResourceHandlerFromType(type);
                        }
                        else
                        {
                            // Otherwise, it's a model-based resource
                            RegisterResourceFromType(type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPRegistry] Error scanning assembly for resource handlers: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register a resource handler class as a resource using reflection and introspection
        /// </summary>
        /// <param name="type">Resource handler class type</param>
        private void RegisterResourceHandlerFromType(Type type)
        {
            try
            {
                ResourceDescriptor descriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(type);
                if (descriptor != null)
                {
                    RegisterResource(descriptor);
                    Debug.Log($"[MCPRegistry] Registered resource handler {type.Name} as resource {descriptor.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPRegistry] Error registering resource handler {type.Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register a resource descriptor from a model type using reflection and attributes
        /// </summary>
        /// <param name="type">Type to create resource descriptor from</param>
        private void RegisterResourceFromType(Type type)
        {
            ResourceDescriptor descriptor = MCPAttributeUtil.CreateResourceDescriptorFromType(type);
            if (descriptor != null)
            {
                RegisterResource(descriptor);
            }
        }
    }
}