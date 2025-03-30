using System;
using UnityEngine;
using UnityEditor;
using YetAnotherUnityMcp.Editor.Net;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Menu items for MCP TCP Server
    /// </summary>
    public static class MCPMenu
    {
        [MenuItem("MCP/Server/Start Server")]
        public static async void StartServer()
        {
            try
            {
                bool result = await MCPTcpServer.Instance.StartAsync();
                if (result)
                {
                    Debug.Log($"[MCP Menu] Server started on {MCPTcpServer.Instance.ServerUrl}");
                }
                else
                {
                    Debug.LogError("[MCP Menu] Failed to start server");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Menu] Error starting server: {ex.Message}");
            }
        }
        
        [MenuItem("MCP/Server/Start Server", true)]
        public static bool ValidateStartServer()
        {
            return !MCPTcpServer.Instance.IsRunning;
        }
        
        [MenuItem("MCP/Server/Stop Server")]
        public static async void StopServer()
        {
            try
            {
                await MCPTcpServer.Instance.StopAsync("Server stopped by user");
                Debug.Log("[MCP Menu] Server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Menu] Error stopping server: {ex.Message}");
            }
        }
        
        [MenuItem("MCP/Server/Stop Server", true)]
        public static bool ValidateStopServer()
        {
            return MCPTcpServer.Instance.IsRunning;
        }
        
        [MenuItem("MCP/Server/Show Server Window")]
        public static void ShowServerWindow()
        {
            MCPWindow.ShowWindow();
        }
        
        [MenuItem("MCP/Server/Enable Auto-start")]
        public static void EnableAutoStart()
        {
            MCPServerInitializer.AutoStartEnabled = true;
            Debug.Log("[MCP Menu] Server auto-start enabled");
        }
        
        [MenuItem("MCP/Server/Enable Auto-start", true)]
        public static bool ValidateEnableAutoStart()
        {
            return !MCPServerInitializer.AutoStartEnabled;
        }
        
        [MenuItem("MCP/Server/Disable Auto-start")]
        public static void DisableAutoStart()
        {
            MCPServerInitializer.AutoStartEnabled = false;
            Debug.Log("[MCP Menu] Server auto-start disabled");
        }
        
        [MenuItem("MCP/Server/Disable Auto-start", true)]
        public static bool ValidateDisableAutoStart()
        {
            return MCPServerInitializer.AutoStartEnabled;
        }
    }
}