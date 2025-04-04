
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
        
        // Track the last log index that was sent
        private static int _lastLogIndexSent = -1;
        
        /// <summary>
        /// Get logs from the Unity Editor console
        /// </summary>
        /// <param name="maxLogs">Maximum number of logs to return</param>
        /// <param name="onlyNewLogs">If true, only logs that haven't been sent previously will be returned</param>
        /// <returns>JSON string with logs</returns>
        [MCPTool("unity_logs", "Get logs from the Unity Editor console")]
        public static string GetLogs(
            [MCPParameter("max_logs", "Maximum number of logs to retrieve", "number", false)] int maxLogs = 100,
            [MCPParameter("only_new_logs", "If true, only logs that haven't been sent previously will be returned", "boolean", false)] bool onlyNewLogs = false)
        {
            try
            {
                var logs = GetConsoleLogs(maxLogs, onlyNewLogs);
                
                StringBuilder result = new StringBuilder();
                result.AppendLine("{");
                result.AppendLine("  \"logs\": [");
                
                for (int i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    result.AppendLine("    {");
                    result.AppendLine($"      \"type\": \"{log.Type}\",");
                    result.AppendLine($"      \"message\": \"{EscapeJsonString(log.Message)}\",");
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
                
                result.AppendLine("  ],");
                result.AppendLine($"  \"total_log_count\": {_cachedLogs.Count},");
                result.AppendLine($"  \"returned_log_count\": {logs.Count},");
                result.AppendLine($"  \"only_new_logs\": {onlyNewLogs.ToString().ToLower()}");
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
        
        // Store logs in memory
        private static System.Collections.Generic.List<LogEntry> _cachedLogs = new System.Collections.Generic.List<LogEntry>();
        
        // Static constructor to register for logs as soon as the class is loaded
        static YaumSpecificMcpContainer()
        {
            InitializeLogMonitoring();
        }
        
        /// <summary>
        /// Initialize log monitoring system
        /// </summary>
        public static void InitializeLogMonitoring()
        {
            // Only register once
            if (_cachedLogs.Count == 0)
            {
                Debug.Log("[MCP Server] Initializing log monitoring system");
                
                // Register for log messages
                Application.logMessageReceived += OnLogMessageReceived;
                
                // Add initial log
                _cachedLogs.Add(new LogEntry
                {
                    Type = "Info",
                    Message = "Log monitoring started",
                    StackTrace = "",
                    Timestamp = DateTime.Now
                });
            }
        }
        
        /// <summary>
        /// Log callback handler for Application.logMessageReceived
        /// </summary>
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            string logType;
            switch (type)
            {
                case LogType.Log:
                    logType = "Log";
                    break;
                case LogType.Warning:
                    logType = "Warning";
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    logType = "Error";
                    break;
                default:
                    logType = "Unknown";
                    break;
            }
            
            _cachedLogs.Add(new LogEntry
            {
                Type = logType,
                Message = logString,
                StackTrace = stackTrace,
                Timestamp = DateTime.Now
            });
            
            // Limit cache size to prevent memory issues
            const int maxCacheSize = 1000;
            if (_cachedLogs.Count > maxCacheSize)
            {
                _cachedLogs.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Get console logs from Unity Editor
        /// </summary>
        /// <param name="maxLogs">Maximum number of logs to retrieve</param>
        /// <param name="onlyNewLogs">If true, only get logs that haven't been sent before</param>
        /// <returns>List of log entries</returns>
        private static System.Collections.Generic.List<LogEntry> GetConsoleLogs(int maxLogs, bool onlyNewLogs)
        {
            // Create a copy of the logs to return
            var logs = new System.Collections.Generic.List<LogEntry>();
            
            // Starting index depends on whether we want only new logs
            int startIndex;
            
            if (onlyNewLogs && _lastLogIndexSent >= 0)
            {
                // If we want only new logs, start after the last index we sent
                startIndex = Math.Max(_lastLogIndexSent + 1, 0);
                
                // If we've sent all logs, return empty list
                if (startIndex >= _cachedLogs.Count)
                {
                    // If no logs are available, add a placeholder
                    logs.Add(new LogEntry
                    {
                        Type = "Info",
                        Message = "No new logs since last request",
                        StackTrace = "",
                        Timestamp = DateTime.Now
                    });
                    
                    return logs;
                }
            }
            else
            {
                // If we want all logs (limited by maxLogs), start from the end and go back maxLogs
                int count = Math.Min(maxLogs, _cachedLogs.Count);
                startIndex = _cachedLogs.Count - count;
            }
            
            // Get the logs starting from startIndex, up to maxLogs or the end of the list
            int remaining = Math.Min(maxLogs, _cachedLogs.Count - startIndex);
            for (int i = 0; i < remaining; i++)
            {
                logs.Add(_cachedLogs[startIndex + i]);
            }
            
            // Remember the index of the last log we sent
            if (_cachedLogs.Count > 0)
            {
                _lastLogIndexSent = _cachedLogs.Count - 1;
            }
            
            // If no logs are available after all that, add a placeholder
            if (logs.Count == 0)
            {
                logs.Add(new LogEntry
                {
                    Type = "Info",
                    Message = "No logs found",
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