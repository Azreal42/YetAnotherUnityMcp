using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using Microsoft.CSharp;
using Newtonsoft.Json;
using YetAnotherUnityMcp.Editor.Models;
using System.Collections;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Container for editor-related MCP tools and resources
    /// </summary>
    [MCPContainer("editor", "Tools and resources for interacting with the Unity Editor")]
    public static class EditorMcpContainer
    {
        #region Code Execution

        private const string ClassTemplate = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using Object = UnityEngine.Object;
{1}

namespace YetAnotherUnityMcp.Runtime
{{
    public class CodeExecutor
    {{
        public static object Execute()
        {{
            try
            {{
                {0}
            }}
            catch (Exception ex)
            {{
                return $""Error: {{ex.Message}}\nStackTrace: {{ex.StackTrace}}"";
            }}
            
            return ""Code executed successfully"";
        }}
    }}
}}";

        /// <summary>
        /// Execute C# code in the Unity Editor
        /// </summary>
        /// <param name="code">The C# code to execute</param>
        /// <returns>Result of the execution</returns>
        const string executeCodeExemple = 
            "editor_execute_code(\"Debug.Log(\\\"Hello from AI\\\"); return 42;\")\n" +
            "editor_execute_code(code=\"Debug.Log(\\\"Hello from AI\\\"); return 42;\",\n" +
            "                    additional_using=[\"Newtonsoft.Json.Linq\", \"System.IO\"],\n" +
            "                    additional_assemblies=[\"UnityEngine.PhysicsModule\", \"UnityEngine.UI\"])\n";

        [MCPTool("execute_code", "Execute C# code in Unity", "executeCodeExemple")]
        public static string ExecuteCode([MCPParameter("code", "C# code to execute in the Unity environment", "string", true)] string code,
                                         [MCPParameter("additional_using", "Additional references to add to the compilation", "string", false)] List<string> additionalUsings = null,
                                         [MCPParameter("additional_assemblies", "Additional assemblies to add to the compilation", "string", false)] List<string> additionalAssemblies = null)
        {
            try
            {
                StringBuilder result = new StringBuilder();
                // Prepare the code
                string codeToCompile = string.Format(ClassTemplate, code, additionalUsings);
                
                // Create compiler parameters
                var parameters = new System.CodeDom.Compiler.CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false
                };

            
                // Add references to Unity assemblies
                var assemblies = new List<string>() {
                    typeof(UnityEngine.Object).Assembly.Location,
                    typeof(GameObject).Assembly.Location,
                    typeof(Debug).Assembly.Location,
                    typeof(EditorWindow).Assembly.Location,
                    typeof(UnityEditor.Editor).Assembly.Location,
                    FindUnityModule("System.Core"),
                    FindUnityModule("netstandard"),
                    FindUnityModule("UnityEngine.UI"),
                    FindUnityModule("UnityEngine.CoreModule"),
                    FindUnityModule("UnityEngine.UIModule"),
                    FindUnityModule("UnityEngine.InputModule"),
                    FindUnityModule("UnityEngine.IMGUIModule"),
                };

                if (additionalAssemblies != null)
                {
                    foreach (var reference in additionalAssemblies)
                    {
                        try
                        {
                            assemblies.Add(FindUnityModule(reference));
                        }
                        catch (Exception ex)
                        {
                            result.AppendLine($"Warning: skipping reference {reference}");
                            result.AppendLine($"Error when adding it to the compilation: {ex.Message}");
                        }
                    }
                }


                assemblies = assemblies.Where(a => !string.IsNullOrEmpty(a)).Distinct().ToList();
                foreach (var assemblyPath in assemblies)
                    parameters.ReferencedAssemblies.Add(assemblyPath);
                
                // Compile the code
                using CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerResults results = provider.CompileAssemblyFromSource(parameters, codeToCompile);
                
                // Check for compilation errors
                if (results.Errors.HasErrors)
                {
                    result.AppendLine("Compilation errors occurred:");
                    foreach (CompilerError error in results.Errors)
                        result.AppendLine($"Line {error.Line}: {error.ErrorText}");
                    
                    return result.ToString();
                }
                
                // Execute the code
                Assembly assembly = results.CompiledAssembly;
                Type type = assembly.GetType("YetAnotherUnityMcp.Runtime.CodeExecutor");
                MethodInfo method = type.GetMethod("Execute");
                
                object methodResult = method.Invoke(null, null);
                if (result.Length == 0)
                    result.AppendLine("Code executed successfully:");
                result.Append($"{methodResult?.ToString() ?? "Code executed with null result"}");
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Error executing code: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }
        
        private static string FindUnityModule(string moduleName)
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == moduleName).Location;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to add Unity module {moduleName}: {ex.Message}");
                return null;
            }
        }

        #endregion
        
        #region Screenshots
        
        // Screenshot state storage for async processing
        private static Dictionary<string, Screenshot> screenshotRequests = new Dictionary<string, Screenshot>();
        
        // Class to hold screenshot processing state
        private class Screenshot
        {
            public string ID { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int SuperSize { get; set; }
            public bool IsComplete { get; set; }
            public byte[] ImageData { get; set; }
            public string Error { get; set; }
        }
        
        /// <summary>
        /// Take a screenshot of the currently active Editor view and return it as base64 encoded image
        /// </summary>
        /// <param name="width">Width of the screenshot</param>
        /// <param name="height">Height of the screenshot</param>
        /// <returns>MCP response with base64 encoded image</returns>
        [MCPTool("take_screenshot", "Take a screenshot of the Unity Editor", "editor_take_screenshot(width=1920, height=1080)", RunInSeparateThread = true)]
        public static MCPResponse TakeScreenshot(
            [MCPParameter("width", "Width of the screenshot", "number", false)] int width = 1920,
            [MCPParameter("height", "Height of the screenshot", "number", false)] int height = 1080)
        {   
            try
            {   
                string screenshotId = Guid.NewGuid().ToString();
                // Calculate superSize (ScreenCapture.CaptureScreenshot supports superSize for higher resolution)
                // Default game view is typically around 1280x720, so calculate the multiplier
                int superSize = 1;
                if (width > 1280 || height > 720)
                {
                    superSize = Math.Max(width / 1280, height / 720);
                }
                
                // Create a completion source for the coroutine to signal completion
                var completionSource = new TaskCompletionSource<byte[]>();
                
                // Create screenshot state object to track progress
                var screenshot = new Screenshot
                {
                    ID = screenshotId,
                    Width = width,
                    Height = height,
                    SuperSize = superSize,
                    IsComplete = false
                };
                
                // Store in dictionary for tracking
                screenshotRequests[screenshotId] = screenshot;
                
                Debug.Log($"Starting screenshot capture with ID {screenshotId}");
                
                // Start the coroutine on the main thread to capture screenshot
                EditorCoroutine.Start(TakeScreenshotCoroutine(screenshot, completionSource));
                
                // Wait for the task to complete with timeout
                Task<byte[]> task = completionSource.Task;
                bool completed = false;
                byte[] fileBytes = null;
                
                try
                {
                    // Wait with 10 second timeout
                    if (task.Wait(10000))
                    {
                        fileBytes = task.Result;
                        completed = true;
                    }
                }
                catch (AggregateException ex)
                {
                    // Unwrap the inner exception
                    Debug.LogError($"Error in screenshot task: {ex.InnerException?.Message}");
                    return MCPResponse.CreateErrorResponse($"Error taking screenshot: {ex.InnerException?.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error waiting for screenshot: {ex.Message}");
                    return MCPResponse.CreateErrorResponse($"Error waiting for screenshot: {ex.Message}");
                }
                finally
                {
                    // Make sure we remove from the dictionary
                    if (screenshotRequests.ContainsKey(screenshotId))
                    {
                        screenshotRequests.Remove(screenshotId);
                    }
                }
                
                // Check if the task completed
                if (!completed)
                {
                    Debug.LogError($"Screenshot task timed out after 10 seconds");
                    return MCPResponse.CreateErrorResponse("Screenshot task timed out after 10 seconds");
                }
                
                // Convert to base64
                string base64 = Convert.ToBase64String(fileBytes);
                
                // Create and return the MCP response
                MCPResponse response = MCPResponse.CreateBase64ImageResponse(
                    base64, 
                    "image/png",
                    $"Screenshot captured with dimensions {width}x{height}"
                );
                
                return response;
            }
            catch (Exception ex)
            {
                // Return error response using the helper method
                return MCPResponse.CreateErrorResponse($"Error taking screenshot: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Coroutine to take a screenshot on the main thread and handle file waiting
        /// </summary>
        private static IEnumerator TakeScreenshotCoroutine(Screenshot screenshot, TaskCompletionSource<byte[]> completionSource)
        {
            Debug.Log($"TakeScreenshotCoroutine: Taking screenshot with ID {screenshot.ID}");
            
            // Take the screenshot on the main thread
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) {
                Debug.LogError("No SceneView available to capture.");
                yield break;
            }
            EditorWindow sceneWindow = sceneView;  // SceneView inherits EditorWindow

            // Optionally ensure the scene view is focused (to bring it to front)
            sceneWindow.Focus();

            // Get window position and size in screen coordinates
            Rect windowRect = sceneWindow.position;
            Vector2 screenPos = windowRect.position;
            int width = (int)windowRect.width;
            int height = (int)windowRect.height;

            // Read the screen pixels from that region
            Color[] pixels = InternalEditorUtility.ReadScreenPixel(screenPos, width, height);
            // Copy into a Texture2D and encode to PNG
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] pngData = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            
            completionSource.SetResult(pngData);
            
            Debug.Log($"TakeScreenshotCoroutine: Screenshot with ID {screenshot.ID} completed successfully");
            yield break;
        }
        
        
        /// <summary>
        /// Simple coroutine runner for Unity Editor
        /// </summary>
        private class EditorCoroutine
        {
            public static EditorCoroutine Start(IEnumerator routine)
            {
                EditorCoroutine coroutine = new EditorCoroutine(routine);
                coroutine.Start();
                return coroutine;
            }
            
            private readonly IEnumerator routine;
            
            private EditorCoroutine(IEnumerator routine)
            {
                this.routine = routine;
            }
            
            private void Start()
            {
                EditorApplication.update += Update;
            }
            
            private void Stop()
            {
                EditorApplication.update -= Update;
            }
            
            private void Update()
            {
                if (!routine.MoveNext())
                {
                    Stop();
                }
            }
        }
        
        #endregion
        
        #region Editor Information
        
        /// <summary>
        /// Get information about the Unity Editor
        /// </summary>
        /// <returns>JSON string with editor information</returns>
        [MCPResource("info", "Get information about the Unity Editor", "unity://editor/info", "unity://editor/info")]
        public static string GetEditorInfo()
        {
            try
            {
                // Get information about the Unity Editor
                string unityVersion = Application.unityVersion;
                string productName = Application.productName;
                string dataPath = Application.dataPath;
                string persistentDataPath = Application.persistentDataPath;
                string streamingAssetsPath = Application.streamingAssetsPath;
                string temporaryCachePath = Application.temporaryCachePath;
                
                // Get information about the Unity Editor platform
                string platform = Application.platform.ToString();
                string systemLanguage = Application.systemLanguage.ToString();
                int targetFrameRate = Application.targetFrameRate;
                bool isFocused = Application.isFocused;
                bool isPlaying = EditorApplication.isPlaying;
                bool isPaused = EditorApplication.isPaused;
                
                // Format information as JSON
                string json = $@"{{
  ""unityVersion"": ""{unityVersion}"",
  ""productName"": ""{productName}"",
  ""dataPath"": ""{dataPath}"",
  ""persistentDataPath"": ""{persistentDataPath}"",
  ""streamingAssetsPath"": ""{streamingAssetsPath}"",
  ""temporaryCachePath"": ""{temporaryCachePath}"",
  ""platform"": ""{platform}"",
  ""systemLanguage"": ""{systemLanguage}"",
  ""targetFrameRate"": {targetFrameRate},
  ""isFocused"": {isFocused.ToString().ToLower()},
  ""isPlaying"": {isPlaying.ToString().ToLower()},
  ""isPaused"": {isPaused.ToString().ToLower()}
}}";
                
                return json;
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting editor info: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Get a list of available packages in the project
        /// </summary>
        /// <returns>JSON string with package information</returns>
        [MCPResource("packages", "Get information about packages in the project", "unity://editor/packages", "unity://editor/packages")]
        public static string GetPackages()
        {
            try
            {
                StringBuilder packagesJson = new StringBuilder("{\n  \"packages\": [\n");

                // Use reflection to access package list (since PackageManager API isn't directly accessible)
                Assembly editorAssembly = typeof(UnityEditor.EditorWindow).Assembly;
                Type packageInfoType = editorAssembly.GetType("UnityEditor.PackageManager.PackageInfo");
                
                if (packageInfoType != null)
                {
                    // Get the GetAllRegisteredPackages method
                    MethodInfo getAllMethod = packageInfoType.GetMethod("GetAllRegisteredPackages", 
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (getAllMethod != null)
                    {
                        // Invoke the method to get all packages
                        var packages = getAllMethod.Invoke(null, null) as System.Collections.IEnumerable;
                        
                        if (packages != null)
                        {
                            bool isFirst = true;
                            
                            foreach (var package in packages)
                            {
                                if (!isFirst)
                                {
                                    packagesJson.Append(",\n");
                                }
                                
                                // Get package properties via reflection
                                string name = GetPropertyValue<string>(package, "name") ?? "unknown";
                                string version = GetPropertyValue<string>(package, "version") ?? "unknown";
                                string displayName = GetPropertyValue<string>(package, "displayName") ?? name;
                                
                                packagesJson.Append($"    {{\n");
                                packagesJson.Append($"      \"name\": \"{name}\",\n");
                                packagesJson.Append($"      \"version\": \"{version}\",\n");
                                packagesJson.Append($"      \"displayName\": \"{displayName}\"\n");
                                packagesJson.Append($"    }}");
                                
                                isFirst = false;
                            }
                        }
                    }
                }
                
                packagesJson.Append("\n  ]\n}");
                return packagesJson.ToString();
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting packages: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Helper method to get property value via reflection
        /// </summary>
        private static T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null)
                return default;
                
            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty(propertyName);
            
            if (property == null)
                return default;
                
            return (T)property.GetValue(obj);
        }
        
        #endregion
    }
}