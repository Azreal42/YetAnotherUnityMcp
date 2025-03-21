using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Collections.Concurrent;
using System.IO;

namespace YetAnotherUnityMcp.Editor.Net
{
    /// <summary>
    /// TCP server for communicating with MCP clients
    /// </summary>
    public class TcpServer
    {
        public event Action OnStarted;
        public event Action OnStopped;
        public event Action<string, TcpConnection> OnMessageReceived;
        public event Action<TcpConnection> OnClientConnected;
        public event Action<TcpConnection> OnClientDisconnected;
        public event Action<string> OnError;

        /// <summary>
        /// Raises the OnError event
        /// </summary>
        /// <param name="errorMessage">Error message to raise</param>
        public void RaiseError(string errorMessage)
        {
            Debug.LogError($"[TCP Server] Error: {errorMessage}");
            OnError?.Invoke(errorMessage);
        }
        
        /// <summary>
        /// Raises the OnStarted event
        /// </summary>
        public void RaiseStarted()
        {
            OnStarted?.Invoke();
        }
        
        /// <summary>
        /// Raises the OnStopped event
        /// </summary>
        public void RaiseStopped()
        {
            OnStopped?.Invoke();
        }
        
        /// <summary>
        /// Raises the OnMessageReceived event
        /// </summary>
        /// <param name="message">Message received</param>
        /// <param name="connection">Connection that received the message</param>
        public void RaiseMessageReceived(string message, TcpConnection connection)
        {
            OnMessageReceived?.Invoke(message, connection);
        }
        
        /// <summary>
        /// Raises the OnClientConnected event
        /// </summary>
        /// <param name="connection">Connection that connected</param>
        public void RaiseClientConnected(TcpConnection connection)
        {
            OnClientConnected?.Invoke(connection);
        }
        
        /// <summary>
        /// Raises the OnClientDisconnected event
        /// </summary>
        /// <param name="connection">Connection that disconnected</param>
        public void RaiseClientDisconnected(TcpConnection connection)
        {
            OnClientDisconnected?.Invoke(connection);
        }

        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;
        private bool _isRunning;
        private string _serverUrl;
        private readonly int _bufferSize = 32768; // Large buffer for better performance
        
        // List of active client connections (internal for processing messages)
        internal ConcurrentDictionary<string, TcpConnection> _connections = 
            new ConcurrentDictionary<string, TcpConnection>();
        
        // Thread-safe message queue for main thread processing
        private ConcurrentQueue<ITcpMessage> _messageQueue = new ConcurrentQueue<ITcpMessage>();
        
        // Performance monitoring
        private int _messageProcessedCount = 0;
        private DateTime _lastPerformanceLog = DateTime.Now;

        // Message delimiters and constants
        public const byte START_MARKER = 0x02; // STX (Start of Text)
        public const byte END_MARKER = 0x03;   // ETX (End of Text)
        public const string PING_MESSAGE = "PING";
        public const string PONG_RESPONSE = "PONG";
        public const string HANDSHAKE_REQUEST = "YAUM_HANDSHAKE_REQUEST";
        public const string HANDSHAKE_RESPONSE = "YAUM_HANDSHAKE_RESPONSE";

        /// <summary>
        /// Constructor - register for update
        /// </summary>
        public TcpServer()
        {
            // Register for the update event to process messages every frame
            EditorApplication.update += Update;
        }
        
        ~TcpServer()
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
        /// Start the TCP server
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

                _serverUrl = $"tcp://{host}:{port}/";
                
                // Create and start the TcpListener
                IPAddress ipAddress;
                if (host == "localhost" || host == "127.0.0.1")
                {
                    ipAddress = IPAddress.Loopback;
                }
                else if (host == "0.0.0.0")
                {
                    ipAddress = IPAddress.Any;
                }
                else
                {
                    ipAddress = IPAddress.Parse(host);
                }
                
                _listener = new TcpListener(ipAddress, port);
                _cancellationTokenSource = new CancellationTokenSource();

                Debug.Log($"[TCP Server] Starting on {_serverUrl}...");
                
