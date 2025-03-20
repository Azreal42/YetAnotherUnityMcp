using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace YetAnotherUnityMcp.Editor.WebSocket
{
    /// <summary>
    /// Interface for all WebSocket messages
    /// </summary>
    public interface IWebSocketMessage
    {
        /// <summary>
        /// Process the message on the main thread
        /// </summary>
        /// <param name="server">The WebSocketServer that received this message</param>
        void Process(WebSocketServer server);
    }

    /// <summary>
    /// Regular WebSocket message with JSON content
    /// </summary>
    public class WebSocketJsonMessage : IWebSocketMessage
    {
        public string JsonContent { get; }
        public long ReceivedTimestamp { get; }
        public Dictionary<string, object> ParsedContent { get; private set; }
        
        public WebSocketJsonMessage(string jsonContent)
        {
            JsonContent = jsonContent;
            ReceivedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            try
            {
                ParsedContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebSocketJsonMessage] Failed to parse JSON: {ex.Message}");
                ParsedContent = new Dictionary<string, object>();
            }
        }
        
        public void Process(WebSocketServer server)
        {
            // Calculate latency if client timestamp is present
            if (ParsedContent.TryGetValue("client_timestamp", out var timestampObj) && timestampObj != null)
            {
                try
                {
                    long clientTimestamp = Convert.ToInt64(timestampObj);
                    long latency = ReceivedTimestamp - clientTimestamp;
                    
                    string command = ParsedContent.TryGetValue("command", out var cmdObj) ? cmdObj?.ToString() : "unknown";
                    
                    Debug.Log($"[WebSocket Server] Message received - Latency: {latency}ms, Command: {command}, Size: {JsonContent.Length} bytes");
                }
                catch
                {
                    LogMessageContent();
                }
            }
            else
            {
                LogMessageContent();
            }
        }
        
        private void LogMessageContent()
        {
            // Log message content with length limit
            if (JsonContent.Length < 500)
            {
                Debug.Log($"[WebSocket Server] Message received: {JsonContent}");
            }
            else
            {
                Debug.Log($"[WebSocket Server] Message received: {JsonContent.Substring(0, 100)}... (truncated, {JsonContent.Length} bytes)");
            }
        }
    }

    /// <summary>
    /// Error message for handling WebSocket errors
    /// </summary>
    public class WebSocketErrorMessage : IWebSocketMessage
    {
        public string ErrorMessage { get; }
        
        public WebSocketErrorMessage(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
        
        public void Process(WebSocketServer server)
        {
            Debug.LogError($"[WebSocket Server] Error: {ErrorMessage}");
        }
    }

    /// <summary>
    /// Disconnect message for handling WebSocket disconnections
    /// </summary>
    public class WebSocketDisconnectMessage : IWebSocketMessage
    {
        public string Reason { get; }
        public string ConnectionId { get; }
        
        public WebSocketDisconnectMessage(string reason = "", string connectionId = null)
        {
            Reason = reason;
            ConnectionId = connectionId;
        }
        
        public void Process(WebSocketServer server)
        {
            if (!string.IsNullOrEmpty(Reason))
            {
                Debug.Log($"[WebSocket Server] Disconnected: {Reason}");
            }
            else
            {
                Debug.Log("[WebSocket Server] Disconnected");
            }
        }
    }

    /// <summary>
    /// Status update message for logging internal WebSocket server status
    /// </summary>
    public class WebSocketStatusMessage : IWebSocketMessage
    {
        public string Status { get; }
        public LogType LogLevel { get; }
        
        public WebSocketStatusMessage(string status, LogType logLevel = LogType.Log)
        {
            Status = status;
            LogLevel = logLevel;
        }
        
        public void Process(WebSocketServer server)
        {
            switch (LogLevel)
            {
                case LogType.Error:
                    Debug.LogError($"[WebSocket Server] {Status}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[WebSocket Server] {Status}");
                    break;
                default:
                    Debug.Log($"[WebSocket Server] {Status}");
                    break;
            }
        }
    }
}