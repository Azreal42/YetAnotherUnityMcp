using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Tests for ResourceInvoker and ToolInvoker
    /// </summary>
    public class MCPInvokersTests
    {
        /// <summary>
        /// Test container for unit tests
        /// </summary>
        [MCPContainer("test", "Test container for unit tests")]
        public static class TestContainer
        {
            /// <summary>
            /// Test resource for unit tests
            /// </summary>
            [MCPResource("resource", "Test resource for unit tests", "unity://test", "unity://test")]
            public static string GetResource()
            {
                return "{\"result\": \"test_success\"}";
            }
            
            /// <summary>
            /// Test resource with parameters
            /// </summary>
            [MCPResource("resource_with_params", "Test resource with parameters", "unity://test/{param1}/{param2}", "unity://test/value1/value2")]
            public static string GetResourceWithParams(
                [MCPParameter("param1", "First parameter", "string", true)] string param1,
                [MCPParameter("param2", "Second parameter", "number", true)] int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
            
            /// <summary>
            /// Test resource with default parameter
            /// </summary>
            [MCPResource("resource_with_default", "Test resource with default parameter", "unity://test/default", "unity://test/default")]
            public static string GetResourceWithDefault(
                [MCPParameter("param1", "Parameter with default value", "string", false)] string param1 = "default_value")
            {
                return $"{{\"param1\": \"{param1}\"}}";
            }
            
            /// <summary>
            /// Test tool for unit tests
            /// </summary>
            [MCPTool("tool", "Test tool for unit tests", "test_tool()")]
            public static string ExecuteTool()
            {
                return "{\"result\": \"tool_success\"}";
            }
            
            /// <summary>
            /// Test tool with parameters
            /// </summary>
            [MCPTool("tool_with_params", "Test tool with parameters", "test_tool_with_params(\"value\", 42)")]
            public static string ExecuteToolWithParams(
                [MCPParameter("param1", "First parameter", "string", true)] string param1,
                [MCPParameter("param2", "Second parameter", "number", true)] int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
            
            /// <summary>
            /// Mock of the execute_code function
            /// </summary>
            [MCPTool("execute_code", "Execute C# code in Unity", "execute_code(\"Debug.Log(\\\"Hello\\\"); return 42;\")")]
            public static string ExecuteCode(
                [MCPParameter("code", "C# code to execute in the Unity environment", "string", true)] string code)
            {
                Debug.Log($"Executing code: {code}");
                return $"{{\"result\": \"Executed: {code}\"}}";
            }
        }
        
        // Legacy classes to support backward compatibility tests
        public static class TestResource
        {
            public static string GetResource()
            {
                return "{\"result\": \"test_success\"}";
            }
        }
        
        public static class TestResourceWithParams
        {
            public static string GetResource(string param1, int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }
        
        public static class TestResourceWithDefault
        {
            public static string GetResource(string param1 = "default_value")
            {
                return $"{{\"param1\": \"{param1}\"}}";
            }
        }
        
        public static class TestTool
        {
            public static string Execute()
            {
                return "{\"result\": \"tool_success\"}";
            }
        }
        
        public static class TestToolWithParams
        {
            public static string Execute(string param1, int param2)
            {
                return $"{{\"param1\": \"{param1}\", \"param2\": {param2}}}";
            }
        }
        
        /// <summary>
        /// Register test resources and tools in the registry
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var registry = MCPRegistry.Instance;
            
            // Clear the registry first to ensure clean state
            MethodInfo clearMethod = registry.GetType().GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clearMethod != null)
            {
                clearMethod.Invoke(registry, null);
            }
            
            // Register container methods
            registry.RegisterMethodsFromContainer(typeof(TestContainer));
            
            // Create and register legacy descriptors for backward compatibility
            var testResourceDescriptor = new ResourceDescriptor
            {
                Name = "test_resource",
                Description = "Test resource for unit tests",
                Uri = "unity://test",
                Example = "unity://test",
                ContainerType = typeof(TestResource),
                MethodInfo = typeof(TestResource).GetMethod("GetResource"),
                MimeType = "application/json"
            };
            registry.RegisterResource(testResourceDescriptor);
            
            var testResourceWithParamsDescriptor = new ResourceDescriptor
            {
                Name = "test_resource_with_params",
                Description = "Test resource with parameters",
                Uri = "unity://test/{param1}/{param2}",
                Example = "unity://test/value1/value2",
                ContainerType = typeof(TestResourceWithParams),
                MethodInfo = typeof(TestResourceWithParams).GetMethod("GetResource"),
                Parameters = new Dictionary<string, ParameterDescriptor>
                {
                    { "param1", new ParameterDescriptor { Description = "First parameter", Type = "string", IsRequired = true } },
                    { "param2", new ParameterDescriptor { Description = "Second parameter", Type = "number", IsRequired = true } }
                },
                MimeType = "application/json"
            };
            registry.RegisterResource(testResourceWithParamsDescriptor);
            
            var testResourceWithDefaultDescriptor = new ResourceDescriptor
            {
                Name = "test_resource_with_default",
                Description = "Test resource with default parameter",
                Uri = "unity://test/default",
                Example = "unity://test/default",
                ContainerType = typeof(TestResourceWithDefault),
                MethodInfo = typeof(TestResourceWithDefault).GetMethod("GetResource"),
                Parameters = new Dictionary<string, ParameterDescriptor>
                {
                    { "param1", new ParameterDescriptor { Description = "Parameter with default value", Type = "string", IsRequired = false } }
                },
                MimeType = "application/json"
            };
            registry.RegisterResource(testResourceWithDefaultDescriptor);
            
            var testToolDescriptor = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Test tool for unit tests",
                Example = "test_tool()",
                ContainerType = typeof(TestTool),
                MethodInfo = typeof(TestTool).GetMethod("Execute"),
                InputSchema = new InputSchema()
            };
            registry.RegisterTool(testToolDescriptor);
            
            var testToolWithParamsDescriptor = new ToolDescriptor
            {
                Name = "test_tool_with_params",
                Description = "Test tool with parameters",
                Example = "test_tool_with_params(\"value\", 42)",
                ContainerType = typeof(TestToolWithParams),
                MethodInfo = typeof(TestToolWithParams).GetMethod("Execute"),
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, ParameterDescriptor>
                    {
                        { "param1", new ParameterDescriptor { Description = "First parameter", Type = "string", IsRequired = true } },
                        { "param2", new ParameterDescriptor { Description = "Second parameter", Type = "number", IsRequired = true } }
                    },
                    Required = new List<string> { "param1", "param2" }
                }
            };
            registry.RegisterTool(testToolWithParamsDescriptor);
        }
        
        /// <summary>
        /// Test ResourceInvoker with a simple resource
        /// </summary>
        [Test]
        public void ResourceInvoker_WithSimpleResource_ReturnsResult()
        {
            // Act
            var result = ResourceInvoker.InvokeResource("test_resource", null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"test_success\"}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with parameters
        /// </summary>
        [Test]
        public void ResourceInvoker_WithParameters_PassesParametersCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };
            
            // Act
            var result = ResourceInvoker.InvokeResource("test_resource_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test ToolInvoker with a simple tool
        /// </summary>
        [Test]
        public void ToolInvoker_WithSimpleTool_ReturnsResult()
        {
            // Act
            var result = ToolInvoker.InvokeTool("test_tool", null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"result\": \"tool_success\"}", result);
        }
        
        /// <summary>
        /// Test ToolInvoker with parameters
        /// </summary>
        [Test]
        public void ToolInvoker_WithParameters_PassesParametersCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("test_tool_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Reproduce the issue with JSON parameters for execute_code
        /// </summary>
        [Test]
        public void ExecuteCode_WithJsonParameters()
        {
            // Arrange - Simulate the JSON payload sent from the TCP client
            string jsonParams = "{\"code\":\"Debug.Log(\\\"Hello from TCP test\\\"); return DateTime.Now.ToString();\"}";
            
            // Parse the JSON to dictionary the same way it would happen in the system
            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonParams);
            
            // Log the parsed parameters to see what's actually in there
            Debug.Log($"Parameters after parsing: {JsonConvert.SerializeObject(parameters)}");
            
            // Act
            var result = ToolInvoker.InvokeTool("test_execute_code", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            // We expect the code to be passed correctly despite coming from JSON
            Assert.IsTrue(result.ToString().Contains("Hello from TCP test"), 
                "Result should contain the code that was passed from JSON");
            
            // Log actual result for debugging
            Debug.Log($"Execute code result with JSON parameters: {result}");
        }
        
        /// <summary>
        /// Test the fix by reproducing the exact TCP server scenario
        /// </summary>
        [Test]
        public void TcpServer_JsonParameterParsing_ShouldWork()
        {
            // This test simulates what happens in the TCP server when receiving a JSON command
            
            // Step 1: The JSON message from client (exactly as seen in the logs)
            string jsonRequest = "{\"id\":\"req_123\",\"command\":\"test_execute_code\",\"parameters\":{\"code\":\"Debug.Log(\\\"Hello from TCP test\\\"); return DateTime.Now.ToString();\"},\"client_timestamp\":1711725076543}";
            
            // Step 2: Parse the request
            var request = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonRequest);
            
            // Step 3: Extract the parameters object
            var parametersObj = request["parameters"];
            
            // Step A: Direct conversion (what happens in the issue)
            Dictionary<string, object> parametersDirectCast = parametersObj as Dictionary<string, object>;
            
            Debug.Log($"Direct cast gave us: {(parametersDirectCast == null ? "null" : JsonConvert.SerializeObject(parametersDirectCast))}");
            
            // Step B: Convert JObject to Dictionary (what should happen)
            Dictionary<string, object> parametersJObjectConvert = null;
            if (parametersObj is JObject jObject)
            {
                parametersJObjectConvert = jObject.ToObject<Dictionary<string, object>>();
                Debug.Log($"JObject conversion gave us: {JsonConvert.SerializeObject(parametersJObjectConvert)}");
            }
            
            // Log what we actually received
            Debug.Log($"Parameters object type: {parametersObj.GetType().FullName}");
            
            // Tests with both approaches
            if (parametersDirectCast != null)
            {
                try
                {
                    var resultDirect = ToolInvoker.InvokeTool(request["command"].ToString(), parametersDirectCast);
                    Debug.Log($"Direct cast worked! Result: {resultDirect}");
                    Assert.IsTrue(resultDirect.ToString().Contains("Hello from TCP test"), 
                        "Direct cast should work if fixed");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Direct cast failed: {ex.Message}");
                    // This is expected to fail in the current implementation
                    Assert.Fail("Direct cast should work but failed - this is the bug");
                }
            }
            
            if (parametersJObjectConvert != null)
            {
                try
                {
                    var resultJObject = ToolInvoker.InvokeTool(request["command"].ToString(), parametersJObjectConvert);
                    Debug.Log($"JObject conversion worked! Result: {resultJObject}");
                    Assert.IsTrue(resultJObject.ToString().Contains("Hello from TCP test"), 
                        "JObject conversion should work");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"JObject conversion failed: {ex.Message}");
                    Assert.Fail("JObject conversion should always work");
                }
            }
        }
    }
}