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
        /// <param name="client">The WebSocketClient that received this message</param>
        void Process(WebSocketClient client);
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
        
        public void Process(WebSocketClient client)
        {
            // Calculate latency if server timestamp is present
            if (ParsedContent.TryGetValue("server_timestamp", out var timestampObj) && timestampObj != null)
            {
                try
                {
                    long serverTimestamp = Convert.ToInt64(timestampObj);
                    long latency = ReceivedTimestamp - serverTimestamp;
                    
                    string messageType = ParsedContent.TryGetValue("type", out var typeObj) ? typeObj?.ToString() : "unknown";
                    string action = ParsedContent.TryGetValue("action", out var actionObj) ? actionObj?.ToString() : "unknown";
                    
                    Debug.Log($"[WebSocket] Message received - Latency: {latency}ms, Type: {messageType}, Action: {action}, Size: {JsonContent.Length} bytes");
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
            
            // Invoke the message received event
            client.InvokeMessageReceived(JsonContent);
        }
        
        private void LogMessageContent()
        {
            // Log message content with length limit
            if (JsonContent.Length < 500)
            {
                Debug.Log($"[WebSocket] Message received: {JsonContent}");
            }
            else
            {
                Debug.Log($"[WebSocket] Message received: {JsonContent.Substring(0, 100)}... (truncated, {JsonContent.Length} bytes)");
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
        
        public void Process(WebSocketClient client)
        {
            Debug.LogError($"[WebSocket] Error: {ErrorMessage}");
            client.InvokeError(ErrorMessage);
        }
    }

    /// <summary>
    /// Disconnect message for handling WebSocket disconnections
    /// </summary>
    public class WebSocketDisconnectMessage : IWebSocketMessage
    {
        public string Reason { get; }
        
        public WebSocketDisconnectMessage(string reason = "")
        {
            Reason = reason;
        }
        
        public void Process(WebSocketClient client)
        {
            if (!string.IsNullOrEmpty(Reason))
            {
                Debug.Log($"[WebSocket] Disconnected: {Reason}");
            }
            else
            {
                Debug.Log("[WebSocket] Disconnected");
            }
            
            client.InvokeDisconnected();
        }
    }

    /// <summary>
    /// Status update message for logging internal WebSocket client status
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
        
        public void Process(WebSocketClient client)
        {
            switch (LogLevel)
            {
                case LogType.Error:
                    Debug.LogError($"[WebSocket] {Status}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[WebSocket] {Status}");
                    break;
                default:
                    Debug.Log($"[WebSocket] {Status}");
                    break;
            }
        }
    }
}