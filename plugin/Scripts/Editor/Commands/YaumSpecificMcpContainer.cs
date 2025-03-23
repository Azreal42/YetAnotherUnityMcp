
using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Containers
{
    /// <summary>
    /// MCP Container for scene-related functionality
    /// </summary>
    [MCPContainer("global", "Scene-related tools and resources")]
    public static class YaumSpecificMcpContainer
    {
    /// <summary>
    /// Get information about the Unity environment
    /// </summary>
    /// <returns>JSON string with Unity information</returns>
        //[MCPResource("unity_info", "Get information about the Unity environment", "unity://info", "unity://info")]
        [MCPTool("unity_info", "Get information about the Unity environment")]
        public static string GetUnityInfo()
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
        
        /// <summary>
        /// Get logs from the Unity Editor console
        /// </summary>
        /// <param name="maxLogs">Maximum number of logs to return</param>
        /// <returns>JSON string with logs</returns>
        [MCPTool("unity_logs", "Get logs from the Unity Editor console")]
        public static string GetLogs(
            [MCPParameter("max_logs", "Maximum number of logs to retrieve", "number", false)] int maxLogs = 100)
        {
            try
            {
                var logs = GetConsoleLogs(maxLogs);
                
                StringBuilder result = new StringBuilder();
                result.AppendLine("{");
                result.AppendLine("  \"logs\": [");
                
                for (int i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    result.AppendLine("    {");
                    result.AppendLine($"      \"type\": \"{log.Type}\",");
                    result.AppendLine($"      \"message\": \"{EscapeJsonString(log.Message)}\",");
                    result.AppendLine($"      \"stackTrace\": \"{EscapeJsonString(log.StackTrace)}\",");
                    result.AppendLine($"      \"timestamp\": \"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"");
                    
                    // If it's the last log, don't add a comma
                    if (i == logs.Count - 1)
                    {
                        result.AppendLine("    }");
                    }
                    else
                    {
                        result.AppendLine("    },");
                    }
                }
                
                result.AppendLine("  ]");
                result.AppendLine("}");
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting logs: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }

        
        #region Helper Classes and Methods
        
        /// <summary>
        /// Log entry class for console logs
        /// </summary>
        private class LogEntry
        {
            public string Type { get; set; }
            public string Message { get; set; }
            public string StackTrace { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        /// <summary>
        /// Get console logs from Unity Editor
        /// </summary>
        /// <param name="maxLogs">Maximum number of logs to retrieve</param>
        /// <returns>List of log entries</returns>
        private static System.Collections.Generic.List<LogEntry> GetConsoleLogs(int maxLogs)
        {
            var logs = new System.Collections.Generic.List<LogEntry>();
            
            // Use reflection to access the internal Unity console log entries
            // since Unity doesn't expose this information via public API
            try
            {
                // Get the console window type
                var consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
                if (consoleWindowType == null)
                {
                    logs.Add(new LogEntry
                    {
                        Type = "Error",
                        Message = "Could not find ConsoleWindow type",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    return logs;
                }
                
                // Get the console window instance
                var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
                if (fieldInfo == null)
                {
                    logs.Add(new LogEntry
                    {
                        Type = "Error",
                        Message = "Could not find ms_ConsoleWindow field",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    return logs;
                }
                
                var consoleWindow = fieldInfo.GetValue(null);
                if (consoleWindow == null)
                {
                    // Try to get by opening the console window if it's not open
                    consoleWindow = EditorWindow.GetWindow(consoleWindowType);
                    if (consoleWindow == null)
                    {
                        logs.Add(new LogEntry
                        {
                            Type = "Error",
                            Message = "Could not get ConsoleWindow instance",
                            StackTrace = "",
                            Timestamp = DateTime.Now
                        });
                        return logs;
                    }
                }
                
                // Get the log entries
                var logEntriesField = consoleWindowType.GetField("m_LogEntries", BindingFlags.Instance | BindingFlags.NonPublic);
                if (logEntriesField == null)
                {
                    logs.Add(new LogEntry
                    {
                        Type = "Error",
                        Message = "Could not find m_LogEntries field",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    return logs;
                }
                
                var logEntries = logEntriesField.GetValue(consoleWindow);
                if (logEntries == null)
                {
                    logs.Add(new LogEntry
                    {
                        Type = "Error",
                        Message = "Could not get log entries",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    return logs;
                }
                
                // Get count property
                var logEntriesType = logEntries.GetType();
                var countProperty = logEntriesType.GetProperty("Count");
                if (countProperty == null)
                {
                    logs.Add(new LogEntry
                    {
                        Type = "Error",
                        Message = "Could not find Count property",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    return logs;
                }
                
                int count = Math.Min(maxLogs, (int)countProperty.GetValue(logEntries, null));
                
                // Get StartGettingEntries and EndGettingEntries methods
                var startMethod = logEntriesType.GetMethod("StartGettingEntries");
                var endMethod = logEntriesType.GetMethod("EndGettingEntries");
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Instance | BindingFlags.Public);
                
                if (startMethod == null || endMethod == null || getEntryMethod == null)
                {
                    logs.Add(new LogEntry
                    {
                        Type = "Error",
                        Message = "Could not find log entry methods",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    return logs;
                }
                
                // Start getting entries
                startMethod.Invoke(logEntries, null);
                
                // Log entry parameters
                object[] index = new object[1];
                int mode = 1; // Mode 1 is default for getting entries
                
                // Get entries
                for (int i = 0; i < count; i++)
                {
                    index[0] = i;
                    object entry = getEntryMethod.Invoke(logEntries, new object[] { i, mode });
                    
                    if (entry != null)
                    {
                        Type entryType = entry.GetType();
                        FieldInfo messageField = entryType.GetField("message");
                        FieldInfo stackTraceField = entryType.GetField("stackTrace");
                        FieldInfo typeField = entryType.GetField("type");
                        
                        if (messageField != null && stackTraceField != null && typeField != null)
                        {
                            string message = messageField.GetValue(entry) as string ?? "No message";
                            string stackTrace = stackTraceField.GetValue(entry) as string ?? "";
                            int type = (int)typeField.GetValue(entry);
                            
                            string logType;
                            switch (type)
                            {
                                case 0:
                                    logType = "Log";
                                    break;
                                case 1:
                                    logType = "Warning";
                                    break;
                                case 2:
                                    logType = "Error";
                                    break;
                                default:
                                    logType = "Unknown";
                                    break;
                            }
                            
                            logs.Add(new LogEntry
                            {
                                Type = logType,
                                Message = message,
                                StackTrace = stackTrace,
                                Timestamp = DateTime.Now.AddSeconds(-i) // Approximate timestamp
                            });
                        }
                    }
                }
                
                // End getting entries
                endMethod.Invoke(logEntries, null);
            }
            catch (Exception ex)
            {
                logs.Add(new LogEntry
                {
                    Type = "Error",
                    Message = $"Error accessing console logs: {ex.Message}",
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.Now
                });
            }
            
            // If reflection approach failed, just add a placeholder
            if (logs.Count == 0)
            {
                logs.Add(new LogEntry
                {
                    Type = "Info",
                    Message = "No logs found or unable to access console logs",
                    StackTrace = "",
                    Timestamp = DateTime.Now
                });
            }
            
            return logs;
        }
        
        /// <summary>
        /// Escape JSON string
        /// </summary>
        /// <param name="str">String to escape</param>
        /// <returns>Escaped string</returns>
        private static string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";
            
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
        
        #endregion
    }
}