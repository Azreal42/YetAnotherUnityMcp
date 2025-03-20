using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Collections.Concurrent;

namespace YetAnotherUnityMcp.Editor.WebSocket
{
    /// <summary>
    /// WebSocket server for communicating with MCP clients
    /// </summary>
    public class WebSocketServer
    {
        public event Action OnStarted;
        public event Action OnStopped;
        public event Action<string, WebSocketConnection> OnMessageReceived;
        public event Action<WebSocketConnection> OnClientConnected;
        public event Action<WebSocketConnection> OnClientDisconnected;
        public event Action<string> OnError;

        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;
        private bool _isRunning;
        private string _serverUrl;
        private readonly int _bufferSize = 32768; // Large buffer for better performance
        
        // List of active client connections (internal for processing messages)
        internal ConcurrentDictionary<string, WebSocketConnection> _connections = 
            new ConcurrentDictionary<string, WebSocketConnection>();
        
        // Thread-safe message queue for main thread processing
        private ConcurrentQueue<IWebSocketMessage> _messageQueue = new ConcurrentQueue<IWebSocketMessage>();
        
        // Performance monitoring
        private int _messageProcessedCount = 0;
        private DateTime _lastPerformanceLog = DateTime.Now;

        /// <summary>
        /// Constructor - register for update
        /// </summary>
        public WebSocketServer()
        {
            // Register for the update event to process messages every frame
            EditorApplication.update += Update;
        }
        
