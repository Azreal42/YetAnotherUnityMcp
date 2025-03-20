using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Command to modify a property of a Unity GameObject
    /// </summary>
    [MCPTool("modify_object", "Modify a property of a Unity GameObject", "modify_object(object_id=\"Main Camera\", property_path=\"transform.position.x\", property_value=10)")]
    public static class ModifyObjectCommand
    {
        /// <summary>
        /// Modify a property of a GameObject or its components
        /// </summary>
        /// <param name="objectId">The name or ID of the GameObject</param>
        /// <param name="propertyPath">Path to the property (e.g. "position.x" or "GetComponent<Renderer>().material.color")</param>
        /// <param name="propertyValue">The new value for the property</param>
        /// <returns>Result message indicating success or failure</returns>
        public static string Execute(
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
    }
}