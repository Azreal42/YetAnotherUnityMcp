using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace YetAnotherUnityMcp.Editor.WebSocket
{
    /// <summary>
    /// WebSocket-based MCP window for real-time communication with the server
    /// </summary>
    public class WebSocketMCPWindow : EditorWindow
    {
        private string _serverUrl = "ws://localhost:8000/ws";
        private string _code = "Debug.Log(\"Hello from WebSocket MCP!\");";
        private Vector2 _codeScrollPosition;
        private string _result = "";
        private Vector2 _resultScrollPosition;
        private string _screenshotPath = "Assets/screenshot.png";
        private int _screenshotWidth = 1920;
        private int _screenshotHeight = 1080;
        private string _objectId = "Main Camera";
        private string _propertyPath = "position.x";
        private string _propertyValue = "0";
        private bool _isConnected = false;
        
        private MCPWebSocketManager _wsManager;

        [MenuItem("Window/WebSocket MCP Client")]
        public static void ShowWindow()
        {
            GetWindow<WebSocketMCPWindow>("WebSocket MCP");
        }

        private void OnEnable()
        {
            _wsManager = MCPWebSocketManager.Instance;
            
            // Subscribe to events
            _wsManager.OnConnected += OnWebSocketConnected;
            _wsManager.OnDisconnected += OnWebSocketDisconnected;
            _wsManager.OnMessageReceived += OnWebSocketMessageReceived;
            _wsManager.OnError += OnWebSocketError;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (_wsManager != null)
            {
                _wsManager.OnConnected -= OnWebSocketConnected;
                _wsManager.OnDisconnected -= OnWebSocketDisconnected;
                _wsManager.OnMessageReceived -= OnWebSocketMessageReceived;
                _wsManager.OnError -= OnWebSocketError;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("WebSocket MCP Client", EditorStyles.boldLabel);
            
            DrawServerSection();
            DrawExecuteCodeSection();
            DrawScreenshotSection();
            DrawModifyObjectSection();
            DrawInfoSection();
        }

        private void DrawServerSection()
        {
            GUILayout.Label("Server Connection", EditorStyles.boldLabel);
            
            _serverUrl = EditorGUILayout.TextField("Server URL", _serverUrl);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_isConnected ? "Disconnect" : "Connect"))
            {
                if (_isConnected)
                {
                    DisconnectFromServer();
                }
                else
                {
                    ConnectToServer();
                }
            }
            
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawExecuteCodeSection()
        {
            GUILayout.Label("Execute Code", EditorStyles.boldLabel);
            
            GUILayout.Label("C# Code:");
            _codeScrollPosition = EditorGUILayout.BeginScrollView(_codeScrollPosition, GUILayout.Height(150));
            _code = EditorGUILayout.TextArea(_code, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Execute"))
            {
                ExecuteCode();
            }
            
            EditorGUILayout.Space();
        }

        private void DrawScreenshotSection()
        {
            GUILayout.Label("Take Screenshot", EditorStyles.boldLabel);
            
            _screenshotPath = EditorGUILayout.TextField("Output Path", _screenshotPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Resolution:");
            _screenshotWidth = EditorGUILayout.IntField(_screenshotWidth, GUILayout.Width(60));
            GUILayout.Label("x");
            _screenshotHeight = EditorGUILayout.IntField(_screenshotHeight, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Take Screenshot"))
            {
                TakeScreenshot();
            }
            
            EditorGUILayout.Space();
        }

        private void DrawModifyObjectSection()
        {
            GUILayout.Label("Modify Object", EditorStyles.boldLabel);
            
            _objectId = EditorGUILayout.TextField("Object ID", _objectId);
            _propertyPath = EditorGUILayout.TextField("Property Path", _propertyPath);
            _propertyValue = EditorGUILayout.TextField("Property Value", _propertyValue);
            
            if (GUILayout.Button("Modify"))
            {
                ModifyObject();
            }
            
            EditorGUILayout.Space();
        }

        private void DrawInfoSection()
        {
            GUILayout.Label("Information", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Get Unity Info"))
            {
                GetUnityInfo();
            }
            
            if (GUILayout.Button("Get Logs"))
            {
                GetLogs();
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Result:");
            _resultScrollPosition = EditorGUILayout.BeginScrollView(_resultScrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(_result, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private async void ConnectToServer()
        {
            try
            {
                _result = "Connecting to server...";
                
                bool success = await _wsManager.ConnectAsync(_serverUrl);
                
                if (success)
                {
                    _isConnected = true;
                    _result = "Connected to server successfully";
                }
                else
                {
                    _result = "Failed to connect to server";
                }
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Connection error: {ex.Message}";
                Repaint();
            }
        }

        private async void DisconnectFromServer()
        {
            try
            {
                _result = "Disconnecting from server...";
                
                await _wsManager.DisconnectAsync();
                
                _isConnected = false;
                _result = "Disconnected from server";
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Disconnection error: {ex.Message}";
                Repaint();
            }
        }

        private async void ExecuteCode()
        {
            if (!_isConnected)
            {
                _result = "Not connected to server. Please connect first.";
                return;
            }

            try
            {
                _result = "Executing code...";
                
                string response = await _wsManager.ExecuteCodeAsync(_code);
                
                _result = response;
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Error executing code: {ex.Message}";
                Repaint();
            }
        }

        private async void TakeScreenshot()
        {
            if (!_isConnected)
            {
                _result = "Not connected to server. Please connect first.";
                return;
            }

            try
            {
                _result = "Taking screenshot...";
                
                string response = await _wsManager.TakeScreenshotAsync(_screenshotPath, _screenshotWidth, _screenshotHeight);
                
                _result = response;
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Error taking screenshot: {ex.Message}";
                Repaint();
            }
        }

        private async void ModifyObject()
        {
            if (!_isConnected)
            {
                _result = "Not connected to server. Please connect first.";
                return;
            }

            try
            {
                _result = "Modifying object...";
                
                // Try to parse the value as a float
                float floatValue;
                object value = _propertyValue;
                
                if (float.TryParse(_propertyValue, out floatValue))
                {
                    value = floatValue;
                }
                
                string response = await _wsManager.ModifyObjectAsync(_objectId, _propertyPath, value);
                
                _result = response;
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Error modifying object: {ex.Message}";
                Repaint();
            }
        }

        private async void GetLogs()
        {
            if (!_isConnected)
            {
                _result = "Not connected to server. Please connect first.";
                return;
            }

            try
            {
                _result = "Getting logs...";
                
                string response = await _wsManager.GetLogsAsync();
                
                _result = response;
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Error getting logs: {ex.Message}";
                Repaint();
            }
        }

        private async void GetUnityInfo()
        {
            if (!_isConnected)
            {
                _result = "Not connected to server. Please connect first.";
                return;
            }

            try
            {
                _result = "Getting Unity info...";
                
                string response = await _wsManager.GetUnityInfoAsync();
                
                _result = response;
                
                Repaint();
            }
            catch (Exception ex)
            {
                _result = $"Error getting Unity info: {ex.Message}";
                Repaint();
            }
        }

        private void OnWebSocketConnected()
        {
            _isConnected = true;
            _result = "Connected to server";
            Repaint();
        }

        private void OnWebSocketDisconnected()
        {
            _isConnected = false;
            _result = "Disconnected from server";
            Repaint();
        }

        private void OnWebSocketMessageReceived(string message)
        {
            // Handle incoming messages if needed
            Debug.Log($"WebSocket message received: {message}");
        }

        private void OnWebSocketError(string error)
        {
            _result = $"WebSocket error: {error}";
            Repaint();
        }
    }
}