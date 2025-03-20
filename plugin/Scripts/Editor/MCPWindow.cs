using System.Collections;
using UnityEditor;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Commands;
using YetAnotherUnityMcp.Editor.WebSocket;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Static initializer for MCP WebSocket Server
    /// </summary>
    [InitializeOnLoad]
    public static class MCPServerInitializer
    {
        // Preference key for auto-start setting
        private const string AUTO_START_PREF_KEY = "YetAnotherUnityMcp.AutoStartServer";
        
        // Default value for auto-start (enabled by default)
        private const bool AUTO_START_DEFAULT = true;
        
        /// <summary>
        /// Get or set whether the server should auto-start
        /// </summary>
        public static bool AutoStartEnabled
        {
            get => EditorPrefs.GetBool(AUTO_START_PREF_KEY, AUTO_START_DEFAULT);
            set => EditorPrefs.SetBool(AUTO_START_PREF_KEY, value);
        }
        
        static MCPServerInitializer()
        {
            // Set up automatic server cleanup when editor is shutting down
            EditorApplication.quitting += () => 
            {
                Debug.Log("[MCP Server] Unity Editor shutting down, stopping WebSocket server...");
                _ = WebSocket.MCPWebSocketServer.Instance.StopAsync();
            };
            
            // Log initialization
            Debug.Log("[MCP Server] MCP WebSocket Server module initialized");
            
            // Auto-start server on editor startup (if enabled)
            EditorApplication.delayCall += () => {
                // Start with a small delay to ensure Unity is fully initialized
                if (AutoStartEnabled)
                {
                    AutoStartServer();
                }
                else
                {
                    Debug.Log("[MCP Server] Auto-start is disabled. Use MCP/Server/Start Server to start manually.");
                }
            };
        }
        
        /// <summary>
        /// Automatically start the WebSocket server
        /// </summary>
        private static async void AutoStartServer()
        {
            try
            {
                if (!WebSocket.MCPWebSocketServer.Instance.IsRunning)
                {
                    Debug.Log("[MCP Server] Auto-starting WebSocket server...");
                    bool success = await WebSocket.MCPWebSocketServer.Instance.StartAsync();
                    
                    if (success)
                    {
                        Debug.Log($"[MCP Server] WebSocket server auto-started on {WebSocket.MCPWebSocketServer.Instance.ServerUrl}");
                    }
                    else
                    {
                        Debug.LogWarning("[MCP Server] Failed to auto-start WebSocket server");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Server] Error auto-starting WebSocket server: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Editor window for MCP server controls
    /// </summary>
    public class MCPWindow : EditorWindow
    {
        private string hostname = "localhost";
        private int port = 8080;
        private string codeToExecute = "Debug.Log(\"Hello from MCP!\");";
        private string screenshotPath = "Assets/screenshot.png";
        private Vector2Int screenshotResolution = new Vector2Int(1920, 1080);
        private string objectId = "MainCamera";
        private string propertyName = "position.x";
        private string propertyValue = "10";

        private string lastResponse = "";
        private Vector2 scrollPosition;
        private List<string> connectedClients = new List<string>();
        private string lastError = "";
        private bool showSettings = true;
        private bool showActions = true;
        private bool showClients = true;
        private bool showResponse = true;

        [MenuItem("Window/MCP Server")]
        public static void ShowWindow()
        {
            MCPWindow window = GetWindow<MCPWindow>("MCP Server");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Subscribe to server events
            WebSocket.MCPWebSocketServer.Instance.OnServerStarted += HandleServerStarted;
            WebSocket.MCPWebSocketServer.Instance.OnServerStopped += HandleServerStopped;
            WebSocket.MCPWebSocketServer.Instance.OnClientConnected += HandleClientConnected;
            WebSocket.MCPWebSocketServer.Instance.OnClientDisconnected += HandleClientDisconnected;
            WebSocket.MCPWebSocketServer.Instance.OnError += HandleError;
        }

        private void OnDisable()
        {
            // Unsubscribe from server events to prevent memory leaks
            WebSocket.MCPWebSocketServer.Instance.OnServerStarted -= HandleServerStarted;
            WebSocket.MCPWebSocketServer.Instance.OnServerStopped -= HandleServerStopped;
            WebSocket.MCPWebSocketServer.Instance.OnClientConnected -= HandleClientConnected;
            WebSocket.MCPWebSocketServer.Instance.OnClientDisconnected -= HandleClientDisconnected;
            WebSocket.MCPWebSocketServer.Instance.OnError -= HandleError;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity MCP Server", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            bool isRunning = WebSocket.MCPWebSocketServer.Instance.IsRunning;

            // Server status and controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Server Status:", isRunning ? 
                $"Running on {WebSocket.MCPWebSocketServer.Instance.ServerUrl}" : 
                "Stopped");
            
            if (GUILayout.Button(isRunning ? "Stop Server" : "Start Server"))
            {
                if (!isRunning)
                {
                    StartServer();
                }
                else
                {
                    StopServer();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            // Server settings
            showSettings = EditorGUILayout.Foldout(showSettings, "Server Settings", true);
            if (showSettings)
            {
                EditorGUI.BeginDisabledGroup(isRunning);
                hostname = EditorGUILayout.TextField("Hostname", hostname);
                port = EditorGUILayout.IntField("Port", port);
                EditorGUI.EndDisabledGroup();
                
                // Auto-start preference
                bool autoStart = MCPServerInitializer.AutoStartEnabled;
                bool newAutoStart = EditorGUILayout.Toggle("Auto-start on Unity Launch", autoStart);
                
                if (newAutoStart != autoStart)
                {
                    MCPServerInitializer.AutoStartEnabled = newAutoStart;
                    EditorUtility.SetDirty(this);
                }
            }
            
            EditorGUILayout.Space();
            
            // Connected clients
            showClients = EditorGUILayout.Foldout(showClients, $"Connected Clients ({connectedClients.Count})", true);
            if (showClients && connectedClients.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var clientId in connectedClients)
                {
                    EditorGUILayout.LabelField(clientId);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();

            // Local command testing interface
            showActions = EditorGUILayout.Foldout(showActions, "Local Command Testing", true);
            if (showActions)
            {
                EditorGUILayout.HelpBox("These actions can be used to test commands locally without a client connection.", MessageType.Info);
                
                // Execute code section
                EditorGUILayout.LabelField("Execute Code", EditorStyles.boldLabel);
                codeToExecute = EditorGUILayout.TextArea(codeToExecute, GUILayout.Height(100));
                if (GUILayout.Button("Execute"))
                {
                    ExecuteCode();
                }
                
                EditorGUILayout.Space();
                
                // Screenshot section
                EditorGUILayout.LabelField("Take Screenshot", EditorStyles.boldLabel);
                screenshotPath = EditorGUILayout.TextField("Output Path", screenshotPath);
                screenshotResolution = EditorGUILayout.Vector2IntField("Resolution", screenshotResolution);
                if (GUILayout.Button("Take Screenshot"))
                {
                    TakeScreenshot();
                }
                
                EditorGUILayout.Space();
                
                // Modify object section
                EditorGUILayout.LabelField("Modify Object", EditorStyles.boldLabel);
                objectId = EditorGUILayout.TextField("Object ID", objectId);
                propertyName = EditorGUILayout.TextField("Property Path", propertyName);
                propertyValue = EditorGUILayout.TextField("Property Value", propertyValue);
                if (GUILayout.Button("Modify Object"))
                {
                    ModifyObject();
                }
                
                EditorGUILayout.Space();
                
                // Get logs section
                if (GUILayout.Button("Get Logs"))
                {
                    GetLogs();
                }
                
                // Get Unity info section
                if (GUILayout.Button("Get Unity Info"))
                {
                    GetUnityInfo();
                }
            }
            
            EditorGUILayout.Space();
            
            // Response and error display
            if (!string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }
            
            showResponse = EditorGUILayout.Foldout(showResponse, "Response", true);
            if (showResponse)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
                EditorGUILayout.TextArea(lastResponse);
                EditorGUILayout.EndScrollView();
            }
            
            // Performance monitor controls
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Log Performance Stats"))
            {
                CommandExecutionMonitor.Instance.LogPerformanceReport();
            }
            if (GUILayout.Button("Clear Stats"))
            {
                CommandExecutionMonitor.Instance.ClearMetrics();
            }
            EditorGUILayout.EndHorizontal();
            
            // Force refresh the UI periodically
            if (EditorApplication.timeSinceStartup % 1 < 0.016f)
            {
                Repaint();
            }
        }

        #region Server Control Methods
        
        private async void StartServer()
        {
            lastError = "";
            lastResponse = "Starting server...";
            
            try
            {
                bool success = await WebSocket.MCPWebSocketServer.Instance.StartAsync(hostname, port);
                if (success)
                {
                    lastResponse = $"Server started on {WebSocket.MCPWebSocketServer.Instance.ServerUrl}";
                }
                else
                {
                    lastError = "Failed to start server";
                }
            }
            catch (Exception ex)
            {
                lastError = $"Error starting server: {ex.Message}";
                Debug.LogError($"[MCP Server] {lastError}");
            }
            
            Repaint();
        }
        
        private async void StopServer()
        {
            lastError = "";
            lastResponse = "Stopping server...";
            
            try
            {
                await WebSocket.MCPWebSocketServer.Instance.StopAsync();
                lastResponse = "Server stopped";
                connectedClients.Clear();
            }
            catch (Exception ex)
            {
                lastError = $"Error stopping server: {ex.Message}";
                Debug.LogError($"[MCP Server] {lastError}");
            }
            
            Repaint();
        }
        
        #endregion
        
        #region Server Event Handlers
        
        private void HandleServerStarted()
        {
            lastResponse = $"Server started on {WebSocket.MCPWebSocketServer.Instance.ServerUrl}";
            Repaint();
        }
        
        private void HandleServerStopped()
        {
            lastResponse = "Server stopped";
            connectedClients.Clear();
            Repaint();
        }
        
        private void HandleClientConnected(string clientId)
        {
            if (!connectedClients.Contains(clientId))
            {
                connectedClients.Add(clientId);
            }
            lastResponse = $"Client connected: {clientId}";
            Repaint();
        }
        
        private void HandleClientDisconnected(string clientId)
        {
            connectedClients.Remove(clientId);
            lastResponse = $"Client disconnected: {clientId}";
            Repaint();
        }
        
        private void HandleError(string error)
        {
            lastError = error;
            Repaint();
        }
        
        #endregion
        
        #region Local Command Execution Methods
        
        private void ExecuteCode()
        {
            try
            {
                var result = ExecuteCodeCommand.Execute(codeToExecute);
                lastResponse = $"Result: {result}";
                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = $"Error executing code: {ex.Message}";
            }
            
            Repaint();
        }
        
        private void TakeScreenshot()
        {
            try
            {
                var result = TakeScreenshotCommand.Execute(screenshotPath, screenshotResolution.x, screenshotResolution.y);
                lastResponse = $"Screenshot saved: {result}";
                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = $"Error taking screenshot: {ex.Message}";
            }
            
            Repaint();
        }
        
        private void ModifyObject()
        {
            try
            {
                // Try to parse property value as float first
                float floatValue;
                object value = propertyValue;
                
                if (float.TryParse(propertyValue, out floatValue))
                {
                    value = floatValue;
                }
                
                var result = ModifyObjectCommand.Execute(objectId, propertyName, value);
                lastResponse = $"Object modified: {result}";
                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = $"Error modifying object: {ex.Message}";
            }
            
            Repaint();
        }
        
        private void GetLogs()
        {
            try
            {
                var result = GetLogsCommand.Execute(100);
                lastResponse = $"Logs: {result}";
                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = $"Error getting logs: {ex.Message}";
            }
            
            Repaint();
        }
        
        private void GetUnityInfo()
        {
            try
            {
                var result = GetUnityInfoCommand.Execute();
                lastResponse = $"Unity info: {result}";
                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = $"Error getting Unity info: {ex.Message}";
            }
            
            Repaint();
        }
        
        #endregion
    }
}