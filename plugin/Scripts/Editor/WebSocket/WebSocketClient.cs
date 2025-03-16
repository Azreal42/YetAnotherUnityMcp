using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using UnityEngine;
using UnityEditor;
using System.Collections.Concurrent;

namespace YetAnotherUnityMcp.Editor.WebSocket
{
    /// <summary>
    /// WebSocket client for communicating with the MCP server
    /// </summary>
    public class WebSocketClient
    {
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnError;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;
        private string _serverUrl;
        private bool _isConnected;
        private readonly int _receiveBufferSize = 32768; // Increased buffer size for better performance
        
        // Thread-safe message queue using ConcurrentQueue
        private ConcurrentQueue<IWebSocketMessage> _messageQueue = new ConcurrentQueue<IWebSocketMessage>();
        
        // Performance monitoring
        private int _messageProcessedCount = 0;
        private DateTime _lastPerformanceLog = DateTime.Now;
        
        // Constructor - register for update
        public WebSocketClient()
        {
            // Register for the update event to process messages every frame
            EditorApplication.update += Update;
        }
        
        ~WebSocketClient()
        {
            // Unregister from the update event to prevent memory leaks
            EditorApplication.update -= Update;
        }

        /// <summary>
        /// Is the client connected to the server
        /// </summary>
        public bool IsConnected => _isConnected;
        
        /// <summary>
        /// Server URL
        /// </summary>
        public string ServerUrl => _serverUrl;
        
        /// <summary>
        /// Invokes the OnMessageReceived event
        /// </summary>
        internal void InvokeMessageReceived(string message)
        {
            OnMessageReceived?.Invoke(message);
        }
        
        /// <summary>
        /// Invokes the OnError event
        /// </summary>
        internal void InvokeError(string error)
        {
            OnError?.Invoke(error);
        }
        
        /// <summary>
        /// Invokes the OnDisconnected event
        /// </summary>
        internal void InvokeDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Connect to the WebSocket server
        /// </summary>
        /// <param name="serverUrl">WebSocket server URL (ws:// or wss://)</param>
        /// <returns>True if connected successfully, false otherwise</returns>
        public async Task<bool> ConnectAsync(string serverUrl)
        {
            try
            {
                if (_isConnected)
                {
                    await DisconnectAsync();
                }

                _serverUrl = serverUrl;

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Add subprotocol if needed
                //_webSocket.Options.AddSubProtocol("mcp-protocol");

                Debug.Log($"[WebSocket] Connecting to {serverUrl}...");
                
                // Connect to the server
                await _webSocket.ConnectAsync(new Uri(serverUrl), _cancellationTokenSource.Token);
                _isConnected = true;

                // Start receiving messages
                _receiveTask = ReceiveMessagesAsync();

                Debug.Log("[WebSocket] Connected successfully");
                OnConnected?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] Connection error: {ex.Message}");
                OnError?.Invoke($"Connection error: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the WebSocket server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!_isConnected || _webSocket == null)
                return;

            try
            {
                _cancellationTokenSource.Cancel();

                // Close the WebSocket connection
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                    "Client disconnected", 
                    CancellationToken.None);
                
                // Clean up resources
                _webSocket.Dispose();
                _webSocket = null;
                
                _isConnected = false;
                Debug.Log("[MCP WebSocket] Disconnected");
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP WebSocket] Disconnect error: {ex.Message}");
                OnError?.Invoke($"Disconnect error: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="message">Message to send</param>
        public async Task SendMessageAsync(string message)
        {
            if (!_isConnected || _webSocket == null)
            {
                Debug.LogError("[MCP WebSocket] Cannot send message: Not connected");
                OnError?.Invoke("Cannot send message: Not connected");
                return;
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), 
                    WebSocketMessageType.Text, 
                    true, 
                    _cancellationTokenSource.Token);

                // Log the message being sent
                if (message.Length < 500) {
                    _messageQueue.Enqueue(new WebSocketStatusMessage($"Message sent: {message}"));
                } else {
                    _messageQueue.Enqueue(new WebSocketStatusMessage($"Message sent: {message.Substring(0, 100)}... (truncated, {message.Length} bytes)"));
                }
            }
            catch (Exception ex)
            {
                // Queue error message
                _messageQueue.Enqueue(new WebSocketErrorMessage($"Send error: {ex.Message}"));
                
                if (_webSocket.State != WebSocketState.Open)
                {
                    _isConnected = false;
                    _messageQueue.Enqueue(new WebSocketDisconnectMessage("Connection lost"));
                }
            }
        }

