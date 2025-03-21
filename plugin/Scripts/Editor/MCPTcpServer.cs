using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using YetAnotherUnityMcp.Editor.Commands;
using YetAnotherUnityMcp.Editor.Models;
using YetAnotherUnityMcp.Editor.Net;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Server for handling TCP MCP connections from clients
    /// </summary>
    public class MCPTcpServer
    {
        private static MCPTcpServer _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static MCPTcpServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MCPTcpServer();
                }
                return _instance;
            }
        }
        
        private TcpServer _server;
        private Dictionary<string, string> _activeClients;
        
        /// <summary>
        /// Is the server running
        /// </summary>
        public bool IsRunning => _server?.IsRunning ?? false;
        
        /// <summary>
        /// Server URL
        /// </summary>
        public string ServerUrl => _server?.ServerUrl ?? string.Empty;
        
        /// <summary>
        /// Number of connected clients
        /// </summary>
        public int ClientCount => _activeClients?.Count ?? 0;
        
        /// <summary>
        /// Event fired when the server starts
        /// </summary>
        public event Action OnServerStarted;
        
        /// <summary>
        /// Event fired when the server stops
        /// </summary>
        public event Action OnServerStopped;
        
        /// <summary>
        /// Event fired when a client connects
        /// </summary>
        public event Action<string> OnClientConnected;
        
        /// <summary>
        /// Event fired when a client disconnects
        /// </summary>
        public event Action<string> OnClientDisconnected;
        
        /// <summary>
        /// Event fired when a message is received from a client
        /// </summary>
        public event Action<string, string> OnMessageReceived;
        
        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        public event Action<string> OnError;

        private MCPTcpServer()
        {
            _server = new TcpServer();
            _activeClients = new Dictionary<string, string>();
            
            // Register event handlers
            _server.OnStarted += HandleServerStarted;
            _server.OnStopped += HandleServerStopped;
            _server.OnClientConnected += HandleClientConnected;
            _server.OnClientDisconnected += HandleClientDisconnected;
            _server.OnMessageReceived += HandleMessageReceived;
            _server.OnError += HandleError;
        }
        
        /// <summary>
        /// Start the MCP TCP server
        /// </summary>
        /// <param name="host">Hostname to bind to (default: localhost)</param>
        /// <param name="port">Port to listen on (default: 8080)</param>
        /// <returns>True if started successfully, false otherwise</returns>
        public async Task<bool> StartAsync(string host = "localhost", int port = 8080)
        {
            return await _server.StartAsync(host, port);
        }
        
        /// <summary>
        /// Stop the MCP TCP server
        /// </summary>
        public async Task StopAsync()
        {
            await _server.StopAsync();
            _activeClients.Clear();
        }
        
        /// <summary>
        /// Broadcast a message to all connected clients
        /// </summary>
        /// <param name="message">Message to broadcast</param>
        public async Task BroadcastMessageAsync(object message)
        {
            string json = JsonConvert.SerializeObject(message);
            await _server.BroadcastMessageAsync(json);
        }
        
        /// <summary>
        /// Send a message to a specific client
        /// </summary>
        /// <param name="clientId">ID of the client to send to</param>
        /// <param name="message">Message to send</param>
        public async Task SendMessageAsync(string clientId, object message)
        {
            string json = JsonConvert.SerializeObject(message);
            await _server.SendMessageAsync(clientId, json);
        }
        
        /// <summary>
        /// Handle server started event
        /// </summary>
        private void HandleServerStarted()
        {
            Debug.Log($"[MCP TCP Server] Started on {_server.ServerUrl}");
            OnServerStarted?.Invoke();
        }
        
        /// <summary>
        /// Handle server stopped event
        /// </summary>
        private void HandleServerStopped()
        {
            Debug.Log("[MCP TCP Server] Stopped");
            OnServerStopped?.Invoke();
        }
        
        /// <summary>
        /// Handle client connected event
        /// </summary>
        private void HandleClientConnected(TcpConnection connection)
        {
            string clientId = connection.Id;
            string clientInfo = $"{connection.RemoteEndPoint.Address}:{connection.RemoteEndPoint.Port}";
            
            _activeClients[clientId] = clientInfo;
            
            Debug.Log($"[MCP TCP Server] Client connected: {clientId} ({clientInfo})");
            OnClientConnected?.Invoke(clientId);
        }
        
        /// <summary>
        /// Handle client disconnected event
        /// </summary>
        private void HandleClientDisconnected(TcpConnection connection)
        {
            string clientId = connection.Id;
            
            if (_activeClients.ContainsKey(clientId))
            {
                string clientInfo = _activeClients[clientId];
                _activeClients.Remove(clientId);
                
                Debug.Log($"[MCP TCP Server] Client disconnected: {clientId} ({clientInfo})");
                OnClientDisconnected?.Invoke(clientId);
            }
        }
        
        /// <summary>
        /// Handle message received event
        /// </summary>
        private void HandleMessageReceived(string message, TcpConnection connection)
        {
            string clientId = connection.Id;
            
            try
            {
                // We'll let client message logging be handled by the TcpJsonMessage.Process
                // Parse the message as JSON
                var request = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                
                // Check for required fields
                if (!(request.TryGetValue("id", out object idObj) && idObj is string requestId))
                {
                    Debug.LogError($"[MCP TCP Server] Invalid request from client {clientId}: Missing ID");
                    SendErrorResponse(clientId, "error_id", "Invalid request: Missing ID");
                    return;
                }
                
                // Process the request based on its type
                if (request.TryGetValue("command", out object commandObj) && commandObj is string command)
                {
                    // This is a command request
                    ProcessCommandRequest(clientId, requestId.ToString(), command, request);
                }
                else if (request.TryGetValue("type", out object typeObj) && 
                         typeObj.ToString() == "response")
                {
                    // This is a response to a request we sent
                    // Currently, the server doesn't initiate requests, so we'll just log this
                    Debug.Log($"[MCP TCP Server] Received response to request {requestId} from client {clientId}");
                }
                else
                {
                    // Unknown request type
                    Debug.LogError($"[MCP TCP Server] Unknown request type from client {clientId}");
                    SendErrorResponse(clientId, requestId.ToString(), "Unknown request type");
                }
                
                // Forward message to subscribers
                OnMessageReceived?.Invoke(clientId, message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP TCP Server] Error handling message from client {clientId}: {ex.Message}");
                SendErrorResponse(clientId, "error", $"Error handling message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle error event
        /// </summary>
        private void HandleError(string error)
        {
            Debug.LogError($"[MCP TCP Server] Error: {error}");
            OnError?.Invoke(error);
        }
        
        /// <summary>
        /// Process a command request from a client
        /// </summary>
        private async void ProcessCommandRequest(string clientId, string requestId, string command, Dictionary<string, object> request)
        {
            // Extract parameters
            Dictionary<string, object> parameters = null;
            if (request.TryGetValue("parameters", out object paramsObj) && 
                paramsObj is Dictionary<string, object> paramsDict)
            {
                parameters = paramsDict;
            }
            
            // Execute the command
            object result = null;
            string error = null;
            
            try
            {
                // Use the performance monitor to track execution time
                using (var timer = CommandExecutionMonitor.Instance.StartOperation($"Command_{command}"))
                {
                    // Track the start time for our own logging
                    long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    Debug.Log($"[MCP Server] Executing command: {command}");
                    
                    // Process different commands
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
                            string outputPath = parameters?.TryGetValue("output_path", out object pathObj) == true ? pathObj.ToString() : "screenshot.png";
                            int width = parameters?.TryGetValue("width", out object widthObj) == true ? Convert.ToInt32(widthObj) : 1920;
                            int height = parameters?.TryGetValue("height", out object heightObj) == true ? Convert.ToInt32(heightObj) : 1080;
                            
                            result = TakeScreenshotCommand.Execute(outputPath, width, height);
                            break;
                            
                        case "modify_object":
                            if (parameters?.TryGetValue("object_id", out object objIdObj) == true && objIdObj is string objectId &&
                                parameters?.TryGetValue("property_path", out object propPathObj) == true && propPathObj is string propertyPath)
                            {
                                object propertyValue = parameters?.TryGetValue("property_value", out object propValueObj) == true ? propValueObj : null;
                                result = ModifyObjectCommand.Execute(objectId, propertyPath, propertyValue);
                            }
                            else
                            {
                                error = "Missing or invalid parameters for modify_object";
                            }
                            break;
                            
                        case "get_logs":
                            int maxLogs = parameters?.TryGetValue("max_logs", out object maxLogsObj) == true ? Convert.ToInt32(maxLogsObj) : 100;
                            result = GetLogsCommand.Execute(maxLogs);
                            break;
                            
                        case "get_unity_info":
                            result = GetUnityInfoCommand.Execute();
                            break;
                            
                        case "get_schema":
                            result = GetSchemaCommand.Execute();
                            break;
                            
                        case "access_resource":
                            // Invoke a resource using the ResourceInvoker
                            if (parameters?.TryGetValue("resource_name", out object resourceNameObj) == true && 
                                resourceNameObj is string resourceName)
                            {
                                // Get the resource parameters
                                Dictionary<string, object> resourceParams = parameters?.TryGetValue("parameters", out object resourceParamsObj) == true &&
                                                                           resourceParamsObj is Dictionary<string, object> paramsDict
                                                                           ? paramsDict
                                                                           : new Dictionary<string, object>();
                                
                                try
                                {
                                    // Use the ResourceInvoker to invoke the resource
                                    result = ResourceInvoker.InvokeResource(resourceName, resourceParams);
                                    Debug.Log($"[MCP TCP Server] Resource {resourceName} accessed successfully");
                                }
                                catch (Exception ex)
                                {
                                    error = ex.Message;
                                    Debug.LogError($"[MCP TCP Server] Error accessing resource {resourceName}: {ex.Message}\n{ex.StackTrace}");
                                }
                            }
                            else
                            {
                                error = "Missing or invalid 'resource_name' parameter";
                            }
                            break;
                            
                        default:
                            error = $"Unknown command: {command}";
                            break;
                    }
                    
                    // Log completion time for long-running commands
                    long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long elapsed = endTime - startTime;
                    
                    if (elapsed > 100)
                    {
                        Debug.Log($"[MCP Server] Completed command: {command} in {elapsed}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                error = $"Error executing command {command}: {ex.Message}";
                Debug.LogError($"[MCP TCP Server] {error}");
            }
            
            // Send the response
            try
            {
                // Get the client timestamp if present
                long clientTimestamp = 0;
                if (request.TryGetValue("client_timestamp", out object timestampObj))
                {
                    clientTimestamp = Convert.ToInt64(timestampObj);
                }
                
                var response = new Dictionary<string, object>
                {
                    { "id", requestId },
                    { "type", "response" },
                    { "status", error == null ? "success" : "error" },
                    { "result", result },
                    { "error", error },
                    { "server_timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                    { "client_timestamp", clientTimestamp } // Echo back the client timestamp
                };
                
                await SendMessageAsync(clientId, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP TCP Server] Error sending response to client {clientId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send an error response to a client
        /// </summary>
        private async void SendErrorResponse(string clientId, string requestId, string errorMessage)
        {
            try
            {
                var response = new Dictionary<string, object>
                {
                    { "id", requestId },
                    { "type", "response" },
                    { "status", "error" },
                    { "result", null },
                    { "error", errorMessage },
                    { "server_timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };
                
                await SendMessageAsync(clientId, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP TCP Server] Error sending error response to client {clientId}: {ex.Message}");
            }
        }
    }
}