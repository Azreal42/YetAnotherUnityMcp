using System;
using System.Text;
using UnityEngine;
using UnityEditor;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Command to get information about the Unity environment
    /// </summary>
    [MCPResource("unity_info", "Get information about the Unity environment", "unity://info", "unity://info")]
    public static class GetUnityInfoCommand
    {
        /// <summary>
        /// Gets information about the Unity Editor environment - Tool method
        /// </summary>
        /// <returns>JSON string with Unity information</returns>
        public static string Execute()
        {
            return GetUnityInfoImpl();
        }
        
        /// <summary>
        /// Gets information about the Unity Editor environment - Resource method
        /// </summary>
        /// <returns>JSON string with Unity information</returns>
        public static string GetResource()
        {
            return GetUnityInfoImpl();
        }
        
        /// <summary>
        /// Implementation of Unity info retrieval shared by both Execute and GetResource methods
        /// </summary>
        private static string GetUnityInfoImpl()
        {
            try
            {
                StringBuilder info = new StringBuilder();
                info.AppendLine("{");
                
                // Unity version info
                info.AppendLine($"  \"unityVersion\": \"{Application.unityVersion}\",");
                info.AppendLine($"  \"platform\": \"{Application.platform}\",");
                info.AppendLine($"  \"isEditor\": {Application.isEditor.ToString().ToLower()},");
                info.AppendLine($"  \"companyName\": \"{Application.companyName}\",");
                info.AppendLine($"  \"productName\": \"{Application.productName}\",");
                
                // Scene info
                info.AppendLine("  \"scenes\": {");
                int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
                for (int i = 0; i < sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    info.AppendLine($"    \"{scene.name}\": {{");
                    info.AppendLine($"      \"path\": \"{scene.path}\",");
                    info.AppendLine($"      \"buildIndex\": {scene.buildIndex},");
                    info.AppendLine($"      \"isLoaded\": {scene.isLoaded.ToString().ToLower()},");
                    info.AppendLine($"      \"isDirty\": {scene.isDirty.ToString().ToLower()}");
                    
                    // If it's the last scene, don't add a comma
                    if (i == sceneCount - 1)
                    {
                        info.AppendLine("    }");
                    }
                    else
                    {
                        info.AppendLine("    },");
                    }
                }
                info.AppendLine("  },");
                
                // Project settings
                info.AppendLine("  \"projectSettings\": {");
                info.AppendLine($"    \"productGUID\": \"{PlayerSettings.productGUID}\",");
                info.AppendLine($"    \"apiCompatibilityLevel\": \"{PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup)}\",");
                info.AppendLine($"    \"scriptingBackend\": \"{PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup)}\",");
                info.AppendLine($"    \"currentBuildTarget\": \"{EditorUserBuildSettings.activeBuildTarget}\"");
                info.AppendLine("  },");
                
                // Build settings
                info.AppendLine("  \"buildSettings\": {");
                info.AppendLine($"    \"developmentBuild\": {EditorUserBuildSettings.development.ToString().ToLower()},");
                info.AppendLine($"    \"buildAppBundle\": {EditorUserBuildSettings.buildAppBundle.ToString().ToLower()},");
                info.AppendLine($"    \"selectedBuildTargetGroup\": \"{EditorUserBuildSettings.selectedBuildTargetGroup}\"");
                info.AppendLine("  }");
                
                info.AppendLine("}");
                
                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting Unity info: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }
    }
}