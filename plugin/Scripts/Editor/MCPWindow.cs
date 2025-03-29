using System.Collections;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using YetAnotherUnityMcp.Editor.Containers;
using YetAnotherUnityMcp.Editor.Net;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Static initializer for MCP TCP Server
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
                Debug.Log("[MCP Server] Unity Editor shutting down, stopping TCP server...");
                _ = MCPTcpServer.Instance.StopAsync("Server stopped by Unity Editor");
            };
            
            // Log initialization
            Debug.Log("[MCP Server] MCP TCP Server module initialized");
            
            // Initialize log monitoring system at startup
            Containers.YaumSpecificMcpContainer.InitializeLogMonitoring();
            
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
        /// Automatically start the TCP server
        /// </summary>
        private static async void AutoStartServer()
        {
            try
            {
                if (!MCPTcpServer.Instance.IsRunning)
                {
                    Debug.Log("[MCP Server] Auto-starting TCP server...");
                    bool success = await MCPTcpServer.Instance.StartAsync();
                    
                    if (success)
                    {
                        Debug.Log($"[MCP Server] TCP server auto-started on {MCPTcpServer.Instance.ServerUrl}");
                    }
                    else
                    {
                        Debug.LogWarning("[MCP Server] Failed to auto-start TCP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Server] Error auto-starting TCP server: {ex.Message}");
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
            MCPTcpServer.Instance.OnServerStarted += HandleServerStarted;
            MCPTcpServer.Instance.OnServerStopped += HandleServerStopped;
            MCPTcpServer.Instance.OnClientConnected += HandleClientConnected;
            MCPTcpServer.Instance.OnClientDisconnected += HandleClientDisconnected;
            MCPTcpServer.Instance.OnError += HandleError;
        }

        private void OnDisable()
        {
            // Unsubscribe from server events to prevent memory leaks
            MCPTcpServer.Instance.OnServerStarted -= HandleServerStarted;
            MCPTcpServer.Instance.OnServerStopped -= HandleServerStopped;
            MCPTcpServer.Instance.OnClientConnected -= HandleClientConnected;
            MCPTcpServer.Instance.OnClientDisconnected -= HandleClientDisconnected;
            MCPTcpServer.Instance.OnError -= HandleError;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity MCP Server", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            bool isRunning = MCPTcpServer.Instance.IsRunning;

            // Server status and controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Server Status:", isRunning ? 
                $"Running on {MCPTcpServer.Instance.ServerUrl}" : 
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
                bool success = await MCPTcpServer.Instance.StartAsync(hostname, port);
                if (success)
                {
                    lastResponse = $"Server started on {MCPTcpServer.Instance.ServerUrl}";
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
                connectedClients.Clear();
                await MCPTcpServer.Instance.StopAsync("Server stopped by user");
                lastResponse = "Server stopped";
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
            lastResponse = $"Server started on {MCPTcpServer.Instance.ServerUrl}";
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
                var result = Commands.EditorMcpContainer.ExecuteCode(codeToExecute);
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
                var result = Commands.EditorMcpContainer.TakeScreenshot(screenshotResolution.x, screenshotResolution.y);
                lastResponse = $"Screenshot saved: {result}";
                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = $"Error taking screenshot: {ex.Message}";
            }
            
            Repaint();
        }
        
        private void GetLogs()
        {
            try
            {
                var result = YaumSpecificMcpContainer.GetLogs(100);
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
                var result = YaumSpecificMcpContainer.GetUnityInfo();
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