using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Container for Unity GameObject-related MCP tools and resources
    /// </summary>
    [MCPContainer("object", "Tools and resources for interacting with Unity GameObjects")]
    public static class ObjectMcpContainer
    {
        #region Object Modification

        /// <summary>
        /// Modify a property of a GameObject or its components
        /// </summary>
        /// <param name="objectId">The name or ID of the GameObject</param>
        /// <param name="propertyPath">Path to the property (e.g. "position.x" or "GetComponent<Renderer>().material.color")</param>
        /// <param name="propertyValue">The new value for the property</param>
        /// <returns>Result message indicating success or failure</returns>
        [MCPTool("modify", "Modify a property of a Unity GameObject", "object_modify(object_id=\"Main Camera\", property_path=\"transform.position.x\", property_value=10)")]
        public static string ModifyObject(
            [MCPParameter("object_id", "ID or path of the GameObject", "string", true)] string objectId,
            [MCPParameter("property_path", "Path to the property to modify", "string", true)] string propertyPath,
            [MCPParameter("property_value", "New value for the property", "any", true)] object propertyValue)
        {
            try
            {
                // Find the GameObject in the scene
                GameObject targetObject = GameObject.Find(objectId);
                if (targetObject == null)
                {
                    return $"Error: GameObject '{objectId}' not found";
                }

                // Support for special property paths
                if (propertyPath.StartsWith("position."))
                {
                    return ModifyTransformProperty(targetObject.transform, "position", propertyPath.Substring(9), propertyValue);
                }
                else if (propertyPath.StartsWith("rotation."))
                {
                    return ModifyTransformProperty(targetObject.transform, "rotation", propertyPath.Substring(9), propertyValue);
                }
                else if (propertyPath.StartsWith("scale.") || propertyPath.StartsWith("localScale."))
                {
                    return ModifyTransformProperty(targetObject.transform, "localScale", propertyPath.Contains("scale.") ? propertyPath.Substring(6) : propertyPath.Substring(11), propertyValue);
                }
                else if (propertyPath.StartsWith("GetComponent<") && propertyPath.Contains(">."))
                {
                    int startIndex = propertyPath.IndexOf('<') + 1;
                    int endIndex = propertyPath.IndexOf('>');
                    string componentTypeName = propertyPath.Substring(startIndex, endIndex - startIndex);
                    string remainingPath = propertyPath.Substring(endIndex + 2); // Skip the ">."

                    return ModifyComponentProperty(targetObject, componentTypeName, remainingPath, propertyValue);
                }
                else
                {
                    // Try to set a property directly on the GameObject or its transform
                    PropertyInfo property = targetObject.GetType().GetProperty(propertyPath);
                    if (property != null)
                    {
                        property.SetValue(targetObject, Convert.ChangeType(propertyValue, property.PropertyType));
                        return $"Modified '{propertyPath}' on GameObject '{objectId}'";
                    }

                    // Try transform properties
                    property = targetObject.transform.GetType().GetProperty(propertyPath);
                    if (property != null)
                    {
                        property.SetValue(targetObject.transform, Convert.ChangeType(propertyValue, property.PropertyType));
                        return $"Modified '{propertyPath}' on Transform of '{objectId}'";
                    }

                    return $"Error: Property '{propertyPath}' not found on GameObject '{objectId}'";
                }
            }
            catch (Exception ex)
            {
                return $"Error modifying object: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }

        private static string ModifyTransformProperty(Transform transform, string propertyName, string componentName, object value)
        {
            try
            {
                // Get the current vector value
                Vector3 vectorValue = Vector3.zero;
                if (propertyName == "position")
                {
                    vectorValue = transform.position;
                }
                else if (propertyName == "rotation")
                {
                    // Convert from euler angles for simplicity
                    vectorValue = transform.rotation.eulerAngles;
                }
                else if (propertyName == "localScale")
                {
                    vectorValue = transform.localScale;
                }

                // Modify the specific component
                float floatValue = Convert.ToSingle(value);
                if (componentName == "x")
                {
                    vectorValue.x = floatValue;
                }
                else if (componentName == "y")
                {
                    vectorValue.y = floatValue;
                }
                else if (componentName == "z")
                {
                    vectorValue.z = floatValue;
                }
                else
                {
                    return $"Error: Invalid vector component '{componentName}'";
                }

                // Set the modified vector back to the transform
                if (propertyName == "position")
                {
                    transform.position = vectorValue;
                }
                else if (propertyName == "rotation")
                {
                    transform.rotation = Quaternion.Euler(vectorValue);
                }
                else if (propertyName == "localScale")
                {
                    transform.localScale = vectorValue;
                }

                return $"Modified '{propertyName}.{componentName}' to {floatValue}";
            }
            catch (Exception ex)
            {
                return $"Error modifying transform property: {ex.Message}";
            }
        }

        private static string ModifyComponentProperty(GameObject gameObject, string componentTypeName, string propertyPath, object value)
        {
            try
            {
                // Find the component type
                Type componentType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    componentType = assembly.GetType(componentTypeName);
                    if (componentType != null)
                        break;

                    // Try with Unity namespace if not fully qualified
                    if (!componentTypeName.Contains("."))
                    {
                        componentType = assembly.GetType($"UnityEngine.{componentTypeName}");
                        if (componentType != null)
                            break;
                    }
                }

                if (componentType == null)
                {
                    return $"Error: Component type '{componentTypeName}' not found";
                }

                // Get the component from the GameObject
                Component component = gameObject.GetComponent(componentType);
                if (component == null)
                {
                    return $"Error: Component '{componentTypeName}' not found on GameObject";
                }

                // Handle nested properties (e.g. "material.color")
                string[] pathParts = propertyPath.Split('.');
                object currentObject = component;

                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    PropertyInfo property = currentObject.GetType().GetProperty(pathParts[i]);
                    if (property == null)
                    {
                        FieldInfo field = currentObject.GetType().GetField(pathParts[i]);
                        if (field == null)
                        {
                            return $"Error: Property or field '{pathParts[i]}' not found on {currentObject.GetType().Name}";
                        }
                        currentObject = field.GetValue(currentObject);
                    }
                    else
                    {
                        currentObject = property.GetValue(currentObject);
                    }

                    if (currentObject == null)
                    {
                        return $"Error: Property '{pathParts[i]}' is null";
                    }
                }

                // Set the final property
                string finalProperty = pathParts[pathParts.Length - 1];
                PropertyInfo finalPropertyInfo = currentObject.GetType().GetProperty(finalProperty);
                if (finalPropertyInfo != null)
                {
                    finalPropertyInfo.SetValue(currentObject, Convert.ChangeType(value, finalPropertyInfo.PropertyType));
                    return $"Modified '{propertyPath}' on component '{componentTypeName}'";
                }

                FieldInfo finalFieldInfo = currentObject.GetType().GetField(finalProperty);
                if (finalFieldInfo != null)
                {
                    finalFieldInfo.SetValue(currentObject, Convert.ChangeType(value, finalFieldInfo.FieldType));
                    return $"Modified '{propertyPath}' on component '{componentTypeName}'";
                }

                return $"Error: Final property '{finalProperty}' not found";
            }
            catch (Exception ex)
            {
                return $"Error modifying component property: {ex.Message}";
            }
        }

        #endregion

        #region Object Resources

        /// <summary>
        /// Get information about a specific GameObject
        /// </summary>
        /// <param name="objectId">Name or ID of the GameObject to get information about</param>
        /// <returns>JSON string with GameObject information</returns>
        [MCPResource("info", "Get information about a specific GameObject", "unity://object/{object_id}", "unity://object/Main Camera")]
        public static string GetObjectInfo(
            [MCPParameter("object_id", "Name or ID of the GameObject", "string", true)] string objectId)
        {
            try
            {
                // Find the GameObject
                GameObject obj = GameObject.Find(objectId);
                if (obj == null)
                {
                    return $"{{\"error\": \"GameObject '{objectId}' not found\"}}";
                }

                // Get basic GameObject info
                string name = obj.name;
                int instanceId = obj.GetInstanceID();
                bool isActive = obj.activeSelf;
                string tag = obj.tag;
                int layer = obj.layer;
                string layerName = LayerMask.LayerToName(layer);
                
                // Get transform info
                Vector3 position = obj.transform.position;
                Vector3 rotation = obj.transform.rotation.eulerAngles;
                Vector3 scale = obj.transform.localScale;
                
                // Get components
                Component[] components = obj.GetComponents<Component>();
                string componentsJson = "[";
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null) continue;
                    
                    string componentName = components[i].GetType().Name;
                    componentsJson += $"\"{componentName}\"";
                    
                    if (i < components.Length - 1)
                    {
                        componentsJson += ", ";
                    }
                }
                componentsJson += "]";
                
                // Format as JSON
                string json = $@"{{
  ""name"": ""{name}"",
  ""id"": {instanceId},
  ""active"": {isActive.ToString().ToLower()},
  ""tag"": ""{tag}"",
  ""layer"": {layer},
  ""layerName"": ""{layerName}"",
  ""position"": {{
    ""x"": {position.x},
    ""y"": {position.y},
    ""z"": {position.z}
  }},
  ""rotation"": {{
    ""x"": {rotation.x},
    ""y"": {rotation.y},
    ""z"": {rotation.z}
  }},
  ""scale"": {{
    ""x"": {scale.x},
    ""y"": {scale.y},
    ""z"": {scale.z}
  }},
  ""components"": {componentsJson},
  ""childCount"": {obj.transform.childCount}
}}";

                return json;
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting object info: {ex.Message}\"}}";
            }
        }

        /// <summary>
        /// Get information about a specific component on a GameObject
        /// </summary>
        /// <param name="objectId">Name or ID of the GameObject</param>
        /// <param name="componentType">Type name of the component</param>
        /// <returns>JSON string with component information</returns>
        [MCPResource("component", "Get information about a component on a GameObject", 
                    "unity://object/{object_id}/component/{component_type}", 
                    "unity://object/Main Camera/component/Camera")]
        public static string GetComponentInfo(
            [MCPParameter("object_id", "Name or ID of the GameObject", "string", true)] string objectId,
            [MCPParameter("component_type", "Type name of the component", "string", true)] string componentType)
        {
            try
            {
                // Find the GameObject
                GameObject obj = GameObject.Find(objectId);
                if (obj == null)
                {
                    return $"{{\"error\": \"GameObject '{objectId}' not found\"}}";
                }
                
                // Find the component type
                Type type = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(componentType);
                    if (type != null)
                        break;
                        
                    // Try with UnityEngine namespace if not fully qualified
                    if (!componentType.Contains("."))
                    {
                        type = assembly.GetType($"UnityEngine.{componentType}");
                        if (type != null)
                            break;
                    }
                }
                
                if (type == null)
                {
                    return $"{{\"error\": \"Component type '{componentType}' not found\"}}";
                }
                
                // Get the component
                Component component = obj.GetComponent(type);
                if (component == null)
                {
                    return $"{{\"error\": \"Component '{componentType}' not found on GameObject '{objectId}'\"}}";
                }
                
                // Build a JSON object with component properties
                string json = $@"{{
  ""type"": ""{type.FullName}"",
  ""properties"": {{";
                
                // Get public properties
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                bool first = true;
                
                foreach (var prop in properties)
                {
                    // Skip indexers and properties with parameters
                    if (prop.GetIndexParameters().Length > 0)
                        continue;
                        
                    try
                    {
                        object value = prop.GetValue(component);
                        string valueString = FormatPropertyValue(value);
                        
                        if (!first)
                            json += ",";
                            
                        json += $@"
    ""{prop.Name}"": {valueString}";
                        
                        first = false;
                    }
                    catch
                    {
                        // Skip properties that cannot be accessed
                    }
                }
                
                json += @"
  }
}";
                
                return json;
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Error getting component info: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// Format a property value as a JSON string
        /// </summary>
        private static string FormatPropertyValue(object value)
        {
            if (value == null)
                return "null";
                
            if (value is bool boolValue)
                return boolValue.ToString().ToLower();
                
            if (value is int || value is float || value is double)
                return value.ToString();
                
            if (value is string stringValue)
                return $"\"{stringValue}\"";
                
            if (value is Vector2 v2)
                return $"{{\"x\": {v2.x}, \"y\": {v2.y}}}";
                
            if (value is Vector3 v3)
                return $"{{\"x\": {v3.x}, \"y\": {v3.y}, \"z\": {v3.z}}}";
                
            if (value is Vector4 v4)
                return $"{{\"x\": {v4.x}, \"y\": {v4.y}, \"z\": {v4.z}, \"w\": {v4.w}}}";
                
            if (value is Color color)
                return $"{{\"r\": {color.r}, \"g\": {color.g}, \"b\": {color.b}, \"a\": {color.a}}}";
                
            if (value is Quaternion q)
                return $"{{\"x\": {q.x}, \"y\": {q.y}, \"z\": {q.z}, \"w\": {q.w}}}";
                
            if (value is Enum)
                return $"\"{value}\"";
                
            // For complex objects, just return a simple description
            return $"\"{value.GetType().Name}\"";
        }

        #endregion
    }
}