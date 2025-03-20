using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

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
            // Use reflection to find and register all tool descriptors from attributed classes
            RegisterToolFromType(typeof(ExecuteCodeTool));
            RegisterToolFromType(typeof(TakeScreenshotTool));
            RegisterToolFromType(typeof(ModifyObjectTool));
            RegisterToolFromType(typeof(GetLogsTool));
            RegisterToolFromType(typeof(GetUnityInfoTool));
            RegisterToolFromType(typeof(GetSchemaTool));
        }
        
        /// <summary>
        /// Register a tool descriptor from a type using reflection and attributes
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
        {
            // Use reflection to find and register all resource descriptors from attributed classes
            RegisterResourceFromType(typeof(UnityInfoResource));
            RegisterResourceFromType(typeof(UnityLogsResource));
            RegisterResourceFromType(typeof(UnitySceneResource));
            RegisterResourceFromType(typeof(UnityObjectResource));
            RegisterResourceFromType(typeof(UnitySchemaResource));
        }
        
        /// <summary>
        /// Register a resource descriptor from a type using reflection and attributes
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