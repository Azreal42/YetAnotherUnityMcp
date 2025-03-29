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
            Debug.Log($"[TCP Server] RaiseStopped");
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
        private bool _isStarting;

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
            StopAsync("~TcpServer").GetAwaiter().GetResult();
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
                if (_isStarting)
                {
                    Debug.LogWarning("[TCP Server] Already starting, please wait");
                    return false;
                }

                _isStarting = true;

                if (_isRunning)
                {
                    await StopAsync("restart");
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
                
                _isStarting = false;
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Start error: {ex.Message}");
                _isRunning = false;
                _isStarting = false;
                return false;
            }
        }

        /// <summary>
        /// Stop the TCP server
        /// </summary>
        public async Task StopAsync(string reason = null)
        {
            Debug.Log($"[TCP Server] Stopping...({reason})");
            if (!_isRunning || _listener == null)
            {
                Debug.Log("[TCP Server] Cannot stop: Server not running");
                return;
            }

            try
            {
                _cancellationTokenSource.Cancel();

                // Stop the listener
                _listener.Stop();
                _listener = null;

                // Close all client connections
                foreach (var connection in _connections.Values)
                {
                    connection.CloseAsync("Server shutting down").Wait(new TimeSpan(0, 0, 5));
                }
                _connections.Clear();
                                
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
                Debug.Log("[TCP Server] Client connected: " + client.Client.RemoteEndPoint);
                
                // Configure the TcpClient
                client.NoDelay = true; // Disable Nagle's algorithm for better responsiveness
                client.ReceiveBufferSize = _bufferSize;
                client.SendBufferSize = _bufferSize;
                
                // Create a new connection object
                string connectionId = Guid.NewGuid().ToString();
                var connection = new TcpConnection(connectionId, client);
                
                // Run handshake and message handling in a background thread to avoid freezing Unity
                await Task.Run(async () => 
                {
                    try
                    {
                        // Perform handshake (in the background thread)
                        if (!await PerformHandshake(connection))
                        {
                            // Handshake failed, close the connection
                            await connection.CloseAsync("Handshake failed");
                            return;
                        }
                        
                        // Add to active connections (thread-safe)
                        _connections.TryAdd(connectionId, connection);
                        
                        // Queue notification message for main thread
                        _messageQueue.Enqueue(new TcpStatusMessage(
                            $"Client connected: {connectionId} from {((IPEndPoint)client.Client.RemoteEndPoint).Address}:{((IPEndPoint)client.Client.RemoteEndPoint).Port}"));
                        
                        // Need to use helper method to invoke event on main thread later via the message queue
                        _messageQueue.Enqueue(new TcpConnectMessage(connection, this));
                        
                        // Start receiving messages from this client (still in background thread)
                        await ReceiveMessagesAsync(connection);
                    }
                    catch (Exception ex)
                    {
                        // Queue error message for main thread
                        _messageQueue.Enqueue(new TcpErrorMessage($"Error handling client {connectionId}: {ex.Message}"));
                        
                        // Remove from connections if added
                        if (_connections.TryRemove(connectionId, out _))
                        {
                            _messageQueue.Enqueue(new TcpDisconnectMessage($"Client {connectionId} disconnected due to error"));
                        }
                        
                        // Close connection
                        try { connection.Client.Close(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the error but we don't need to do anything else here since the Task.Run in the method
                // body already has its own error handling that will close connections and clean up
                Debug.LogError($"[TCP Server] Error initializing client connection: {ex.Message}");
                _messageQueue.Enqueue(new TcpErrorMessage($"Error initializing client connection: {ex.Message}"));
                
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
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Extended timeout
                
                // Use a more reliable approach - read until we detect the full handshake
                int totalBytesRead = 0;
                bool handshakeFound = false;
                
                try
                {
                    while (totalBytesRead < buffer.Length)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationTokenSource.Token);
                        
                        if (bytesRead == 0)
                        {
                            // Connection closed
                            Debug.LogError("[TCP Server] Connection closed during handshake");
                            return false;
                        }
                        
                        totalBytesRead += bytesRead;
                        
                        // Convert current buffer to string and check for handshake
                        string currentMessage = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                        
                        // Log progress
                        Debug.Log($"[TCP Server] Read {bytesRead} bytes, total: {totalBytesRead}, current content: '{currentMessage}'");
                        
                        // Check if the buffer contains the handshake request (with any whitespace/formatting differences)
                        if (currentMessage.Contains(HANDSHAKE_REQUEST))
                        {
                            handshakeFound = true;
                            break;
                        }
                        
                                // Use Task.Yield() to ensure the main thread isn't blocked
                        await Task.Yield();
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.LogError("[TCP Server] Handshake timeout");
                    return false;
                }
                
                if (!handshakeFound)
                {
                    Debug.LogError("[TCP Server] Handshake request not found in received data");
                    return false;
                }
                
                // Log the received handshake request
                string message = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                Debug.Log($"[TCP Server] Received handshake request: '{message}' (length: {totalBytesRead})");
                
                // Log hex representation for binary analysis
                string hexBytes = BitConverter.ToString(buffer, 0, totalBytesRead);
                Debug.Log($"[TCP Server] Handshake bytes: {hexBytes}");
                
                // Send handshake response (as plain text, not framed)
                byte[] response = Encoding.UTF8.GetBytes(HANDSHAKE_RESPONSE);
                Debug.Log($"[TCP Server] Sending handshake response: '{HANDSHAKE_RESPONSE}' (length: {response.Length})");
                string hexResponse = BitConverter.ToString(response);
                Debug.Log($"[TCP Server] Response bytes: {hexResponse}");
                
                await stream.WriteAsync(response, 0, response.Length);
                await stream.FlushAsync(); // Ensure the response is sent immediately
                
                // Add a small delay to ensure the client receives the response
                await Task.Delay(100);
                
                Debug.Log("[TCP Server] Handshake successful");
                return true;
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
            
            Debug.Log($"[TCP Server] Starting message receive loop for client {connection.Id}");
            
            // Setup a ping timer for this client to keep the connection alive
            System.Timers.Timer pingTimer = new System.Timers.Timer(30000); // 30 seconds
            
            // Create a dedicated cancellation token source for this client
            using (var clientCts = new CancellationTokenSource())
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, clientCts.Token))
            {
                try
                {
                    pingTimer.Elapsed += async (s, e) =>
                    {
                        try
                        {
                            // Only send ping if connected
                            if (connection.IsConnected && !linkedCts.Token.IsCancellationRequested)
                            {
                                await connection.SendAsync(PING_MESSAGE);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TCP Server] Error sending ping to client {connection.Id}: {ex.Message}");
                        }
                    };
                    pingTimer.Start();
                    
                    while (connection.IsConnected && 
                           !linkedCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        Debug.Log($"[TCP Server] Waiting for start marker (STX) from client {connection.Id}...");
                        
                        // Wait for the start marker (STX) using async method with timeout
                        int bytesChecked = 0;
                        bool startMarkerFound = false;
                        
                        // Temporary buffer to log initial bytes
                        byte[] initialBytes = new byte[16];
                        int initialBytesCount = 0;
                        
                        // Create a timeout for the read operation
                        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                        {
                            using (var readTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                                timeoutCts.Token, _cancellationTokenSource.Token))
                            {
                                try 
                                {
                                    // Set a limit to avoid infinite loops
                                    while (bytesChecked < 1000 && !readTimeoutCts.Token.IsCancellationRequested)
                                    {
                                        // Read one byte asynchronously with timeout
                                        byte[] oneByte = new byte[1];
                                        int bytesReadForMarker = await stream.ReadAsync(oneByte, 0, 1, readTimeoutCts.Token);
                                        
                                        if (bytesReadForMarker == 0)
                                        {
                                            Debug.LogError($"[TCP Server] Client {connection.Id} disconnected while waiting for start marker");
                                            throw new EndOfStreamException("Client disconnected");
                                        }
                                        
                                        byte b = oneByte[0];
                                        bytesChecked++;
                                        
                                        // Store initial bytes for debugging
                                        if (initialBytesCount < initialBytes.Length)
                                        {
                                            initialBytes[initialBytesCount++] = b;
                                        }
                                        
                                        if (b == START_MARKER)
                                        {
                                            Debug.Log($"[TCP Server] Found start marker (STX) after {bytesChecked} bytes");
                                            startMarkerFound = true;
                                            break;
                                        }
                                        
                                        // Log occasionally
                                        if (bytesChecked % 10 == 0)
                                        {
                                            string hexInitial = BitConverter.ToString(initialBytes, 0, initialBytesCount);
                                            Debug.Log($"[TCP Server] Checked {bytesChecked} bytes, no start marker yet. Initial bytes: {hexInitial}");
                                        }
                                        
                                        // Allow other tasks to run
                                        await Task.Yield();
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    if (timeoutCts.Token.IsCancellationRequested && !_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        Debug.LogWarning($"[TCP Server] Timeout while waiting for start marker from client {connection.Id}");
                                    }
                                    throw; // Re-throw for outer catch to handle
                                }
                            }
                        }
                        
                        if (!startMarkerFound)
                        {
                            string hexInitial = BitConverter.ToString(initialBytes, 0, initialBytesCount);
                            Debug.LogError($"[TCP Server] No start marker found after {bytesChecked} bytes. Initial bytes: {hexInitial}");
                            continue;
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
                        
                        // Read end marker (ETX) asynchronously with timeout
                        byte[] endMarkerBytes = new byte[1];
                        try
                        {
                            // Use a timeout to avoid blocking forever
                            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                            {
                                using (var endMarkerCts = CancellationTokenSource.CreateLinkedTokenSource(
                                    timeoutCts.Token, _cancellationTokenSource.Token))
                                {
                                    int endMarkerBytesRead = await stream.ReadAsync(endMarkerBytes, 0, 1, endMarkerCts.Token);
                                    
                                    if (endMarkerBytesRead == 0)
                                    {
                                        throw new EndOfStreamException("Client disconnected while reading end marker");
                                    }
                                }
                            }
                            
                            byte endMarker = endMarkerBytes[0];
                            if (endMarker != END_MARKER)
                            {
                                Debug.LogError($"[TCP Server] Missing end marker, got: {endMarker}");
                                
                                // If this looks like the end of JSON ('}'), we'll be more forgiving
                                if (endMarker == 0x7D) // '}'
                                {
                                    string messageContent = Encoding.UTF8.GetString(messageData);
                                    if (messageContent.TrimEnd().EndsWith("}"))
                                    {
                                        Debug.LogWarning("[TCP Server] Got '}' instead of ETX - appears to be valid JSON, accepting anyway");
                                        // Queue the message for processing on the main thread
                                        _messageQueue.Enqueue(new TcpJsonMessage(messageContent, connection));
                                    }
                                }
                                
                                continue;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.LogWarning($"[TCP Server] Timeout while reading end marker from client {connection.Id}");
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
                    if (!linkedCts.Token.IsCancellationRequested)
                    {
                        Debug.LogError($"[TCP Server] Unexpected error for client {connection.Id}: {ex.Message}");
                    }
                }
                finally
                {
                    // Stop the ping timer
                    pingTimer.Stop();
                    pingTimer.Dispose();
                    
                    // Ensure the connection is removed from active connections
                    if (_connections.TryRemove(connection.Id, out _))
                    {
                        // Queue the disconnect message for main thread processing
                        // This will also invoke the OnClientDisconnected event on the main thread
                        _messageQueue.Enqueue(new TcpDisconnectMessage($"Client {connection.Id} disconnected", connection));
                        
                        // Queue disconnect notification for main thread
                        _messageQueue.Enqueue(new TcpStatusMessage($"Client {connection.Id} disconnected", LogType.Warning));
                    }
                    
                    // Close the connection
                    try
                    {
                        connection.Client.Close();
                    }
                    catch { /* Ignore errors during cleanup */ }
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
                // Use a time-based approach to avoid blocking the main thread for too long
                DateTime startTime = DateTime.Now;
                int processedCount = 0;
                int maxTimeMs = 5; // Max milliseconds to spend processing messages per frame
                
                // Process messages until we reach time limit or the queue is empty
                while (!_messageQueue.IsEmpty && (DateTime.Now - startTime).TotalMilliseconds < maxTimeMs && processedCount < 20)
                {
                    // Try to dequeue a message
                    if (_messageQueue.TryDequeue(out ITcpMessage message))
                    {
                        // Process the message - handle all event invocation
                        message.Process(this);
                        processedCount++;
                    }
                    else
                    {
                        // Queue is empty, stop processing
                        break;
                    }
                }
                
                // If we still have many messages, log a warning
                if (_messageQueue.Count > 50)
                {
                    Debug.LogWarning($"[TCP Server] Message queue still has {_messageQueue.Count} messages after processing {processedCount}");
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
            
            // Create a single buffer for the complete frame to ensure atomic write
            int totalLength = 1 + 4 + messageBytes.Length + 1; // STX + LENGTH + MESSAGE + ETX
            byte[] frameBuffer = new byte[totalLength];
            int offset = 0;
            
            // STX marker
            frameBuffer[offset++] = TcpServer.START_MARKER;
            
            // Message length (4 bytes)
            Buffer.BlockCopy(lengthBytes, 0, frameBuffer, offset, 4);
            offset += 4;
            
            // Message content
            Buffer.BlockCopy(messageBytes, 0, frameBuffer, offset, messageBytes.Length);
            offset += messageBytes.Length;
            
            // ETX marker
            frameBuffer[offset] = TcpServer.END_MARKER;
            
            // Log the frame details for debugging
            if (message.Length > 500)
            {
                Debug.Log($"[TCP Server] Message content (truncated): {message.Substring(0, 100)}... (total: {message.Length} bytes)");
            }
            else
            {
                Debug.Log($"[TCP Server] Message content: {message} (total: {message.Length} bytes)");
            }
            
            Debug.Log($"[TCP Server] Frame includes STX (0x{TcpServer.START_MARKER:X2}) at start and ETX (0x{TcpServer.END_MARKER:X2}) at end");
            
            // Write the entire frame as a single atomic operation with timeout
            NetworkStream stream = Client.GetStream();
            
            try
            {
                // Create a cancellation token with timeout to avoid blocking forever
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                {
                    // Write with timeout
                    var writeTask = stream.WriteAsync(frameBuffer, 0, totalLength, timeoutCts.Token);
                    await writeTask;
                    
                    // Flush stream with timeout
                    var flushTask = stream.FlushAsync(timeoutCts.Token);
                    await flushTask;
                    
                    Debug.Log("[TCP Server] Message frame sent successfully");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[TCP Server] Send operation timed out");
                throw new TimeoutException("Send operation timed out");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP Server] Error sending frame: {ex.Message}");
                throw;
            }   
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