                // Start the listener
                _listener.Start();
                _isRunning = true;

                // Start accepting connections
                _listenerTask = AcceptConnectionsAsync();

                Debug.Log("[TCP Server] Started successfully");
                RaiseStarted();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP Server] Start error: {ex.Message}");
                RaiseError($"Start error: {ex.Message}");
                _isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Stop the TCP server
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
                    await connection.CloseAsync("Server shutting down");
                }
                _connections.Clear();
                
                // Stop the listener
                _listener.Stop();
                _listener = null;
                
                _isRunning = false;
                Debug.Log("[TCP Server] Stopped");
                RaiseStopped();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP Server] Stop error: {ex.Message}");
                RaiseError($"Stop error: {ex.Message}");
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
                Debug.LogError("[TCP Server] Cannot send message: Server not running");
                RaiseError("Cannot send message: Server not running");
                return;
            }

            if (!_connections.TryGetValue(connectionId, out TcpConnection connection))
            {
                Debug.LogError($"[TCP Server] Cannot send message: Client {connectionId} not found");
                RaiseError($"Cannot send message: Client {connectionId} not found");
                return;
            }

            try
            {
                await connection.SendAsync(message);
                
                // Log the message being sent
                if (message.Length < 500) 
                {
                    _messageQueue.Enqueue(new TcpStatusMessage($"Message sent to {connectionId}: {message}"));
                } 
                else 
                {
                    _messageQueue.Enqueue(new TcpStatusMessage(
                        $"Message sent to {connectionId}: {message.Substring(0, 100)}... (truncated, {message.Length} bytes)"));
                }
            }
            catch (Exception ex)
            {
                // Queue error message
                _messageQueue.Enqueue(new TcpErrorMessage($"Send error to {connectionId}: {ex.Message}"));
                
                // Check if the client is still connected
                if (!connection.IsConnected)
                {
                    // Remove the client from active connections
                    if (_connections.TryRemove(connectionId, out _))
                    {
                        _messageQueue.Enqueue(new TcpDisconnectMessage($"Client {connectionId} disconnected"));
                        RaiseClientDisconnected(connection);
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
                Debug.LogError("[TCP Server] Cannot broadcast message: Server not running");
                RaiseError("Cannot broadcast message: Server not running");
                return;
            }

            // Get a snapshot of the current connections to avoid issues with concurrent modification
            var connections = new Dictionary<string, TcpConnection>(_connections);
            
            if (connections.Count == 0)
            {
                Debug.LogWarning("[TCP Server] No clients connected to broadcast to");
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
                    Debug.LogError($"[TCP Server] Error broadcasting to client {kvp.Key}: {ex.Message}");
                    disconnectedClients.Add(kvp.Key);
                }
            }
            
            // Remove disconnected clients
            foreach (var clientId in disconnectedClients)
            {
                if (_connections.TryRemove(clientId, out TcpConnection connection))
                {
                    _messageQueue.Enqueue(new TcpDisconnectMessage($"Client {clientId} disconnected"));
                    RaiseClientDisconnected(connection);
                }
            }
            
            // Log the broadcast
            _messageQueue.Enqueue(new TcpStatusMessage(
                $"Broadcast message to {connections.Count} clients (size: {message.Length} bytes)"));
        }

        /// <summary>
        /// Accept and handle new TCP connections
        /// </summary>
        private async Task AcceptConnectionsAsync()
        {
            try
            {
                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Wait for an incoming connection
                    var client = await _listener.AcceptTcpClientAsync();
                    
                    // Handle the connection in a separate task
                    _ = HandleClientConnectionAsync(client);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no need to report an error
                Debug.Log("[TCP Server] Accept operation cancelled");
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[TCP Server] Accept error: {ex.Message}");
                    RaiseError($"Accept error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle a new client connection
        /// </summary>
        /// <param name="client">TCP client that connected</param>
        private async Task HandleClientConnectionAsync(TcpClient client)
        {
            try
            {
                // Configure the TcpClient
                client.NoDelay = true; // Disable Nagle's algorithm for better responsiveness
                client.ReceiveBufferSize = _bufferSize;
                client.SendBufferSize = _bufferSize;
                
                // Create a new connection object
                string connectionId = Guid.NewGuid().ToString();
                var connection = new TcpConnection(connectionId, client);
                
                // Perform handshake
                if (!await PerformHandshake(connection))
                {
                    // Handshake failed, close the connection
                    await connection.CloseAsync("Handshake failed");
                    return;
                }
                
                // Add to active connections
                _connections.TryAdd(connectionId, connection);
                
                // Notify of new connection
                _messageQueue.Enqueue(new TcpStatusMessage(
                    $"Client connected: {connectionId} from {((IPEndPoint)client.Client.RemoteEndPoint).Address}:{((IPEndPoint)client.Client.RemoteEndPoint).Port}"));
                RaiseClientConnected(connection);
                
                // Start receiving messages from this client
                await ReceiveMessagesAsync(connection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP Server] Error handling client connection: {ex.Message}");
                RaiseError($"Error handling client connection: {ex.Message}");
                
                // Ensure the client is properly closed
                try { client.Close(); } catch { }
            }
        }

        /// <summary>
        /// Perform handshake with client
        /// </summary>
        private async Task<bool> PerformHandshake(TcpConnection connection)
        {
            try
            {
                // Wait for handshake request
                var stream = connection.Client.GetStream();
                byte[] buffer = new byte[1024];
                
                Debug.Log("[TCP Server] Waiting for handshake request from client...");
                
                // Read initial handshake request with timeout
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                
                // Wait for the read to complete or timeout
                if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
                {
                    Debug.LogError("[TCP Server] Handshake timeout");
                    return false;
                }
                
                int bytesRead = await readTask;
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                // Log received message details for debugging
                Debug.Log($"[TCP Server] Received handshake request: '{message}' (length: {bytesRead})");
                
                // Trim the message to handle any whitespace or newlines
                string trimmedMessage = message.Trim();
                Debug.Log($"[TCP Server] Trimmed handshake request: '{trimmedMessage}'");
                
                if (trimmedMessage == HANDSHAKE_REQUEST)
                {
                    // Send handshake response (as plain text, not framed)
                    byte[] response = Encoding.UTF8.GetBytes(HANDSHAKE_RESPONSE);
                    await stream.WriteAsync(response, 0, response.Length);
                    await stream.FlushAsync(); // Ensure the response is sent immediately
                    
                    Debug.Log("[TCP Server] Handshake successful");
                    return true;
                }
                else
                {
                    Debug.LogError($"[TCP Server] Invalid handshake request: {message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP Server] Handshake error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Continuously receive messages from a client
        /// </summary>
        /// <param name="connection">Client connection</param>
        private async Task ReceiveMessagesAsync(TcpConnection connection)
        {
            byte[] buffer = new byte[_bufferSize];
            var stream = connection.Client.GetStream();
            
            try
            {
                while (connection.IsConnected && 
                       !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for the start marker (STX)
                        int b;
                        while ((b = stream.ReadByte()) != START_MARKER)
                        {
                            if (b == -1) // End of stream
                            {
                                throw new EndOfStreamException("Client disconnected");
                            }
                            
                            await Task.Yield(); // Allow Unity to breathe
                        }
                        
                        // Read message length (4 bytes)
                        byte[] lengthBytes = new byte[4];
                        int bytesRead = 0;
                        while (bytesRead < 4)
                        {
                            int n = await stream.ReadAsync(lengthBytes, bytesRead, 4 - bytesRead, _cancellationTokenSource.Token);
                            if (n == 0)
                            {
                                throw new EndOfStreamException("Client disconnected");
                            }
                            bytesRead += n;
                        }
                        
                        int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                        
                        // Message sanity check
                        if (messageLength <= 0 || messageLength > _bufferSize - 1)
                        {
                            Debug.LogError($"[TCP Server] Invalid message length: {messageLength}");
                            continue;
                        }
                        
                        // Read the message data
                        byte[] messageData = new byte[messageLength];
                        bytesRead = 0;
                        while (bytesRead < messageLength)
                        {
                            int n = await stream.ReadAsync(messageData, bytesRead, messageLength - bytesRead, _cancellationTokenSource.Token);
                            if (n == 0)
                            {
                                throw new EndOfStreamException("Client disconnected");
                            }
                            bytesRead += n;
                        }
                        
                        // Read end marker (ETX)
                        b = stream.ReadByte();
                        if (b != END_MARKER)
                        {
                            Debug.LogError($"[TCP Server] Missing end marker, got: {b}");
                            continue;
                        }
                        
                        // Convert bytes to string
                        string message = Encoding.UTF8.GetString(messageData);
                        
                        // Handle special messages
                        if (message == PING_MESSAGE)
                        {
                            // Respond to ping with pong
                            await connection.SendAsync(PONG_RESPONSE);
                            continue;
                        }
                        
                        // Queue the message for processing on the main thread
                        _messageQueue.Enqueue(new TcpJsonMessage(message, connection));
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal cancellation
                        break;
                    }
                    catch (EndOfStreamException ex)
                    {
                        Debug.Log($"[TCP Server] Client {connection.Id} disconnected: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            Debug.LogError($"[TCP Server] Error receiving from client {connection.Id}: {ex.Message}");
                            _messageQueue.Enqueue(new TcpErrorMessage(
                                $"Receive error from client {connection.Id}: {ex.Message}"));
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[TCP Server] Unexpected error for client {connection.Id}: {ex.Message}");
                }
            }
            finally
            {
                // Ensure the connection is removed from active connections
                if (_connections.TryRemove(connection.Id, out _))
                {
                    // Queue the disconnect message
                    _messageQueue.Enqueue(new TcpDisconnectMessage($"Client {connection.Id} disconnected"));
                    RaiseClientDisconnected(connection);
                }
                
                // Close the connection
                try
                {
                    connection.Client.Close();
                }
                catch { /* Ignore errors during cleanup */ }
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
                    Debug.LogWarning($"[TCP Server] Large message queue: {messageCount} messages");
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
                        Debug.LogWarning($"[TCP Server] High message rate: {messagesPerSecond:F1} messages/sec");
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
                    if (_messageQueue.TryDequeue(out ITcpMessage message))
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
                Debug.LogError($"[TCP Server] Error processing message queue: {ex.Message}");
                RaiseError($"Error processing message queue: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Represents a client connection to the TCP server
    /// </summary>
    public class TcpConnection
    {
        public string Id { get; }
        public TcpClient Client { get; }
        public DateTime ConnectedAt { get; }
        
        public bool IsConnected => Client != null && Client.Connected;
        public IPEndPoint RemoteEndPoint => (IPEndPoint)Client?.Client?.RemoteEndPoint;
        
        public TcpConnection(string id, TcpClient client)
        {
            Id = id;
            Client = client;
            ConnectedAt = DateTime.Now;
        }
        
        /// <summary>
        /// Send a message to this client
        /// </summary>
        /// <param name="message">Message to send</param>
        public async Task SendAsync(string message)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Client is not connected");
            }
            
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            
            NetworkStream stream = Client.GetStream();
            
            // Frame format: STX + [LENGTH:4] + [MESSAGE] + ETX
            await stream.WriteAsync(new byte[] { TcpServer.START_MARKER }, 0, 1); // STX
            await stream.WriteAsync(lengthBytes, 0, 4); // Message length
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length); // Message content
            await stream.WriteAsync(new byte[] { TcpServer.END_MARKER }, 0, 1); // ETX
            
            await stream.FlushAsync(); // Ensure all data is sent
        }
        
        /// <summary>
        /// Close the connection
        /// </summary>
        public async Task CloseAsync(string reason)
        {
            if (IsConnected)
            {
                try
                {
                    // Try to send a close message
                    string closeMessage = $"{{\"type\":\"close\",\"reason\":\"{reason}\"}}";
                    await SendAsync(closeMessage);
                }
                catch
                {
                    // Ignore errors during close message send
                }
                
                // Close the client
                Client.Close();
            }
        }
    }
}