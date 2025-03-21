using System;
using System.Collections.Generic;
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
            
            parameters = parameters ?? new Dictionary<string, object>();
            Debug.Log($"[ResourceInvoker] Invoking resource: {resourceName} with parameters: {JsonConvert.SerializeObject(parameters)}");
            
            // Find the resource in the registry
            var registry = MCPRegistry.Instance;
            var resourceDescriptor = registry.GetResourceByName(resourceName);
            
            if (resourceDescriptor == null)
            {
                throw new ArgumentException($"Resource not found in registry: {resourceName}");
            }
            
            // Find the resource handler type
            var handlerType = registry.GetResourceHandlerType(resourceName);
            if (handlerType == null)
            {
                throw new ArgumentException($"No handler type found for resource: {resourceName}");
            }
            
            Debug.Log($"[ResourceInvoker] Found handler type: {handlerType.Name}");
            
            // Get the GetResource method
            var getResourceMethod = handlerType.GetMethod("GetResource", BindingFlags.Public | BindingFlags.Static);
            if (getResourceMethod == null)
            {
                throw new ArgumentException($"Resource handler {handlerType.Name} does not have a GetResource method");
            }
            
            // Map parameters
            var methodParams = getResourceMethod.GetParameters();
            var args = MapParameters(methodParams, parameters);
            
            // Invoke the method
            Debug.Log($"[ResourceInvoker] Invoking {handlerType.Name}.GetResource");
            object result = getResourceMethod.Invoke(null, args);
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
        private static object[] MapParameters(ParameterInfo[] methodParams, Dictionary<string, object> parameters)
        {
            var args = new object[methodParams.Length];
            
            for (int i = 0; i < methodParams.Length; i++)
            {
                var paramInfo = methodParams[i];
                string paramName = paramInfo.Name;
                
                if (parameters.TryGetValue(paramName, out object paramValue))
                {
                    // Convert parameter value if needed
                    if (paramValue != null && paramInfo.ParameterType != paramValue.GetType())
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
                else if (paramInfo.HasDefaultValue)
                {
                    // Use default value
                    args[i] = paramInfo.DefaultValue;
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
        public static object InvokeTool(string toolName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException("Tool name cannot be null or empty");
            }
            
            parameters = parameters ?? new Dictionary<string, object>();
            Debug.Log($"[ToolInvoker] Invoking tool: {toolName} with parameters: {JsonConvert.SerializeObject(parameters)}");
            
            // Find the tool in the registry
            var registry = MCPRegistry.Instance;
            var toolDescriptor = registry.GetToolByName(toolName);
            
            if (toolDescriptor == null)
            {
                throw new ArgumentException($"Tool not found in registry: {toolName}");
            }
            
            // Find the tool handler type
            var handlerType = registry.GetToolHandlerType(toolName);
            if (handlerType == null)
            {
                throw new ArgumentException($"No handler type found for tool: {toolName}");
            }
            
            Debug.Log($"[ToolInvoker] Found handler type: {handlerType.Name}");
            
            // Get the Execute method
            var executeMethod = handlerType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            if (executeMethod == null)
            {
                throw new ArgumentException($"Tool handler {handlerType.Name} does not have an Execute method");
            }
            
            // Map parameters
            var methodParams = executeMethod.GetParameters();
            var args = ResourceInvoker.MapParameters(methodParams, parameters);
            
            // Invoke the method
            Debug.Log($"[ToolInvoker] Invoking {handlerType.Name}.Execute");
            object result = executeMethod.Invoke(null, args);
            Debug.Log($"[ToolInvoker] Tool {toolName} invoked successfully");
            
            return result;
        }
    }
}