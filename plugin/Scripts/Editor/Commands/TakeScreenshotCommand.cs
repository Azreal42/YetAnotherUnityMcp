using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Command to take a screenshot of the Unity Editor
    /// </summary>
    [MCPTool("take_screenshot", "Take a screenshot of the Unity Editor", "take_screenshot(output_path=\"screenshot.png\", width=1920, height=1080)")]
    public static class TakeScreenshotCommand
    {
        /// <summary>
        /// Take a screenshot of the currently active Editor view
        /// </summary>
        /// <param name="outputPath">Path where to save the screenshot</param>
        /// <param name="width">Width of the screenshot (only used for superSize calculation)</param>
        /// <param name="height">Height of the screenshot (only used for superSize calculation)</param>
        /// <returns>Result message indicating success or failure</returns>
        public static string Execute(
            [MCPParameter("output_path", "Path where to save the screenshot", "string", false)] string outputPath,
            [MCPParameter("width", "Width of the screenshot", "number", false)] int width = 1920,
            [MCPParameter("height", "Height of the screenshot", "number", false)] int height = 1080)
        {
            try
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Calculate superSize (Application.CaptureScreenshot supports superSize for higher resolution)
                // Default game view is typically around 1280x720, so calculate the multiplier
                // to get the desired resolution
                int superSize = 1;
                
                // Get the game view size
                System.Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                
                if (gameView != null)
                {
                    // Get the game view size
                    Rect gameViewRect = gameView.position;
                    float gameViewWidth = gameViewRect.width;
                    float gameViewHeight = gameViewRect.height;
                    
                    // Calculate the superSize based on the target resolution
                    float widthRatio = width / gameViewWidth;
                    float heightRatio = height / gameViewHeight;
                    superSize = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(widthRatio, heightRatio)));
                    
                    // Cap the superSize at 8 (Unity's typical upper limit)
                    superSize = Mathf.Min(8, superSize);
                }
                
                // Focus the game view before taking the screenshot
                if (gameView != null)
                {
                    gameView.Focus();
                }

                // Delay one frame to ensure the game view is properly focused
                EditorApplication.delayCall += () =>
                {
                    // Take the screenshot using Application.CaptureScreenshot
                    // This will capture the game view
                    ScreenCapture.CaptureScreenshot(outputPath, superSize);
                    
                    Debug.Log($"Screenshot saved to {outputPath} with superSize {superSize}");
                };
                
                return $"Screenshot will be saved to {outputPath}";
            }
            catch (Exception ex)
            {
                return $"Error taking screenshot: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }
    }
}