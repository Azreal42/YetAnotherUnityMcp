using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Containers
{
    /// <summary>
    /// MCP Container for scene-related functionality
    /// </summary>
    [MCPContainer("scene", "Scene-related tools and resources")]
    public static class SceneMcpContainer
    {
        /// <summary>
        /// Get information about the active scene
        /// </summary>
        /// <returns>JSON string with scene information</returns>
        [MCPResource("active_scene", "Get information about the active scene", "unity://scene/active", "unity://scene/active")]
        public static string GetActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            return FormatSceneInfo(activeScene);
        }
        
        /// <summary>
        /// Get information about a specific scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to get information about</param>
        /// <returns>JSON string with scene information</returns>
        [MCPResource("scene_by_name", "Get information about a specific scene", "unity://scene/{scene_name}", "unity://scene/MainScene")]
        public static string GetSceneByName(
            [MCPParameter("scene_name", "Name of the scene to get information about", "string", true)] string sceneName)
        {
            try
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid())
                {
                    return $"{{\"error\": \"Scene '{sceneName}' not found\"}}";
                }
                
                return FormatSceneInfo(scene);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting scene '{sceneName}': {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Get a list of all loaded scenes
        /// </summary>
        /// <returns>JSON string with scene list</returns>
        [MCPResource("loaded_scenes", "Get a list of all loaded scenes", "unity://scene/loaded", "unity://scene/loaded")]
        public static string GetLoadedScenes()
        {
            try
            {
                int sceneCount = SceneManager.sceneCount;
                var scenes = new List<Scene>();
                
                for (int i = 0; i < sceneCount; i++)
                {
                    scenes.Add(SceneManager.GetSceneAt(i));
                }
                
                return FormatScenesList(scenes);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting loaded scenes: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Load a scene by name
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="mode">Load mode (Single, Additive, Specified)</param>
        /// <returns>Result of the operation</returns>
        [MCPTool("load_scene", "Load a scene by name", "scene_load_scene(\"MainScene\", \"Additive\")")]
        public static string LoadScene(
            [MCPParameter("scene_name", "Name of the scene to load", "string", true)] string sceneName,
            [MCPParameter("mode", "Load mode (Single, Additive)", "string", false)] string mode = "Single")
        {
            try
            {
                LoadSceneMode loadMode;
                if (mode.Equals("Additive", StringComparison.OrdinalIgnoreCase))
                {
                    loadMode = LoadSceneMode.Additive;
                }
                else
                {
                    loadMode = LoadSceneMode.Single;
                }
                
                // Check if the scene exists in build settings
                bool sceneExists = false;
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                    if (name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                    {
                        sceneExists = true;
                        break;
                    }
                }
                
                if (!sceneExists)
                {
                    return $"{{\"error\": \"Scene '{sceneName}' not found in build settings\"}}";
                }
                
                // Load the scene asynchronously
                var asyncOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(sceneName, new LoadSceneParameters(loadMode));
                if (asyncOperation == null)
                {
                    return $"{{\"error\": \"Failed to load scene '{sceneName}'\"}}";
                }
                
                return $"{{\"result\": \"Loading scene '{sceneName}' with mode {mode}\"}}";
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error loading scene '{sceneName}': {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Get a list of all GameObjects in the active scene
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive GameObjects</param>
        /// <returns>JSON string with GameObject list</returns>
        [MCPResource("scene_objects", "Get a list of all GameObjects in the active scene", 
                   "unity://scene/objects?include_inactive={include_inactive}", 
                   "unity://scene/objects?include_inactive=true")]
        public static string GetSceneObjects(
            [MCPParameter("include_inactive", "Whether to include inactive GameObjects", "boolean", false)] bool includeInactive = false)
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                var allObjects = new List<GameObject>();
                
                // Start with root objects
                allObjects.AddRange(rootObjects);
                
                // Add all children recursively
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    AddChildrenRecursively(rootObjects[i].transform, allObjects, includeInactive);
                }
                
                return FormatGameObjectsList(allObjects);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting scene objects: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Helper method to add all children of a transform recursively
        /// </summary>
        private static void AddChildrenRecursively(Transform parent, List<GameObject> objects, bool includeInactive)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                
                if (includeInactive || child.gameObject.activeSelf)
                {
                    objects.Add(child.gameObject);
                    AddChildrenRecursively(child, objects, includeInactive);
                }
            }
        }
        
        /// <summary>
        /// Format scene information as JSON
        /// </summary>
        private static string FormatSceneInfo(Scene scene)
        {
            return $@"{{
  ""name"": ""{scene.name}"",
  ""path"": ""{scene.path}"",
  ""isLoaded"": {scene.isLoaded.ToString().ToLower()},
  ""isDirty"": {scene.isDirty.ToString().ToLower()},
  ""rootGameObjectCount"": {scene.rootCount},
  ""buildIndex"": {scene.buildIndex},
  ""isActive"": {(scene == SceneManager.GetActiveScene()).ToString().ToLower()}
}}";
        }
        
        /// <summary>
        /// Format a list of scenes as JSON
        /// </summary>
        private static string FormatScenesList(List<Scene> scenes)
        {
            string result = "{\n  \"scenes\": [";
            
            for (int i = 0; i < scenes.Count; i++)
            {
                Scene scene = scenes[i];
                result += $@"
    {{
      ""name"": ""{scene.name}"",
      ""path"": ""{scene.path}"",
      ""isLoaded"": {scene.isLoaded.ToString().ToLower()},
      ""isDirty"": {scene.isDirty.ToString().ToLower()},
      ""rootGameObjectCount"": {scene.rootCount},
      ""buildIndex"": {scene.buildIndex},
      ""isActive"": {(scene == SceneManager.GetActiveScene()).ToString().ToLower()}
    }}";
                
                if (i < scenes.Count - 1)
                {
                    result += ",";
                }
            }
            
            result += "\n  ]\n}";
            return result;
        }
        
        /// <summary>
        /// Format a list of GameObjects as JSON
        /// </summary>
        private static string FormatGameObjectsList(List<GameObject> objects)
        {
            string result = "{\n  \"objects\": [";
            
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject obj = objects[i];
                result += $@"
    {{
      ""name"": ""{obj.name}"",
      ""id"": {obj.GetInstanceID()},
      ""active"": {obj.activeSelf.ToString().ToLower()},
      ""tag"": ""{obj.tag}"",
      ""layer"": ""{LayerMask.LayerToName(obj.layer)}"",
      ""position"": {{
        ""x"": {obj.transform.position.x},
        ""y"": {obj.transform.position.y},
        ""z"": {obj.transform.position.z}
      }},
      ""rotation"": {{
        ""x"": {obj.transform.rotation.eulerAngles.x},
        ""y"": {obj.transform.rotation.eulerAngles.y},
        ""z"": {obj.transform.rotation.eulerAngles.z}
      }},
      ""scale"": {{
        ""x"": {obj.transform.localScale.x},
        ""y"": {obj.transform.localScale.y},
        ""z"": {obj.transform.localScale.z}
      }},
      ""componentCount"": {obj.GetComponents<Component>().Length}
    }}";
                
                if (i < objects.Count - 1)
                {
                    result += ",";
                }
            }
            
            result += "\n  ]\n}";
            return result;
        }
    }
}