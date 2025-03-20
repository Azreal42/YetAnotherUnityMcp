using System;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Command to retrieve information about available tools and resources
    /// </summary>
    [MCPTool("get_schema", "Get information about available tools and resources", "get_schema()")]
    [MCPResource("unity_schema", "Get information about available tools and resources", "unity://schema", "unity://schema")]
    public static class GetSchemaCommand
    {
        /// <summary>
        /// Get information about all registered tools and resources - Tool method
        /// </summary>
        /// <returns>JSON string describing all tools and resources</returns>
        public static string Execute()
        {
            return GetSchemaImpl();
        }
        
        /// <summary>
        /// Get information about all registered tools and resources - Resource method
        /// </summary>
        /// <returns>JSON string describing all tools and resources</returns>
        public static string GetResource()
        {
            return GetSchemaImpl();
        }
        
        /// <summary>
        /// Implementation of schema retrieval shared by both Execute and GetResource methods
        /// </summary>
        private static string GetSchemaImpl()
        {
            try
            {
                // Get schema as JSON
                string result = MCPRegistry.Instance.GetSchemaAsJson();
                
                // Log success
                Debug.Log("[GetSchemaCommand] Schema retrieved successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                // Log error
                Debug.LogError($"[GetSchemaCommand] Error retrieving schema: {ex.Message}");
                
                // Return error result
                return $"{{\"error\": \"Error retrieving schema: {ex.Message}\"}}";
            }
        }
    }
}