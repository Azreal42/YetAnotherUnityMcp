using System;
using System.Collections.Generic;
using UnityEngine;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Execute code in Unity MCP tool
    /// </summary>
    [MCPTool("execute_code", "Execute C# code in Unity", "execute_code(\"Debug.Log(\\\"Hello from AI\\\"); return 42;\")")]
    public class ExecuteCodeTool
    {
        /// <summary>
        /// Input schema for execute_code command
        /// </summary>
        [MCPSchema("Input parameters for the execute_code command")]
        public class InputModel
        {
            /// <summary>
            /// C# code to execute
            /// </summary>
            [MCPParameter("code", "C# code to execute", "string", true)]
            public string Code { get; set; }
        }
        
        /// <summary>
        /// Output schema for execute_code command
        /// </summary>
        [MCPSchema("Output results from the execute_code command", "output")]
        public class OutputModel
        {
            /// <summary>
            /// String representation of the return value
            /// </summary>
            [MCPParameter("output", "String representation of the return value", "string", true)]
            public string Output { get; set; }
            
            /// <summary>
            /// Array of log messages generated during execution
            /// </summary>
            [MCPParameter("logs", "Array of log messages generated during execution", "array", true)]
            public List<string> Logs { get; set; }
            
            /// <summary>
            /// The actual return value (if serializable)
            /// </summary>
            [MCPParameter("returnValue", "The actual return value (if serializable)", "object", false)]
            public object ReturnValue { get; set; }
        }
    }
    
    /// <summary>
    /// Take screenshot MCP tool
    /// </summary>
    [MCPTool("take_screenshot", "Take a screenshot of the Unity Editor", "take_screenshot(output_path=\"screenshot.png\", width=1920, height=1080)")]
    public class TakeScreenshotTool
    {
        /// <summary>
        /// Input schema for take_screenshot command
        /// </summary>
        [MCPSchema("Input parameters for the take_screenshot command")]
        public class InputModel
        {
            /// <summary>
            /// Path to save the screenshot
            /// </summary>
            [MCPParameter("output_path", "Path to save the screenshot", "string", false)]
            public string OutputPath { get; set; }
            
            /// <summary>
            /// Width of the screenshot
            /// </summary>
            [MCPParameter("width", "Width of the screenshot", "number", false)]
            public int Width { get; set; }
            
            /// <summary>
            /// Height of the screenshot
            /// </summary>
            [MCPParameter("height", "Height of the screenshot", "number", false)]
            public int Height { get; set; }
        }
        
        /// <summary>
        /// Output schema for take_screenshot command
        /// </summary>
        [MCPSchema("Output results from the take_screenshot command", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Path to the saved screenshot file
            /// </summary>
            [MCPParameter("filePath", "Path to the saved screenshot file", "string", true)]
            public string FilePath { get; set; }
            
            /// <summary>
            /// Width of the captured screenshot
            /// </summary>
            [MCPParameter("width", "Width of the captured screenshot", "number", true)]
            public int Width { get; set; }
            
            /// <summary>
            /// Height of the captured screenshot
            /// </summary>
            [MCPParameter("height", "Height of the captured screenshot", "number", true)]
            public int Height { get; set; }
        }
    }
    
    /// <summary>
    /// Modify object MCP tool
    /// </summary>
    [MCPTool("modify_object", "Modify a property of a Unity GameObject", "modify_object(object_id=\"Main Camera\", property_path=\"transform.position.x\", property_value=10)")]
    public class ModifyObjectTool
    {
        /// <summary>
        /// Input schema for modify_object command
        /// </summary>
        [MCPSchema("Input parameters for the modify_object command")]
        public class InputModel
        {
            /// <summary>
            /// ID or path of the GameObject
            /// </summary>
            [MCPParameter("object_id", "ID or path of the GameObject", "string", true)]
            public string ObjectId { get; set; }
            
            /// <summary>
            /// Path to the property to modify
            /// </summary>
            [MCPParameter("property_path", "Path to the property to modify", "string", true)]
            public string PropertyPath { get; set; }
            
            /// <summary>
            /// New value for the property
            /// </summary>
            [MCPParameter("property_value", "New value for the property", "any", true)]
            public object PropertyValue { get; set; }
        }
        
        /// <summary>
        /// Output schema for modify_object command
        /// </summary>
        [MCPSchema("Output results from the modify_object command", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Whether the property was modified successfully
            /// </summary>
            [MCPParameter("success", "Whether the property was modified successfully", "boolean", true)]
            public bool Success { get; set; }
            
            /// <summary>
            /// Full path to the modified GameObject
            /// </summary>
            [MCPParameter("objectPath", "Full path to the modified GameObject", "string", true)]
            public string ObjectPath { get; set; }
            
            /// <summary>
            /// Full path to the modified property
            /// </summary>
            [MCPParameter("propertyPath", "Full path to the modified property", "string", true)]
            public string PropertyPath { get; set; }
            
            /// <summary>
            /// Previous value of the property
            /// </summary>
            [MCPParameter("oldValue", "Previous value of the property", "any", false)]
            public object OldValue { get; set; }
            
            /// <summary>
            /// New value of the property
            /// </summary>
            [MCPParameter("newValue", "New value of the property", "any", true)]
            public object NewValue { get; set; }
        }
    }
    
    /// <summary>
    /// Get logs MCP tool
    /// </summary>
    [MCPTool("get_logs", "Get logs from Unity", "get_logs(max_logs=50)")]
    public class GetLogsTool
    {
        /// <summary>
        /// Input schema for get_logs command
        /// </summary>
        [MCPSchema("Input parameters for the get_logs command")]
        public class InputModel
        {
            /// <summary>
            /// Maximum number of logs to retrieve
            /// </summary>
            [MCPParameter("max_logs", "Maximum number of logs to retrieve", "number", false)]
            public int MaxLogs { get; set; }
        }
        
        /// <summary>
        /// Output schema for get_logs command
        /// </summary>
        [MCPSchema("Output results from the get_logs command", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Array of log entries
            /// </summary>
            [MCPParameter("logs", "Array of log entries", "array", true)]
            public List<object> Logs { get; set; }
            
            /// <summary>
            /// Total number of logs returned
            /// </summary>
            [MCPParameter("count", "Total number of logs returned", "number", true)]
            public int Count { get; set; }
        }
    }
    
    /// <summary>
    /// Get Unity info MCP tool
    /// </summary>
    [MCPTool("get_unity_info", "Get information about the Unity environment", "get_unity_info()")]
    public class GetUnityInfoTool
    {
        /// <summary>
        /// Input schema for get_unity_info command
        /// </summary>
        [MCPSchema("Input parameters for the get_unity_info command")]
        public class InputModel
        {
            // No parameters
        }
        
        /// <summary>
        /// Output schema for get_unity_info command
        /// </summary>
        [MCPSchema("Output results from the get_unity_info command", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Version of Unity Editor
            /// </summary>
            [MCPParameter("unityVersion", "Version of Unity Editor", "string", true)]
            public string UnityVersion { get; set; }
            
            /// <summary>
            /// Platform the Unity Editor is running on
            /// </summary>
            [MCPParameter("platform", "Platform the Unity Editor is running on", "string", true)]
            public string Platform { get; set; }
            
            /// <summary>
            /// Whether the Unity Editor is in play mode
            /// </summary>
            [MCPParameter("isPlaying", "Whether the Unity Editor is in play mode", "boolean", true)]
            public bool IsPlaying { get; set; }
            
            /// <summary>
            /// Number of active scenes
            /// </summary>
            [MCPParameter("activeScenesCount", "Number of active scenes", "number", true)]
            public int ActiveScenesCount { get; set; }
            
            /// <summary>
            /// List of active scene names
            /// </summary>
            [MCPParameter("activeScenes", "List of active scene names", "array", true)]
            public List<string> ActiveScenes { get; set; }
        }
    }
    
    /// <summary>
    /// Get schema MCP tool
    /// </summary>
    [MCPTool("get_schema", "Get information about available tools and resources", "get_schema()")]
    public class GetSchemaTool
    {
        /// <summary>
        /// Input schema for get_schema command
        /// </summary>
        [MCPSchema("Input parameters for the get_schema command")]
        public class InputModel
        {
            // No parameters
        }
        
        /// <summary>
        /// Output schema for get_schema command
        /// </summary>
        [MCPSchema("Output results from the get_schema command", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Array of available tool descriptors
            /// </summary>
            [MCPParameter("tools", "Array of available tool descriptors", "array", true)]
            public List<ToolDescriptor> Tools { get; set; }
            
            /// <summary>
            /// Array of available resource descriptors
            /// </summary>
            [MCPParameter("resources", "Array of available resource descriptors", "array", true)]
            public List<ResourceDescriptor> Resources { get; set; }
        }
    }
    
    /// <summary>
    /// Unity info MCP resource
    /// </summary>
    [MCPResource("unity_info", "Get information about the Unity environment", "unity://info", "unity://info")]
    public class UnityInfoResource
    {
        /// <summary>
        /// Output schema for unity://info resource
        /// </summary>
        [MCPSchema("Output results from the unity://info resource", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Version of Unity Editor
            /// </summary>
            [MCPParameter("unityVersion", "Version of Unity Editor", "string", true)]
            public string UnityVersion { get; set; }
            
            /// <summary>
            /// Platform the Unity Editor is running on
            /// </summary>
            [MCPParameter("platform", "Platform the Unity Editor is running on", "string", true)]
            public string Platform { get; set; }
            
            /// <summary>
            /// Whether the Unity Editor is in play mode
            /// </summary>
            [MCPParameter("isPlaying", "Whether the Unity Editor is in play mode", "boolean", true)]
            public bool IsPlaying { get; set; }
            
            /// <summary>
            /// List of active scene names
            /// </summary>
            [MCPParameter("activeScenes", "List of active scene names", "array", true)]
            public List<string> ActiveScenes { get; set; }
        }
    }
    
    /// <summary>
    /// Unity logs MCP resource
    /// </summary>
    [MCPResource("unity_logs", "Get logs from Unity", "unity://logs", "unity://logs?max_logs=50")]
    public class UnityLogsResource
    {
        /// <summary>
        /// Output schema for unity://logs resource
        /// </summary>
        [MCPSchema("Output results from the unity://logs resource", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Array of log entries
            /// </summary>
            [MCPParameter("logs", "Array of log entries", "array", true)]
            public List<object> Logs { get; set; }
            
            /// <summary>
            /// Total number of logs returned
            /// </summary>
            [MCPParameter("count", "Total number of logs returned", "number", true)]
            public int Count { get; set; }
        }
    }
    
    /// <summary>
    /// Unity scene MCP resource
    /// </summary>
    [MCPResource("unity_scene", "Get information about a Unity scene", "unity://scene/{scene_name}", "unity://scene/SampleScene")]
    public class UnitySceneResource
    {
        /// <summary>
        /// Output schema for unity://scene resource
        /// </summary>
        [MCPSchema("Output results from the unity://scene resource", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Scene name
            /// </summary>
            [MCPParameter("name", "Scene name", "string", true)]
            public string Name { get; set; }
            
            /// <summary>
            /// Scene asset path
            /// </summary>
            [MCPParameter("path", "Scene asset path", "string", true)]
            public string Path { get; set; }
            
            /// <summary>
            /// Whether the scene is currently loaded
            /// </summary>
            [MCPParameter("isLoaded", "Whether the scene is currently loaded", "boolean", true)]
            public bool IsLoaded { get; set; }
            
            /// <summary>
            /// Root GameObjects in the scene
            /// </summary>
            [MCPParameter("rootObjects", "Root GameObjects in the scene", "array", true)]
            public List<string> RootObjects { get; set; }
            
            /// <summary>
            /// Hierarchical representation of the scene objects
            /// </summary>
            [MCPParameter("hierarchy", "Hierarchical representation of the scene objects", "object", true)]
            public object Hierarchy { get; set; }
        }
    }
    
    /// <summary>
    /// Unity object MCP resource
    /// </summary>
    [MCPResource("unity_object", "Get information about a Unity GameObject", "unity://object/{object_id}", "unity://object/Main Camera")]
    public class UnityObjectResource
    {
        /// <summary>
        /// Output schema for unity://object resource
        /// </summary>
        [MCPSchema("Output results from the unity://object resource", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Name of the GameObject
            /// </summary>
            [MCPParameter("name", "Name of the GameObject", "string", true)]
            public string Name { get; set; }
            
            /// <summary>
            /// Full path to the GameObject in hierarchy
            /// </summary>
            [MCPParameter("path", "Full path to the GameObject in hierarchy", "string", true)]
            public string Path { get; set; }
            
            /// <summary>
            /// Whether the GameObject is active
            /// </summary>
            [MCPParameter("active", "Whether the GameObject is active", "boolean", true)]
            public bool Active { get; set; }
            
            /// <summary>
            /// List of components attached to the GameObject
            /// </summary>
            [MCPParameter("components", "List of components attached to the GameObject", "array", true)]
            public List<string> Components { get; set; }
            
            /// <summary>
            /// List of child GameObjects
            /// </summary>
            [MCPParameter("children", "List of child GameObjects", "array", true)]
            public List<string> Children { get; set; }
            
            /// <summary>
            /// Transform component information
            /// </summary>
            [MCPParameter("transform", "Transform component information", "object", true)]
            public object Transform { get; set; }
        }
    }
    
    /// <summary>
    /// Unity schema MCP resource
    /// </summary>
    [MCPResource("unity_schema", "Get information about available tools and resources", "unity://schema", "unity://schema")]
    public class UnitySchemaResource
    {
        /// <summary>
        /// Output schema for unity://schema resource
        /// </summary>
        [MCPSchema("Output results from the unity://schema resource", "output")]
        public class OutputModel
        {
            /// <summary>
            /// Array of available tool descriptors
            /// </summary>
            [MCPParameter("tools", "Array of available tool descriptors", "array", true)]
            public List<ToolDescriptor> Tools { get; set; }
            
            /// <summary>
            /// Array of available resource descriptors
            /// </summary>
            [MCPParameter("resources", "Array of available resource descriptors", "array", true)]
            public List<ResourceDescriptor> Resources { get; set; }
        }
    }
}