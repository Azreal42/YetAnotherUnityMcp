using System.Collections;
using UnityEditor;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Commands;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Editor window for MCP controls
    /// </summary>
    public class MCPWindow : EditorWindow
    {
        private string serverUrl = "http://localhost:8000";
        private string codeToExecute = "Debug.Log(\"Hello from MCP!\");";
        private string screenshotPath = "Assets/screenshot.png";
        private Vector2Int screenshotResolution = new Vector2Int(1920, 1080);
        private string objectId = "MainCamera";
        private string propertyName = "position.x";
        private string propertyValue = "10";

        private bool isConnected = false;
        private string lastResponse = "";
        private Vector2 scrollPosition;

        [MenuItem("Window/MCP Client")]
        public static void ShowWindow()
        {
            MCPWindow window = GetWindow<MCPWindow>("MCP Client");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity MCP Client", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawServerSettings();
            EditorGUILayout.Space();

            DrawConnectionStatus();
            EditorGUILayout.Space();

            DrawActions();
            EditorGUILayout.Space();

            DrawResponsePanel();
        }

        private void DrawServerSettings()
        {
            EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
            serverUrl = EditorGUILayout.TextField("Server URL", serverUrl);
        }

        private void DrawConnectionStatus()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Connection Status:", isConnected ? "Connected" : "Disconnected");
            
            if (GUILayout.Button(isConnected ? "Disconnect" : "Connect"))
            {
                if (!isConnected)
                {
                    Connect();
                }
                else
                {
                    isConnected = false;
                    lastResponse = "Disconnected from server";
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            // Execute code section
            EditorGUILayout.LabelField("Execute Code");
            codeToExecute = EditorGUILayout.TextArea(codeToExecute, GUILayout.Height(100));
            if (GUILayout.Button("Execute"))
            {
                ExecuteCode();
            }
            
            EditorGUILayout.Space();
            
            // Screenshot section
            EditorGUILayout.LabelField("Take Screenshot");
            screenshotPath = EditorGUILayout.TextField("Output Path", screenshotPath);
            screenshotResolution = EditorGUILayout.Vector2IntField("Resolution", screenshotResolution);
            if (GUILayout.Button("Take Screenshot"))
            {
                TakeScreenshot();
            }
            
            EditorGUILayout.Space();
            
            // Modify object section
            EditorGUILayout.LabelField("Modify Object");
            objectId = EditorGUILayout.TextField("Object ID", objectId);
            propertyName = EditorGUILayout.TextField("Property Name", propertyName);
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

        private void DrawResponsePanel()
        {
            EditorGUILayout.LabelField("Response", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            EditorGUILayout.TextArea(lastResponse);
            EditorGUILayout.EndScrollView();
        }

        private async void Connect()
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                lastResponse = "Server URL cannot be empty";
                return;
            }

            lastResponse = "Connecting to server...";
            
            // Initialize the connection with WebSocket
            MCPConnection.Initialize(serverUrl, true); // Use true to enable local fallback if connection fails
            
            // Connect to the server
            bool success = await MCPConnection.Connect();
            
            isConnected = success;
            if (success)
            {
                lastResponse = "Connected to server";
            }
            else
            {
                lastResponse = "Failed to connect to server";
            }
            
            // Force UI update
            Repaint();
        }

        private async void ExecuteCode()
        {
            if (!isConnected)
            {
                lastResponse = "Not connected to server. Please connect first.";
                return;
            }

            lastResponse = "Executing code...";
            
            // Execute code through the MCPConnection
            lastResponse = await MCPConnection.ExecuteCode(codeToExecute);
            
            // Force UI update
            Repaint();
        }

        private async void TakeScreenshot()
        {
            if (!isConnected)
            {
                lastResponse = "Not connected to server. Please connect first.";
                return;
            }

            lastResponse = "Taking screenshot...";
            
            // Take screenshot through the MCPConnection
            lastResponse = await MCPConnection.TakeScreenshot(screenshotPath, screenshotResolution);
            
            // Force UI update
            Repaint();
        }

        private async void ModifyObject()
        {
            if (!isConnected)
            {
                lastResponse = "Not connected to server. Please connect first.";
                return;
            }

            lastResponse = "Modifying object...";
            
            // Try to parse property value as float first
            float floatValue;
            object value = propertyValue;
            
            if (float.TryParse(propertyValue, out floatValue))
            {
                value = floatValue;
            }
            
            // Modify object through the MCPConnection
            lastResponse = await MCPConnection.ModifyObject(objectId, propertyName, value);
            
            // Force UI update
            Repaint();
        }

        private async void GetLogs()
        {
            if (!isConnected)
            {
                lastResponse = "Not connected to server. Please connect first.";
                return;
            }

            lastResponse = "Getting logs...";
            
            // Get logs through the MCPConnection
            lastResponse = await MCPConnection.GetLogs();
            
            // Force UI update
            Repaint();
        }

        private async void GetUnityInfo()
        {
            if (!isConnected)
            {
                lastResponse = "Not connected to server. Please connect first.";
                return;
            }

            lastResponse = "Getting Unity info...";
            
            // Get Unity info through the MCPConnection
            lastResponse = await MCPConnection.GetUnityInfo();
            
            // Force UI update
            Repaint();
        }
    }
}