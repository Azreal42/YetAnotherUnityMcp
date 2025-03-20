using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using YetAnotherUnityMcp.Editor.WebSocket;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Debug window for monitoring MCP performance and connections
    /// </summary>
    public class MCPDebugWindow : EditorWindow
    {
        private bool _showPerformanceMetrics = true;
        private bool _showConnectionStatus = true;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private float _refreshInterval = 2.0f; // Refresh every 2 seconds
        
        private Vector2 _scrollPosition;
        
        [MenuItem("Window/YetAnotherUnityMcp/Debug Window")]
        public static void ShowWindow()
        {
            MCPDebugWindow window = GetWindow<MCPDebugWindow>("MCP Debug");
            window.Show();
        }
        
        private void OnEnable()
        {
            _lastRefreshTime = Time.realtimeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            if (_autoRefresh)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - _lastRefreshTime >= _refreshInterval)
                {
                    _lastRefreshTime = currentTime;
                    Repaint();
                }
            }
        }
        
        private void OnGUI()
        {
            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MCP Debug Window", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Connection status
            _showConnectionStatus = EditorGUILayout.Foldout(_showConnectionStatus, "Connection Status", true);
            if (_showConnectionStatus)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                bool isConnected = MCPConnection.IsConnected;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Connection Status:", GUILayout.Width(150));
                
                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                statusStyle.normal.textColor = isConnected ? Color.green : Color.red;
                EditorGUILayout.LabelField(isConnected ? "Connected" : "Disconnected", statusStyle);
                EditorGUILayout.EndHorizontal();
                
                if (isConnected)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Server URL:", GUILayout.Width(150));
                    EditorGUILayout.LabelField(MCPConnection.ServerUrl);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Server URL:", GUILayout.Width(150));
                    string newServerUrl = EditorGUILayout.TextField(MCPConnection.ServerUrl);
                    if (newServerUrl != MCPConnection.ServerUrl)
                    {
                        // Update server URL in MCPConnection
                        MCPConnection.Initialize(newServerUrl, MCPConnection.UseLocalFallback);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Use Local Fallback:", GUILayout.Width(150));
                    bool newFallbackValue = EditorGUILayout.Toggle(MCPConnection.UseLocalFallback);
                    if (newFallbackValue != MCPConnection.UseLocalFallback)
                    {
                        // Update fallback setting in MCPConnection
                        MCPConnection.Initialize(MCPConnection.ServerUrl, newFallbackValue);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                if (isConnected)
                {
                    if (GUILayout.Button("Disconnect", GUILayout.Width(120)))
                    {
                        MCPConnection.Disconnect().ContinueWith(_ => { });
                    }
                }
                else
                {
                    if (GUILayout.Button("Connect", GUILayout.Width(120)))
                    {
                        MCPConnection.Connect().ContinueWith(_ => { });
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            // Performance metrics
            _showPerformanceMetrics = EditorGUILayout.Foldout(_showPerformanceMetrics, "Performance Metrics", true);
            if (_showPerformanceMetrics)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Auto Refresh:", GUILayout.Width(150));
                _autoRefresh = EditorGUILayout.Toggle(_autoRefresh);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Refresh Interval:", GUILayout.Width(150));
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.5f, 10f);
                EditorGUILayout.LabelField("seconds", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Generate Performance Report", GUILayout.Width(200)))
                {
                    WebSocketPerformanceMonitor.Instance.LogPerformanceReport();
                }
                
                if (GUILayout.Button("Clear All Metrics", GUILayout.Width(200)))
                {
                    WebSocketPerformanceMonitor.Instance.ClearMetrics();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            // Test area
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Test Commands", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Get Unity Info", GUILayout.Width(200)))
            {
                MCPConnection.GetUnityInfo().ContinueWith(t => 
                {
                    Debug.Log($"Unity Info: {t.Result}");
                });
            }
            
            if (GUILayout.Button("Take Screenshot", GUILayout.Width(200)))
            {
                MCPConnection.TakeScreenshot("EditorScreenshot.png", new Vector2Int(1920, 1080)).ContinueWith(t => 
                {
                    Debug.Log($"Screenshot Result: {t.Result}");
                });
            }
            
            if (GUILayout.Button("Execute Test Command", GUILayout.Width(200)))
            {
                string testCode = "return UnityEngine.Application.version;";
                MCPConnection.ExecuteCode(testCode).ContinueWith(t => 
                {
                    Debug.Log($"Code Execution Result: {t.Result}");
                });
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
    }
}