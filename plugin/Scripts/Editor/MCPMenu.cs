using System;
using UnityEngine;
using UnityEditor;
using YetAnotherUnityMcp.Editor.WebSocket;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Menu items for MCP WebSocket Server
    /// </summary>
    public static class MCPMenu
    {
        [MenuItem("MCP/Server/Start Server")]
        public static async void StartServer()
        {
            try
            {
                bool result = await MCPWebSocketServer.Instance.StartAsync();
                if (result)
                {
                    Debug.Log($"[MCP Menu] Server started on {MCPWebSocketServer.Instance.ServerUrl}");
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
            return !MCPWebSocketServer.Instance.IsRunning;
        }
        
        [MenuItem("MCP/Server/Stop Server")]
        public static async void StopServer()
        {
            try
            {
                await MCPWebSocketServer.Instance.StopAsync();
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
            return MCPWebSocketServer.Instance.IsRunning;
        }
        
        [MenuItem("MCP/Server/Show Server Window")]
        public static void ShowServerWindow()
        {
            MCPWindow.ShowWindow();
        }
        
        [MenuItem("MCP/Server/Log Performance Report")]
        public static void LogPerformanceReport()
        {
            CommandExecutionMonitor.Instance.LogPerformanceReport();
        }
        
        [MenuItem("MCP/Server/Reset Performance Metrics")]
        public static void ResetPerformanceMetrics()
        {
            CommandExecutionMonitor.Instance.ClearMetrics();
            Debug.Log("[MCP Menu] Performance metrics cleared");
        }
    }
}