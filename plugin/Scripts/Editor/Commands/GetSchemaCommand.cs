using System;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Command to retrieve information about available tools and resources
    /// </summary>
    public static class GetSchemaCommand
    {
        /// <summary>
        /// Get information about all registered tools and resources
        /// </summary>
        /// <returns>JSON string describing all tools and resources</returns>
        public static string Execute()
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