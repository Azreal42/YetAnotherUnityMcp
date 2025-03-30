using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Helper class for invoking MCP resources
    /// </summary>
    public static class ResourceInvoker
    {
        /// <summary>
        /// Invoke a resource by name
        /// </summary>
        /// <param name="resourceName">Name of the resource to invoke</param>
        /// <param name="parameters">Parameters for the resource</param>
        /// <returns>Result of the resource invocation</returns>
        /// <exception cref="ArgumentException">Thrown if resource not found or parameters invalid</exception>
        public static object InvokeResource(string resourceName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentException("Resource name cannot be null or empty");
            }
            
            var processedParams = parameters ?? new Dictionary<string, object>();
            
            Debug.Log($"[ResourceInvoker] Invoking resource: {resourceName} with parameters: {JsonConvert.SerializeObject(processedParams)}");
            
            // Find the resource in the registry
            var registry = MCPRegistry.Instance;
            var resourceDescriptor = registry.GetResourceByName(resourceName);
            
            if (resourceDescriptor == null)
            {
                throw new ArgumentException($"Resource not found in registry: {resourceName}");
            }
            
            // Check if this is a method-based resource
            if (resourceDescriptor.MethodInfo == null || resourceDescriptor.ContainerType == null)
            {
                throw new ArgumentException($"Resource {resourceName} is not a method-based resource");
            }

            Debug.Log($"[ResourceInvoker] Using container method for resource: {resourceName}");
            
            // Get the method info
            var methodInfo = resourceDescriptor.MethodInfo;
            var containerType = resourceDescriptor.ContainerType;
            
            // Map parameters
            var containerMethodParams = methodInfo.GetParameters();
            var containerArgs = MapParameters(containerMethodParams, processedParams);
            
            // Invoke the method
            Debug.Log($"[ResourceInvoker] Invoking {containerType.Name}.{methodInfo.Name}");
            object result = methodInfo.Invoke(null, containerArgs);
            Debug.Log($"[ResourceInvoker] Resource {resourceName} invoked successfully");
            
            return result;
        }
        
        /// <summary>
        /// Map dictionary parameters to method parameters
        /// </summary>
        /// <param name="methodParams">Method parameters</param>
        /// <param name="parameters">Parameter dictionary</param>
        /// <returns>Mapped parameter array</returns>
        /// <exception cref="ArgumentException">Thrown if required parameter is missing or parameter cannot be converted</exception>
        internal static object[] MapParameters(ParameterInfo[] methodParams, Dictionary<string, object> parameters)
        {
            var args = new object[methodParams.Length];
            
            Dictionary<string, object> effectiveParams = parameters;
            
            for (int i = 0; i < methodParams.Length; i++)
            {
                var paramInfo = methodParams[i];
                string paramName = paramInfo.Name;

                var mcpParameterAttribute = paramInfo.GetCustomAttributes(typeof(MCPParameterAttribute), false).FirstOrDefault() as MCPParameterAttribute;
                if (mcpParameterAttribute != null)
                    paramName = mcpParameterAttribute.Name;
                
                if (effectiveParams.TryGetValue(paramName, out object paramValue) && paramValue != null)
                {
                    // Handle JObject conversion first
                    if (paramValue is Newtonsoft.Json.Linq.JObject jObject)
                    {
                        // If we need a dictionary, convert JObject to dictionary
                        if (paramInfo.ParameterType == typeof(Dictionary<string, object>))
                        {
                            args[i] = jObject.ToObject<Dictionary<string, object>>();
                            Debug.Log($"Converted JObject parameter {paramName} to Dictionary<string, object>");
                        }
                        else
                        {
                            // Otherwise try to convert to the target type
                            try
                            {
                                args[i] = jObject.ToObject(paramInfo.ParameterType);
                                Debug.Log($"Converted JObject parameter {paramName} to {paramInfo.ParameterType.Name}");
                            }
                            catch (Exception ex)
                            {
                                throw new ArgumentException($"Cannot convert JObject parameter {paramName} to type {paramInfo.ParameterType.Name}: {ex.Message}");
                            }
                        }
                    }
                    // Convert parameter value if needed
                    else if (paramValue != null && paramInfo.ParameterType != paramValue.GetType())
                    {
                        try
                        {
                            args[i] = Convert.ChangeType(paramValue, paramInfo.ParameterType);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"Cannot convert parameter {paramName} to type {paramInfo.ParameterType.Name}: {ex.Message}");
                        }
                    }
                    else
                    {
                        args[i] = paramValue;
                    }
                }
                else if (paramInfo.HasDefaultValue && paramValue == null)
                {
                    args[i] = paramInfo.DefaultValue;
                }
                else if (paramValue == null)
                {
                    args[i] = null;
                }
                else
                {
                    // Missing required parameter
                    throw new ArgumentException($"Required parameter {paramName} not provided");
                }
            }
            
            return args;
        }
    }
    
    /// <summary>
    /// Helper class for invoking MCP tools
    /// </summary>
    public static class ToolInvoker
    {
        /// <summary>
        /// Invoke a tool by name
        /// </summary>
        /// <param name="toolName">Name of the tool to invoke</param>
        /// <param name="parameters">Parameters for the tool</param>
        /// <returns>Result of the tool invocation</returns>
        /// <exception cref="ArgumentException">Thrown if tool not found or parameters invalid</exception>
        public static object InvokeTool(ToolDescriptor toolDescriptor, Dictionary<string, object> parameters)
        {            
            var processedParams = parameters ?? new Dictionary<string, object>();
            
            // Check if this is a method-based tool
            if (toolDescriptor.MethodInfo == null || toolDescriptor.ContainerType == null)
            {
                throw new ArgumentException($"Tool {toolDescriptor.Name} is not a method-based tool");
            }

            Debug.Log($"[ToolInvoker] Using container method for tool: {toolDescriptor.Name}");
            
            // Get the method info
            var methodInfo = toolDescriptor.MethodInfo;
            var containerType = toolDescriptor.ContainerType;
            
            // Map parameters
            var containerToolParams = methodInfo.GetParameters();
            var containerArgs = ResourceInvoker.MapParameters(containerToolParams, processedParams);
            
            
            // Invoke the method
            Debug.Log($"[ToolInvoker] Invoking {containerType.Name}.{methodInfo.Name}");
            object result = methodInfo.Invoke(null, containerArgs);
            Debug.Log($"[ToolInvoker] Tool {toolDescriptor.Name} invoked successfully");
            
            return result;
        }
    }
}