        ~WebSocketServer()
        {
            // Unregister from the update event to prevent memory leaks
            EditorApplication.update -= Update;
            
            // Ensure server is stopped
            StopAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Is the server running
        /// </summary>
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// Server URL
        /// </summary>
        public string ServerUrl => _serverUrl;
        
        /// <summary>
        /// Number of connected clients
        /// </summary>
        public int ConnectionCount => _connections.Count;

        /// <summary>
        /// Start the WebSocket server
        /// </summary>
        /// <param name="host">Hostname to bind to</param>
        /// <param name="port">Port to listen on</param>
        /// <returns>True if started successfully, false otherwise</returns>
        public async Task<bool> StartAsync(string host = "localhost", int port = 8080)
        {
            try
            {
                if (_isRunning)
                {
                    await StopAsync();
                }

                _serverUrl = $"ws://{host}:{port}/";
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://{host}:{port}/");
                _cancellationTokenSource = new CancellationTokenSource();

                Debug.Log($"[WebSocket Server] Starting on {_serverUrl}...");
                
                // Start the listener
                _listener.Start();
                _isRunning = true;

                // Start accepting connections
                _listenerTask = AcceptConnectionsAsync();

                Debug.Log("[WebSocket Server] Started successfully");
                OnStarted?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket Server] Start error: {ex.Message}");
                OnError?.Invoke($"Start error: {ex.Message}");
                _isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning || _listener == null)
                return;

            try
            {
                _cancellationTokenSource.Cancel();

                // Close all client connections
                foreach (var connection in _connections.Values)
                {
                    await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down");
                }
                _connections.Clear();
                
                // Stop the listener
                _listener.Stop();
                _listener.Close();
                _listener = null;
                
                _isRunning = false;
                Debug.Log("[WebSocket Server] Stopped");
                OnStopped?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket Server] Stop error: {ex.Message}");
                OnError?.Invoke($"Stop error: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a message to a specific client
        /// </summary>
        /// <param name="connectionId">ID of the client to send to</param>
        /// <param name="message">Message to send</param>
        public async Task SendMessageAsync(string connectionId, string message)
        {
            if (!_isRunning)
            {
                Debug.LogError("[WebSocket Server] Cannot send message: Server not running");
                OnError?.Invoke("Cannot send message: Server not running");
                return;
            }

            if (!_connections.TryGetValue(connectionId, out WebSocketConnection connection))
            {
                Debug.LogError($"[WebSocket Server] Cannot send message: Client {connectionId} not found");
                OnError?.Invoke($"Cannot send message: Client {connectionId} not found");
                return;
            }

            try
            {
                await connection.SendAsync(message);
                
                // Log the message being sent
                if (message.Length < 500) 
                {
                    _messageQueue.Enqueue(new WebSocketStatusMessage($"Message sent to {connectionId}: {message}"));
                } 
                else 
                {
                    _messageQueue.Enqueue(new WebSocketStatusMessage(
                        $"Message sent to {connectionId}: {message.Substring(0, 100)}... (truncated, {message.Length} bytes)"));
                }
            }
            catch (Exception ex)
            {
                // Queue error message
                _messageQueue.Enqueue(new WebSocketErrorMessage($"Send error to {connectionId}: {ex.Message}"));
                
                // Check if the client is still connected
                if (connection.State != WebSocketState.Open)
                {
                    // Remove the client from active connections
                    if (_connections.TryRemove(connectionId, out _))
                    {
                        _messageQueue.Enqueue(new WebSocketDisconnectMessage($"Client {connectionId} disconnected"));
                        OnClientDisconnected?.Invoke(connection);
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast a message to all connected clients
        /// </summary>
        /// <param name="message">Message to broadcast</param>
        public async Task BroadcastMessageAsync(string message)
        {
            if (!_isRunning)
            {
                Debug.LogError("[WebSocket Server] Cannot broadcast message: Server not running");
                OnError?.Invoke("Cannot broadcast message: Server not running");
                return;
            }

            // Get a snapshot of the current connections to avoid issues with concurrent modification
            var connections = new Dictionary<string, WebSocketConnection>(_connections);
            
            if (connections.Count == 0)
            {
                Debug.LogWarning("[WebSocket Server] No clients connected to broadcast to");
                return;
            }

            var disconnectedClients = new List<string>();
            
            foreach (var kvp in connections)
            {
                try
                {
                    await kvp.Value.SendAsync(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebSocket Server] Error broadcasting to client {kvp.Key}: {ex.Message}");
                    disconnectedClients.Add(kvp.Key);
                }
            }
            
            // Remove disconnected clients
            foreach (var clientId in disconnectedClients)
            {
                if (_connections.TryRemove(clientId, out WebSocketConnection connection))
                {
                    _messageQueue.Enqueue(new WebSocketDisconnectMessage($"Client {clientId} disconnected"));
                    OnClientDisconnected?.Invoke(connection);
                }
            }
            
            // Log the broadcast
            _messageQueue.Enqueue(new WebSocketStatusMessage(
                $"Broadcast message to {connections.Count} clients (size: {message.Length} bytes)"));
        }

        /// <summary>
        /// Accept and handle new WebSocket connections
        /// </summary>
        private async Task AcceptConnectionsAsync()
        {
            try
            {
                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Wait for an incoming connection
                    var context = await _listener.GetContextAsync();
                    
                    // Handle the connection in a separate task
                    _ = HandleClientConnectionAsync(context);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no need to report an error
                Debug.Log("[WebSocket Server] Accept operation cancelled");
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[WebSocket Server] Accept error: {ex.Message}");
                    OnError?.Invoke($"Accept error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle a new client connection
        /// </summary>
        /// <param name="context">HTTP context for the connection</param>
        private async Task HandleClientConnectionAsync(HttpListenerContext context)
        {
            try
            {
                // Ensure this is a WebSocket request
                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    return;
                }
                
                // Accept the WebSocket connection
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                
                // Create a new connection object
                string connectionId = Guid.NewGuid().ToString();
                var connection = new WebSocketConnection(connectionId, webSocket, context.Request.RemoteEndPoint);
                
                // Add to active connections
                _connections.TryAdd(connectionId, connection);
                
                // Notify of new connection
                _messageQueue.Enqueue(new WebSocketStatusMessage(
                    $"Client connected: {connectionId} from {context.Request.RemoteEndPoint}"));
                OnClientConnected?.Invoke(connection);
                
                // Start receiving messages from this client
                await ReceiveMessagesAsync(connection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket Server] Error handling client connection: {ex.Message}");
                OnError?.Invoke($"Error handling client connection: {ex.Message}");
                
                // Ensure the context is properly closed
                try { context.Response.Close(); } catch { }
            }
        }

        /// <summary>
        /// Continuously receive messages from a client
        /// </summary>
        /// <param name="connection">Client connection</param>
        private async Task ReceiveMessagesAsync(WebSocketConnection connection)
        {
            byte[] buffer = new byte[_bufferSize];
            var memoryBuffer = new Memory<byte>(buffer);
            
            try
            {
                while (connection.State == WebSocketState.Open && 
                       !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Reset buffer for new message
                    ValueWebSocketReceiveResult result = default;
                    var messageBuilder = new StringBuilder();
                    
                    do
                    {
                        // Receive data with larger buffer for better performance
                        result = await connection.WebSocket.ReceiveAsync(
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
                            // Connection was closed by the client
                            await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledging close frame");
                            
                            // Remove from active connections
                            if (_connections.TryRemove(connection.Id, out _))
                            {
                                // Queue the disconnect message
                                _messageQueue.Enqueue(new WebSocketDisconnectMessage(
                                    $"Client {connection.Id} disconnected gracefully"));
                                OnClientDisconnected?.Invoke(connection);
                            }
                            return;
                        }
                    }
                    while (!result.EndOfMessage); // Continue until we get the end of the message
                    
                    // Process the complete message
                    if (messageBuilder.Length > 0)
                    {
                        string completeMessage = messageBuilder.ToString();
                        _messageQueue.Enqueue(new WebSocketJsonMessage(completeMessage));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no need to report an error
                Debug.Log($"[WebSocket Server] Receive operation cancelled for client {connection.Id}");
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[WebSocket Server] Receive error from client {connection.Id}: {ex.Message}");
                    _messageQueue.Enqueue(new WebSocketErrorMessage(
                        $"Receive error from client {connection.Id}: {ex.Message}"));
                }
            }
            finally
            {
                // Ensure the connection is removed from active connections
                if (_connections.TryRemove(connection.Id, out _))
                {
                    // Queue the disconnect message
                    _messageQueue.Enqueue(new WebSocketDisconnectMessage($"Client {connection.Id} disconnected"));
                    OnClientDisconnected?.Invoke(connection);
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
                    Debug.LogWarning($"[WebSocket Server] Large message queue: {messageCount} messages");
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
                        Debug.LogWarning($"[WebSocket Server] High message rate: {messagesPerSecond:F1} messages/sec");
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
                        // Process the message - handle all event invocation
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
                Debug.LogError($"[WebSocket Server] Error processing message queue: {ex.Message}");
                OnError?.Invoke($"Error processing message queue: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Represents a client connection to the WebSocket server
    /// </summary>
    public class WebSocketConnection
    {
        public string Id { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public IPEndPoint RemoteEndPoint { get; }
        public DateTime ConnectedAt { get; }
        
        public WebSocketState State => WebSocket.State;
        
        public WebSocketConnection(string id, System.Net.WebSockets.WebSocket webSocket, IPEndPoint remoteEndPoint)
        {
            Id = id;
            WebSocket = webSocket;
            RemoteEndPoint = remoteEndPoint;
            ConnectedAt = DateTime.Now;
        }
        
        /// <summary>
        /// Send a message to this client
        /// </summary>
        /// <param name="message">Message to send</param>
        public async Task SendAsync(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await WebSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        
        /// <summary>
        /// Close the connection
        /// </summary>
        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription)
        {
            if (WebSocket.State == WebSocketState.Open)
            {
                await WebSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
            }
        }
    }

}