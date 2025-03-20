using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Newtonsoft.Json;
using YetAnotherUnityMcp.Editor.Commands;

namespace YetAnotherUnityMcp.Editor.WebSocket
{
    /// <summary>
    /// Manager for WebSocket communication with the MCP server
    /// </summary>
    public class MCPWebSocketManager
    {
        private static MCPWebSocketManager _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static MCPWebSocketManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MCPWebSocketManager();
                }
                return _instance;
            }
        }
        
        private WebSocketClient _client;
        private Dictionary<string, TaskCompletionSource<string>> _pendingRequests;
        private int _requestId = 0;
        
        /// <summary>
        /// Is the client connected to the server
        /// </summary>
        public bool IsConnected => _client?.IsConnected ?? false;
        
        /// <summary>
        /// Event fired when the client connects to the server
        /// </summary>
        public event Action OnConnected;
        
        /// <summary>
        /// Event fired when the client disconnects from the server
        /// </summary>
        public event Action OnDisconnected;
        
        /// <summary>
        /// Event fired when a message is received from the server
        /// </summary>
        public event Action<string> OnMessageReceived;
        
        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        public event Action<string> OnError;

        private MCPWebSocketManager()
        {
            _client = new WebSocketClient();
            _pendingRequests = new Dictionary<string, TaskCompletionSource<string>>();
            
            // Register event handlers
            _client.OnConnected += HandleConnected;
            _client.OnDisconnected += HandleDisconnected;
            _client.OnMessageReceived += HandleMessageReceived;
            _client.OnError += HandleError;
        }

        /// <summary>
        /// Connect to the MCP server
        /// </summary>
        /// <param name="serverUrl">WebSocket server URL (ws:// or wss://)</param>
        /// <returns>True if connected successfully, false otherwise</returns>
        public async Task<bool> ConnectAsync(string serverUrl)
        {
            // Ensure the URL is a WebSocket URL
            if (!serverUrl.StartsWith("ws://") && !serverUrl.StartsWith("wss://"))
            {
                Uri uri = new Uri(serverUrl);
                string scheme = uri.Scheme == "https" ? "wss" : "ws";
                serverUrl = $"{scheme}://{uri.Host}:{uri.Port}/ws";
            }
            
            return await _client.ConnectAsync(serverUrl);
        }

        /// <summary>
        /// Disconnect from the MCP server
        /// </summary>
        public async Task DisconnectAsync()
        {
            await _client.DisconnectAsync();
            
            // Clear any pending requests
            foreach (var request in _pendingRequests)
            {
                request.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();
        }

        /// <summary>
        /// Execute code on the MCP server
        /// </summary>
        /// <param name="code">Code to execute</param>
        /// <returns>Result of the execution</returns>
        public async Task<string> ExecuteCodeAsync(string code)
        {
            return await SendCommandAsync("execute_code", new Dictionary<string, object>
            {
                { "code", code }
            });
        }
        
        /// <summary>
        /// Take a screenshot
        /// </summary>
        /// <param name="outputPath">Path to save the screenshot</param>
        /// <param name="width">Width of the screenshot</param>
        /// <param name="height">Height of the screenshot</param>
        /// <returns>Result of the screenshot operation</returns>
        public async Task<string> TakeScreenshotAsync(string outputPath, int width = 1920, int height = 1080)
        {
            return await SendCommandAsync("take_screenshot", new Dictionary<string, object>
            {
                { "output_path", outputPath },
                { "width", width },
                { "height", height }
            });
        }
        
        /// <summary>
        /// Modify an object property
        /// </summary>
        /// <param name="objectId">ID of the object to modify</param>
        /// <param name="propertyPath">Path to the property</param>
        /// <param name="propertyValue">New value for the property</param>
        /// <returns>Result of the modification</returns>
        public async Task<string> ModifyObjectAsync(string objectId, string propertyPath, object propertyValue)
        {
            return await SendCommandAsync("modify_object", new Dictionary<string, object>
            {
                { "object_id", objectId },
                { "property_path", propertyPath },
                { "property_value", propertyValue }
            });
        }
        
        /// <summary>
        /// Get logs from the server
        /// </summary>
        /// <returns>Logs from the server</returns>
        public async Task<string> GetLogsAsync()
        {
            return await SendCommandAsync("get_logs", null);
        }
        
        /// <summary>
        /// Get Unity info from the server
        /// </summary>
        /// <returns>Unity info from the server</returns>
        public async Task<string> GetUnityInfoAsync()
        {
            return await SendCommandAsync("get_unity_info", null);
        }
        
        /// <summary>
        /// Send a command to the server
        /// </summary>
        /// <param name="command">Command name</param>
        /// <param name="parameters">Command parameters</param>
        /// <returns>Result of the command</returns>
        public async Task<string> SendCommandAsync(string command, Dictionary<string, object> parameters)
        {
            // Use the performance monitor to track execution time
            using (var timer = WebSocketPerformanceMonitor.Instance.StartOperation($"Command_{command}"))
            {
                // Track the start time for our own logging
                long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Debug.Log($"[MCP] Starting command: {command} at {startTime}ms");
                
                if (!IsConnected)
                {
                    throw new InvalidOperationException("WebSocket is not connected");
                }
                
                // Generate a unique request ID
                string requestId = GenerateRequestId();
                
                // Create a task completion source for the response
                var tcs = new TaskCompletionSource<string>();
                _pendingRequests[requestId] = tcs;
                
                // Create the request message
                var request = new Dictionary<string, object>
                {
                    { "id", requestId },
                    { "command", command },
                    { "client_timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };
                
                if (parameters != null)
                {
                    request["parameters"] = parameters;
                }
                
                // Serialize and send the request
                string json = JsonConvert.SerializeObject(request);
                await _client.SendMessageAsync(json);
                
                // Set a timeout for the request (60 seconds)
                var timeoutTask = Task.Delay(60000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Request timed out, remove it from pending requests
                    _pendingRequests.Remove(requestId);
                    throw new TimeoutException($"Command {command} timed out");
                }
                
                // Request completed successfully, return the result
                var result = await tcs.Task;
                long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long elapsed = endTime - startTime;
                
                // Only log if it took a significant amount of time
                if (elapsed > 100)
                {
                    Debug.Log($"[MCP] Completed command: {command} in {elapsed}ms");
                }
                
                return result;
            }
        }

        private string GenerateRequestId()
        {
            return $"req_{DateTime.Now.Ticks}_{_requestId++}";
        }
        
        private void HandleConnected()
        {
            Debug.Log("[MCP WebSocket Manager] Connected to server");
            OnConnected?.Invoke();
        }
        
        private void HandleDisconnected()
        {
            Debug.Log("[MCP WebSocket Manager] Disconnected from server");
            OnDisconnected?.Invoke();
        }
        
        private void HandleMessageReceived(string message)
        {
            try
            {
                // We'll let the WebSocketJsonMessage handle the logging with its Process method
                // Try to parse the message as JSON
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                
                // Check if this is a response to a request
                if (!(response.TryGetValue("id", out object idObj) && idObj is string id))
                {
                    Debug.LogError($"[MCP WebSocket Manager] Invalid response: {message}");
                    return;
                }

                // Check if this is a direct resource request from server
                if (response.TryGetValue("type", out object typeObj) && 
                            typeObj.ToString() == "request" &&
                            response.TryGetValue("action", out object actionObj))
                {
                    Debug.LogError($"[MCP WebSocket Manager] Resource request: {actionObj}");

                    string action = actionObj.ToString();
                    Dictionary<string, object> payload = null;
                    
                    // Parse payload if it exists
                    if (response.TryGetValue("payload", out object payloadObj) && 
                        payloadObj is Dictionary<string, object> payloadDict)
                    {
                        payload = payloadDict;
                    }
                    
                    // Handle different resource types
                    object result = null;
                    string error = null;
                    
                    try
                    {
                        if (action == "unity://info")
                        {
                            // Execute the Unity info command on the main thread
                            // This approach ensures we're executing on Unity's main thread
                            UnityEditor.EditorApplication.delayCall += () => {
                                result = Commands.GetUnityInfoCommand.Execute();
                            };
                            // Since we're using delayCall, make sure we wait briefly
                            System.Threading.Thread.Sleep(50); 
                            result = Commands.GetUnityInfoCommand.Execute();
                        }
                        // Add handlers for other resource types as needed
                        else if (action.StartsWith("unity://scene/"))
                        {
                            string sceneName = action.Substring("unity://scene/".Length);
                            // Handle scene request
                            // For now, just return an error
                            error = $"Scene resource not implemented: {sceneName}";
                        }
                        else if (action.StartsWith("unity://object/"))
                        {
                            string objectId = action.Substring("unity://object/".Length);
                            // Handle object request
                            // For now, just return an error
                            error = $"Object resource not implemented: {objectId}";
                        }
                        else
                        {
                            error = $"Unsupported resource action: {action}";
                        }
                    }
                    catch (Exception ex)
                    {
                        error = $"Error handling resource request: {ex.Message}";
                        Debug.LogError($"[MCP WebSocket Manager] {error}");
                    }
                    
                    // Create and send response
                    var responseObj = new Dictionary<string, object>
                    {
                        { "id", id },
                        { "type", "response" },
                        { "status", error == null ? "success" : "error" },
                        { "result", result },
                        { "error", error }
                    };
                    
                    // Send the response
                    string responseJson = JsonConvert.SerializeObject(responseObj);
                    _client.SendMessageAsync(responseJson).ContinueWith(t => 
                    {
                        if (t.IsFaulted)
                        {
                            Debug.LogError($"[MCP WebSocket Manager] Error sending response: {t.Exception?.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP WebSocket Manager] Error handling message: {ex.Message}");
            }
            
            // Always forward the message to any subscribers
            OnMessageReceived?.Invoke(message);
        }
        
        private void HandleError(string error)
        {
            Debug.LogError($"[MCP WebSocket Manager] Error: {error}");
            OnError?.Invoke(error);
        }
    }
}