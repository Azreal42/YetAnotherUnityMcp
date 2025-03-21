using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace YetAnotherUnityMcp.Editor.Net
{
    /// <summary>
    /// Interface for all TCP messages
    /// </summary>
    public interface ITcpMessage
    {
        /// <summary>
        /// Process the message on the main thread
        /// </summary>
        /// <param name="server">The TCP server that received this message</param>
        void Process(TcpServer server);
    }

    /// <summary>
    /// Regular TCP message with JSON content
    /// </summary>
    public class TcpJsonMessage : ITcpMessage
    {
        public string JsonContent { get; }
        public long ReceivedTimestamp { get; }
        public Dictionary<string, object> ParsedContent { get; private set; }
        public TcpConnection Connection { get; }
        
        public TcpJsonMessage(string jsonContent)
        {
            JsonContent = jsonContent;
            ReceivedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Connection = null;
            
            try
            {
                ParsedContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TCP Message] Failed to parse JSON: {ex.Message}");
                ParsedContent = new Dictionary<string, object>();
            }
        }
        
        // Constructor with connection info
        public TcpJsonMessage(string jsonContent, TcpConnection connection)
        {
            JsonContent = jsonContent;
            ReceivedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Connection = connection;
            
            try
            {
                ParsedContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TCP Message] Failed to parse JSON: {ex.Message}");
                ParsedContent = new Dictionary<string, object>();
            }
        }
        
        public void Process(TcpServer server)
        {
            // Calculate latency if client timestamp is present
            if (ParsedContent.TryGetValue("client_timestamp", out var timestampObj) && timestampObj != null)
            {
                try
                {
                    long clientTimestamp = Convert.ToInt64(timestampObj);
                    long latency = ReceivedTimestamp - clientTimestamp;
                    
                    string command = ParsedContent.TryGetValue("command", out var cmdObj) ? cmdObj?.ToString() : "unknown";
                    
                    Debug.Log($"[TCP Server] Message received - Latency: {latency}ms, Command: {command}, Size: {JsonContent.Length} bytes");
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
            
            // Notify message received event
            if (Connection != null)
            {
                server.RaiseMessageReceived(JsonContent, Connection);
            }
        }
        
        private void LogMessageContent()
        {
            // Log message content with length limit
            if (JsonContent.Length < 500)
            {
                Debug.Log($"[TCP Server] Message received: {JsonContent}");
            }
            else
            {
                Debug.Log($"[TCP Server] Message received: {JsonContent.Substring(0, 100)}... (truncated, {JsonContent.Length} bytes)");
            }
        }
    }

    /// <summary>
    /// Error message for handling TCP errors
    /// </summary>
    public class TcpErrorMessage : ITcpMessage
    {
        public string ErrorMessage { get; }
        
        public TcpErrorMessage(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
        
        public void Process(TcpServer server)
        {
            Debug.LogError($"[TCP Server] Error: {ErrorMessage}");
            server.RaiseError(ErrorMessage);
        }
    }

    /// <summary>
    /// Disconnect message for handling TCP disconnections
    /// </summary>
    public class TcpDisconnectMessage : ITcpMessage
    {
        public string Reason { get; }
        public string ConnectionId { get; }
        public TcpConnection Connection { get; }
        
        public TcpDisconnectMessage(string reason = "", string connectionId = null)
        {
            Reason = reason;
            ConnectionId = connectionId;
            Connection = null;
        }
        
        public TcpDisconnectMessage(string reason, TcpConnection connection)
        {
            Reason = reason;
            ConnectionId = connection?.Id;
            Connection = connection;
        }
        
        public void Process(TcpServer server)
        {
            if (!string.IsNullOrEmpty(Reason))
            {
                Debug.Log($"[TCP Server] Disconnected: {Reason}");
            }
            else
            {
                Debug.Log("[TCP Server] Disconnected");
            }
            
            // If we have a connection object, raise the disconnected event
            if (Connection != null)
            {
                server.RaiseClientDisconnected(Connection);
            }
        }
    }

    /// <summary>
    /// Status update message for logging internal TCP server status
    /// </summary>
    public class TcpStatusMessage : ITcpMessage
    {
        public string Status { get; }
        public LogType LogLevel { get; }
        
        public TcpStatusMessage(string status, LogType logLevel = LogType.Log)
        {
            Status = status;
            LogLevel = logLevel;
        }
        
        public void Process(TcpServer server)
        {
            switch (LogLevel)
            {
                case LogType.Error:
                    Debug.LogError($"[TCP Server] {Status}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[TCP Server] {Status}");
                    break;
                default:
                    Debug.Log($"[TCP Server] {Status}");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Message for handling client connection events on the main thread
    /// </summary>
    public class TcpConnectMessage : ITcpMessage
    {
        public TcpConnection Connection { get; }
        private TcpServer _server;
        
        public TcpConnectMessage(TcpConnection connection, TcpServer server)
        {
            Connection = connection;
            _server = server;
        }
        
        public void Process(TcpServer server)
        {
            // Notify of client connection on the main thread
            Debug.Log($"[TCP Server] Processing connection notification for client {Connection.Id}");
            server.RaiseClientConnected(Connection);
        }
    }
}