using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using YetAnotherUnityMcp.Editor.WebSocket;
using YetAnotherUnityMcp.Editor.Commands;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Handles communication with the MCP server via WebSockets
    /// </summary>
    public class MCPConnection
    {
        private static MCPConnection _instance;
        
        // Connection options
        private static string _serverUrl = "ws://localhost:8080/ws";
        private static bool _isConnected = false;
        private static bool _useLocalFallback = true;
        
        // WebSocket Client
        private static MCPWebSocketManager _wsManager;
        
        // Events
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<string> OnError;

        public static bool IsConnected => _isConnected;
        public static string ServerUrl => _serverUrl;
        public static bool UseLocalFallback => _useLocalFallback;

        public static void Initialize(string url, bool useLocalFallback = true)
        {
            _serverUrl = url;
            _useLocalFallback = useLocalFallback;
            
            // Convert HTTP URLs to WebSocket URLs
            if (_serverUrl.StartsWith("http://"))
            {
                _serverUrl = _serverUrl.Replace("http://", "ws://") + "/ws";
            }
            else if (_serverUrl.StartsWith("https://"))
            {
                _serverUrl = _serverUrl.Replace("https://", "wss://") + "/ws";
            }
            else if (!_serverUrl.StartsWith("ws://") && !_serverUrl.StartsWith("wss://"))
            {
                _serverUrl = "ws://" + _serverUrl + "/ws";
            }
            
            // Setup WebSocket manager
            _wsManager = MCPWebSocketManager.Instance;
            _wsManager.OnConnected += HandleWebSocketConnected;
            _wsManager.OnDisconnected += HandleWebSocketDisconnected;
            _wsManager.OnError += HandleWebSocketError;
        }

        public static async Task<bool> Connect()
        {
            try
            {
                // Connect via WebSocket
                return await _wsManager.ConnectAsync(_serverUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to MCP server: {ex.Message}");
                _isConnected = false;
                OnError?.Invoke($"Connection error: {ex.Message}");
                return false;
            }
        }
        
        public static async Task Disconnect()
        {
            if (_wsManager != null)
            {
                await _wsManager.DisconnectAsync();
            }
        }

        public static async Task<string> ExecuteCode(string code)
        {
            try
            {
                if (_isConnected)
                {
                    // Execute code via WebSocket
                    return await _wsManager.ExecuteCodeAsync(code);
                }
                else if (_useLocalFallback)
                {
                    // Execute code locally as fallback
                    return Commands.ExecuteCodeCommand.Execute(code);
                }
                else
                {
                    return "Error: Not connected to server and local fallback is disabled";
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public static async Task<string> TakeScreenshot(string path, Vector2Int resolution)
        {
            try
            {
                if (_isConnected)
                {
                    // Take screenshot via WebSocket
                    return await _wsManager.TakeScreenshotAsync(path, resolution.x, resolution.y);
                }
                else if (_useLocalFallback)
                {
                    // Take screenshot locally as fallback
                    return Commands.TakeScreenshotCommand.Execute(path, resolution.x, resolution.y);
                }
                else
                {
                    return "Error: Not connected to server and local fallback is disabled";
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public static async Task<string> ModifyObject(string objectId, string propertyName, object propertyValue)
        {
            try
            {
                if (_isConnected)
                {
                    // Modify object via WebSocket
                    return await _wsManager.ModifyObjectAsync(objectId, propertyName, propertyValue);
                }
                else if (_useLocalFallback)
                {
                    // Modify object locally as fallback
                    return Commands.ModifyObjectCommand.Execute(objectId, propertyName, propertyValue);
                }
                else
                {
                    return "Error: Not connected to server and local fallback is disabled";
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public static async Task<string> GetLogs()
        {
            try
            {
                if (_isConnected)
                {
                    // Get logs via WebSocket
                    return await _wsManager.GetLogsAsync();
                }
                else if (_useLocalFallback)
                {
                    // Get logs locally as fallback
                    return Commands.GetLogsCommand.Execute();
                }
                else
                {
                    return "Error: Not connected to server and local fallback is disabled";
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public static async Task<string> GetUnityInfo()
        {
            try
            {
                if (_isConnected)
                {
                    // Get Unity info via WebSocket
                    return await _wsManager.GetUnityInfoAsync();
                }
                else if (_useLocalFallback)
                {
                    // Get Unity info locally as fallback
                    return Commands.GetUnityInfoCommand.Execute();
                }
                else
                {
                    return "Error: Not connected to server and local fallback is disabled";
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }
        
        private static void HandleWebSocketConnected()
        {
            _isConnected = true;
            OnConnected?.Invoke();
        }

        private static void HandleWebSocketDisconnected()
        {
            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        private static void HandleWebSocketError(string error)
        {
            OnError?.Invoke(error);
        }
    }
}