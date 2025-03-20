using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using YetAnotherUnityMcp.Editor.Commands;

namespace YetAnotherUnityMcp.Editor.WebSocket
{
    /// <summary>
    /// Local command executor for MCP commands
    /// Used for direct execution when WebSocket server is not running
    /// </summary>
    public class MCPLocalCommandExecutor
    {
        private static MCPLocalCommandExecutor _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static MCPLocalCommandExecutor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MCPLocalCommandExecutor();
                }
                return _instance;
            }
        }
        
        private MCPLocalCommandExecutor()
        {
        }
        
        /// <summary>
        /// Execute a command locally
        /// </summary>
        /// <param name="command">Command name</param>
        /// <param name="parameters">Command parameters</param>
        /// <returns>Result of the command execution</returns>
        public string ExecuteCommand(string command, Dictionary<string, object> parameters)
        {
            // Use the performance monitor to track execution time
            using (var timer = WebSocketPerformanceMonitor.Instance.StartOperation($"LocalCommand_{command}"))
            {
                try
                {
                    // Start timing for our logging
                    long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    Debug.Log($"[MCP] Executing local command: {command} at {startTime}ms");
                    
                    object result = null;
                    string error = null;
                    
                    // Execute the command based on type
                    try
                    {
                        switch (command)
                        {
                            case "execute_code":
                                if (parameters?.TryGetValue("code", out object codeObj) == true && codeObj is string code)
                                {
                                    result = ExecuteCodeCommand.Execute(code);
                                }
                                else
                                {
                                    error = "Missing or invalid 'code' parameter";
                                }
                                break;
                            
                            case "take_screenshot":
                                string outputPath = parameters?.TryGetValue("output_path", out object pathObj) == true
                                    ? pathObj.ToString()
                                    : "screenshot.png";
                                int width = parameters?.TryGetValue("width", out object widthObj) == true
                                    ? Convert.ToInt32(widthObj)
                                    : 1920;
                                int height = parameters?.TryGetValue("height", out object heightObj) == true
                                    ? Convert.ToInt32(heightObj)
                                    : 1080;
                                
                                result = TakeScreenshotCommand.Execute(outputPath, width, height);
                                break;
                            
                            case "modify_object":
                                if (parameters?.TryGetValue("object_id", out object objIdObj) == true && objIdObj is string objectId &&
                                    parameters?.TryGetValue("property_path", out object propPathObj) == true && propPathObj is string propertyPath)
                                {
                                    object propertyValue = parameters?.TryGetValue("property_value", out object propValueObj) == true
                                        ? propValueObj
                                        : null;
                                    result = ModifyObjectCommand.Execute(objectId, propertyPath, propertyValue);
                                }
                                else
                                {
                                    error = "Missing or invalid parameters for modify_object";
                                }
                                break;
                            
                            case "get_logs":
                                int maxLogs = parameters?.TryGetValue("max_logs", out object maxLogsObj) == true
                                    ? Convert.ToInt32(maxLogsObj)
                                    : 100;
                                result = GetLogsCommand.Execute(maxLogs);
                                break;
                            
                            case "get_unity_info":
                                result = GetUnityInfoCommand.Execute();
                                break;
                            
                            default:
                                error = $"Unknown command: {command}";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = $"Error executing command {command}: {ex.Message}";
                        Debug.LogError($"[MCP Local Executor] {error}");
                    }
                    
                    // Create the response
                    var response = new Dictionary<string, object>
                    {
                        { "status", error == null ? "success" : "error" },
                        { "result", result },
                        { "error", error }
                    };
                    
                    // End timing
                    long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long elapsed = endTime - startTime;
                    
                    // Only log if it took significant time
                    if (elapsed > 100)
                    {
                        Debug.Log($"[MCP] Completed local command: {command} in {elapsed}ms");
                    }
                    
                    // Serialize and return the response
                    return JsonConvert.SerializeObject(response);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MCP Local Executor] Unhandled error: {ex.Message}");
                    
                    // Return error response
                    var errorResponse = new Dictionary<string, object>
                    {
                        { "status", "error" },
                        { "result", null },
                        { "error", $"Unhandled error: {ex.Message}" }
                    };
                    
                    return JsonConvert.SerializeObject(errorResponse);
                }
            }
        }
    }
}