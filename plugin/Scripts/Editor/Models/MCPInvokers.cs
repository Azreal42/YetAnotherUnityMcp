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
            
            // Create a new dictionary to ensure we're working with a proper Dictionary<string, object>
            Dictionary<string, object> processedParams = new Dictionary<string, object>();
            
            // Process the parameters, converting JObjects if needed
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is Newtonsoft.Json.Linq.JObject jObject)
                    {
                        // Convert JObject to Dictionary<string, object>
                        processedParams[kvp.Key] = jObject.ToObject<Dictionary<string, object>>();
                        Debug.Log($"[ResourceInvoker] Converted JObject parameter '{kvp.Key}' to Dictionary");
                    }
                    else if (kvp.Value is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        // Convert JArray to List<object>
                        processedParams[kvp.Key] = jArray.ToObject<List<object>>();
                        Debug.Log($"[ResourceInvoker] Converted JArray parameter '{kvp.Key}' to List");
                    }
                    else if (kvp.Value is Newtonsoft.Json.Linq.JValue jValue)
                    {
                        // Extract the raw value from JValue
                        processedParams[kvp.Key] = jValue.Value;
                        Debug.Log($"[ResourceInvoker] Extracted value from JValue parameter '{kvp.Key}'");
                    }
                    else
                    {
                        // Keep the original value
                        processedParams[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                processedParams = new Dictionary<string, object>();
            }
            
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
            
            // Check for args/kwargs pattern
            bool hasArgs = parameters.TryGetValue("args", out object argsValue);
            bool hasKwargs = parameters.TryGetValue("kwargs", out object kwargsValue);
            
            // If using args/kwargs pattern, build a new parameters dictionary from it
            Dictionary<string, object> effectiveParams = parameters;
            
            if (hasArgs || hasKwargs)
            {
                Debug.Log($"[MapParameters] Using args/kwargs pattern");
                effectiveParams = new Dictionary<string, object>();
                
                // Process positional args first
                if (hasArgs)
                {
                    // Handle positional args
                    List<object> positionalArgs = new List<object>();
                    
                    // Handle different types of args value
                    if (argsValue is string argsStr)
                    {
                        // Single arg as string (common case)
                        positionalArgs.Add(argsStr);
                        Debug.Log($"[MapParameters] Single arg as string: '{argsStr}'");
                    }
                    else if (argsValue is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        // Array of args
                        foreach (var item in jArray)
                        {
                            positionalArgs.Add(item.ToObject<object>());
                        }
                        Debug.Log($"[MapParameters] JArray args with {positionalArgs.Count} items");
                    }
                    else if (argsValue is Newtonsoft.Json.Linq.JValue jValue)
                    {
                        // Handle JValue specifically
                        positionalArgs.Add(jValue.Value);
                        Debug.Log($"[MapParameters] JValue arg: {jValue.Value}");
                    }
                    else if (argsValue is Newtonsoft.Json.Linq.JToken jToken)
                    {
                        // Handle other JToken types
                        positionalArgs.Add(jToken.ToObject<object>());
                        Debug.Log($"[MapParameters] JToken arg: {jToken}");
                    }
                    else if (argsValue != null)
                    {
                        // Any other non-null type
                        positionalArgs.Add(argsValue);
                        Debug.Log($"[MapParameters] Other arg type: {argsValue.GetType().Name}");
                    }
                    
                    // Map positional args to method parameters in order
                    if (positionalArgs.Count > 0)
                    {
                        for (int i = 0; i < Math.Min(positionalArgs.Count, methodParams.Length); i++)
                        {
                            string paramName = methodParams[i].Name;
                            object argValue = positionalArgs[i];
                            
                            Debug.Log($"[MapParameters] Mapping arg[{i}] to param '{paramName}': {argValue}");
                            effectiveParams[paramName] = argValue;
                        }
                    }
                }
                
                // Process keyword args next (these will override positional args if there are conflicts)
                if (hasKwargs)
                {
                    Dictionary<string, object> keywordArgs = new Dictionary<string, object>();
                    
                    if (kwargsValue is Newtonsoft.Json.Linq.JObject jObject)
                    {
                        // Convert JObject to Dictionary
                        keywordArgs = jObject.ToObject<Dictionary<string, object>>();
                        Debug.Log($"[MapParameters] Mapped JObject kwargs with {keywordArgs.Count} entries");
                    }
                    else if (kwargsValue is Dictionary<string, object> dict)
                    {
                        // Already a Dictionary
                        keywordArgs = dict;
                        Debug.Log($"[MapParameters] Using Dictionary kwargs with {keywordArgs.Count} entries");
                    }
                    
                    // Add keyword args to effective parameters (overriding positional args)
                    foreach (var kvp in keywordArgs)
                    {
                        effectiveParams[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            // Now map parameters using the effective parameters (either original or from args/kwargs)
            for (int i = 0; i < methodParams.Length; i++)
            {
                var paramInfo = methodParams[i];
                string paramName = paramInfo.Name;
                
                if (effectiveParams.TryGetValue(paramName, out object paramValue))
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
            
            // Create a new dictionary to ensure we're working with a proper Dictionary<string, object>
            Dictionary<string, object> processedParams = new Dictionary<string, object>();
            
            // Process the parameters, converting JObjects if needed
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is Newtonsoft.Json.Linq.JObject jObject)
                    {
                        // Convert JObject to Dictionary<string, object>
                        processedParams[kvp.Key] = jObject.ToObject<Dictionary<string, object>>();
                        Debug.Log($"[ToolInvoker] Converted JObject parameter '{kvp.Key}' to Dictionary");
                    }
                    else if (kvp.Value is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        // Convert JArray to List<object>
                        processedParams[kvp.Key] = jArray.ToObject<List<object>>();
                        Debug.Log($"[ToolInvoker] Converted JArray parameter '{kvp.Key}' to List");
                    }
                    else if (kvp.Value is Newtonsoft.Json.Linq.JValue jValue)
                    {
                        // Extract the raw value from JValue
                        processedParams[kvp.Key] = jValue.Value;
                        Debug.Log($"[ToolInvoker] Extracted value from JValue parameter '{kvp.Key}'");
                    }
                    else
                    {
                        // Keep the original value
                        processedParams[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                processedParams = new Dictionary<string, object>();
            }
            
            Debug.Log($"[ToolInvoker] Invoking tool: {toolName} with parameters: {JsonConvert.SerializeObject(processedParams)}");
            
            // Find the tool in the registry
            var registry = MCPRegistry.Instance;
            var toolDescriptor = registry.GetToolByName(toolName);
            
            if (toolDescriptor == null)
            {
                throw new ArgumentException($"Tool not found in registry: {toolName}");
            }
            
            // Check if this is a method-based tool
            if (toolDescriptor.MethodInfo == null || toolDescriptor.ContainerType == null)
            {
                throw new ArgumentException($"Tool {toolName} is not a method-based tool");
            }

            Debug.Log($"[ToolInvoker] Using container method for tool: {toolName}");
            
            // Get the method info
            var methodInfo = toolDescriptor.MethodInfo;
            var containerType = toolDescriptor.ContainerType;
            
            // Map parameters
            var containerToolParams = methodInfo.GetParameters();
            var containerArgs = ResourceInvoker.MapParameters(containerToolParams, processedParams);
            
            
            // Invoke the method
            Debug.Log($"[ToolInvoker] Invoking {containerType.Name}.{methodInfo.Name}");
            object result = methodInfo.Invoke(null, containerArgs);
            Debug.Log($"[ToolInvoker] Tool {toolName} invoked successfully");
            
            return result;
        }
    }
}