        /// <summary>
        /// Continuously receive messages from the server
        /// </summary>
        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[_receiveBufferSize];
            var memoryBuffer = new Memory<byte>(buffer);
            
            try
            {
                while (_isConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Reset buffer for new message
                    ValueWebSocketReceiveResult result = default;
                    var messageBuilder = new StringBuilder();
                    
                    do
                    {
                        // Receive data with larger buffer for better performance
                        result = await _webSocket.ReceiveAsync(
                            memoryBuffer, 
                            _cancellationTokenSource.Token);

                        // Handle different message types
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            // Only decode the portion of the buffer that has data
                            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            messageBuilder.Append(message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Connection was closed
                            _isConnected = false;
                            
                            // Queue the disconnect message
                            _messageQueue.Enqueue(new WebSocketDisconnectMessage("Connection closed by the server"));
                            return;
                        }
                    }
                    while (!result.EndOfMessage); // Continue until we get the end of the message
                    
                    // Process the complete message
                    if (messageBuilder.Length > 0)
                    {
                        string completeMessage = messageBuilder.ToString();
                        
                        // Enqueue as a proper message object - logging will be handled by the message
                        _messageQueue.Enqueue(new WebSocketJsonMessage(completeMessage));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no need to report an error
                Debug.Log("[WebSocket] Receive operation cancelled");
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[WebSocket] Receive error: {ex.Message}");
                    
                    // Queue error and disconnect messages
                    _messageQueue.Enqueue(new WebSocketErrorMessage(ex.Message));
                    _messageQueue.Enqueue(new WebSocketDisconnectMessage("Error in WebSocket connection"));
                    
                    // Mark as disconnected
                    _isConnected = false;
                }
            }
        }
        
        /// <summary>
        /// Update method called every frame by the Unity editor
        /// </summary>
        private void Update()
        {
            // Check if we have messages to process
            if (!_messageQueue.IsEmpty)
            {
                // Log a warning if we have a large queue
                int messageCount = _messageQueue.Count;
                if (messageCount > 100)
                {
                    Debug.LogWarning($"[WebSocket] Large message queue: {messageCount} messages");
                }
                
                // Process a batch of messages
                ProcessMessageQueue();
                
                // Log performance stats periodically
                _messageProcessedCount += messageCount;
                TimeSpan elapsed = DateTime.Now - _lastPerformanceLog;
                if (elapsed.TotalSeconds >= 5)
                {
                    float messagesPerSecond = _messageProcessedCount / (float)elapsed.TotalSeconds;
                    if (messagesPerSecond > 100)
                    {
                        Debug.LogWarning($"[WebSocket] High message rate: {messagesPerSecond:F1} messages/sec");
                    }
                    _messageProcessedCount = 0;
                    _lastPerformanceLog = DateTime.Now;
                }
            }
        }
        
        /// <summary>
        /// Process message queue on the main thread
        /// </summary>
        private void ProcessMessageQueue()
        {
            try
            {
                // Process up to 10 messages per frame to avoid frame rate drops
                int messagesToProcess = Math.Min(_messageQueue.Count, 10);
                
                for (int i = 0; i < messagesToProcess; i++)
                {
                    // Try to dequeue a message
                    if (_messageQueue.TryDequeue(out IWebSocketMessage message))
                    {
                        // Process the message - this will handle all event invocation
                        message.Process(this);
                    }
                    else
                    {
                        // Queue is empty, stop processing
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] Error processing message queue: {ex.Message}");
                InvokeError($"Error processing message queue: {ex.Message}");
            }
        }
    }
}