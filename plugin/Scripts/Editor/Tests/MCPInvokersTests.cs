using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

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
                UrlPattern = "unity://test",
                Example = "unity://test",
                ContainerType = typeof(TestResource),
                OutputSchema = new Schema()
            };
            registry.RegisterResource(testResourceDescriptor);
            
            var testResourceWithParamsDescriptor = new ResourceDescriptor
            {
                Name = "test_resource_with_params",
                Description = "Test resource with parameters",
                UrlPattern = "unity://test/{param1}/{param2}",
                Example = "unity://test/value1/value2",
                ContainerType = typeof(TestResourceWithParams),
                Parameters = new Dictionary<string, ParameterDescriptor>
                {
                    { "param1", new ParameterDescriptor { Description = "First parameter", Type = "string", Required = true } },
                    { "param2", new ParameterDescriptor { Description = "Second parameter", Type = "number", Required = true } }
                },
                OutputSchema = new Schema()
            };
            registry.RegisterResource(testResourceWithParamsDescriptor);
            
            var testResourceWithDefaultDescriptor = new ResourceDescriptor
            {
                Name = "test_resource_with_default",
                Description = "Test resource with default parameter",
                UrlPattern = "unity://test/default",
                Example = "unity://test/default",
                ContainerType = typeof(TestResourceWithDefault),
                Parameters = new Dictionary<string, ParameterDescriptor>
                {
                    { "param1", new ParameterDescriptor { Description = "Parameter with default value", Type = "string", Required = false } }
                },
                OutputSchema = new Schema()
            };
            registry.RegisterResource(testResourceWithDefaultDescriptor);
            
            var testToolDescriptor = new ToolDescriptor
            {
                Name = "test_tool",
                Description = "Test tool for unit tests",
                Example = "test_tool()",
                ContainerType = typeof(TestTool),
                InputSchema = new InputSchema(),
                OutputSchema = new Schema()
            };
            registry.RegisterTool(testToolDescriptor);
            
            var testToolWithParamsDescriptor = new ToolDescriptor
            {
                Name = "test_tool_with_params",
                Description = "Test tool with parameters",
                Example = "test_tool_with_params(\"value\", 42)",
                ContainerType = typeof(TestToolWithParams),
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, ParameterDescriptor>
                    {
                        { "param1", new ParameterDescriptor { Description = "First parameter", Type = "string", Required = true } },
                        { "param2", new ParameterDescriptor { Description = "Second parameter", Type = "number", Required = true } }
                    },
                    Required = new List<string> { "param1", "param2" }
                },
                OutputSchema = new Schema()
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
        /// Test ResourceInvoker with default parameter
        /// </summary>
        [Test]
        public void ResourceInvoker_WithDefaultParameter_UsesDefaultValue()
        {
            // Act - don't provide the parameter
            var result = ResourceInvoker.InvokeResource("test_resource_with_default", null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"default_value\"}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with type conversion
        /// </summary>
        [Test]
        public void ResourceInvoker_WithTypeConversion_ConvertsParametersCorrectly()
        {
            // Arrange - provide param2 as string instead of int
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", "42" }  // String instead of int
            };
            
            // Act
            var result = ResourceInvoker.InvokeResource("test_resource_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test ResourceInvoker with nonexistent resource
        /// </summary>
        [Test]
        public void ResourceInvoker_WithNonexistentResource_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ResourceInvoker.InvokeResource("nonexistent_resource", null)
            );
        }
        
        /// <summary>
        /// Test ResourceInvoker with missing required parameter
        /// </summary>
        [Test]
        public void ResourceInvoker_WithMissingRequiredParameter_ThrowsArgumentException()
        {
            // Arrange - missing required param2
            var parameters = new Dictionary<string, object>
            {
                { "param1", "test_value" }
                // param2 is missing
            };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ResourceInvoker.InvokeResource("test_resource_with_params", parameters)
            );
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
        /// Test ToolInvoker with args parameter
        /// </summary>
        [Test]
        public void ToolInvoker_WithArgs_ThrowsExceptionForMissingParam2()
        {
            // Arrange - using positional args (only one parameter where two are required)
            var parameters = new Dictionary<string, object>
            {
                { "args", "test_value" } // Single arg as string
            };
            
            // Act & Assert
            // This should throw an exception because param2 is required and not provided
            Assert.Throws<ArgumentException>(() => 
                ToolInvoker.InvokeTool("tool_with_params", parameters)
            );
        }
        
        /// <summary>
        /// Test ToolInvoker with args parameter using a tool with optional parameters
        /// </summary>
        [Test]
        public void ToolInvoker_WithArgs_HandlesOptionalParameters()
        {
            // We need to register a tool with optional parameters for this test
            var methodInfo = typeof(TestResourceWithDefault).GetMethod("GetResource");
            var toolDescriptor = new ToolDescriptor
            {
                Name = "tool_with_optional",
                Description = "Test tool with optional parameter",
                Example = "tool_with_optional()",
                ContainerType = typeof(TestResourceWithDefault),
                MethodInfo = methodInfo,
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, ParameterDescriptor>
                    {
                        { "param1", new ParameterDescriptor { Description = "Parameter with default value", Type = "string", Required = false } }
                    }
                },
                OutputSchema = new Schema()
            };
            MCPRegistry.Instance.RegisterTool(toolDescriptor);
            
            // Arrange - using no args for a tool with optional parameters
            var parameters = new Dictionary<string, object>();
            
            // Act
            var result = ToolInvoker.InvokeTool("tool_with_optional", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"default_value\"}", result);
        }
        
        /// <summary>
        /// Test ToolInvoker with args array
        /// </summary>
        [Test]
        public void ToolInvoker_WithArgsArray_PassesParametersCorrectly()
        {
            // Arrange - using positional args as JArray
            var jsonArray = Newtonsoft.Json.Linq.JArray.FromObject(new object[] { "test_value", 42 });
            var parameters = new Dictionary<string, object>
            {
                { "args", jsonArray }
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("tool_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test ToolInvoker with kwargs parameter
        /// </summary>
        [Test]
        public void ToolInvoker_WithKwargs_PassesParametersCorrectly()
        {
            // Arrange - using kwargs as JObject
            var dict = new Dictionary<string, object>
            {
                { "param1", "test_value" },
                { "param2", 42 }
            };
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(dict);
            var parameters = new Dictionary<string, object>
            {
                { "kwargs", jObject }
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("tool_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test ToolInvoker with mixed args and kwargs
        /// </summary>
        [Test]
        public void ToolInvoker_WithMixedArgsAndKwargs_PassesParametersCorrectly()
        {
            // Arrange - using both args and kwargs
            var dict = new Dictionary<string, object>
            {
                { "param2", 42 } // Only provide param2 in kwargs
            };
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(dict);
            var parameters = new Dictionary<string, object>
            {
                { "args", "test_value" }, // param1 as positional arg
                { "kwargs", jObject }      // param2 as kwarg
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("tool_with_params", parameters);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test mapping string args to first parameter (like the editor_execute_code command)
        /// </summary>
        [Test]
        public void StringArgs_ShouldMapToFirstParameter()
        {
            // Define a tool with param1, param2 like most tools would have
            var toolMethod = typeof(TestResourceWithParams).GetMethod("GetResource"); 
            var toolDescriptor = new ToolDescriptor
            {
                Name = "execute_with_code",
                Description = "Command with code as first parameter",
                Example = "execute_with_code(code, 42)",
                ContainerType = typeof(TestResourceWithParams),
                MethodInfo = toolMethod,
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, ParameterDescriptor>
                    {
                        { "param1", new ParameterDescriptor { Description = "Code to execute", Type = "string", Required = true } },
                        { "param2", new ParameterDescriptor { Description = "Some number", Type = "number", Required = true } }
                    }
                },
                OutputSchema = new Schema()
            };
            MCPRegistry.Instance.RegisterTool(toolDescriptor);
            
            // Arrange - Using just a string as args with a second param in kwargs
            var parameters = new Dictionary<string, object>
            {
                { "args", "Debug.Log(\"Hello from cursor\");" },
                { "kwargs", Newtonsoft.Json.Linq.JObject.FromObject(new Dictionary<string, object> {
                    { "param2", 42 }
                })}
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("execute_with_code", parameters);
            
            // Assert - The string should map to param1 (first parameter)
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"Debug.Log(\\\"Hello from cursor\\\");\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test with single string argument for commands like editor_execute_code
        /// </summary>
        [Test]
        public void SingleStringArg_NoKwargs_ShouldWorkForSimpleCommands()
        {
            // Define a one-parameter tool like editor_execute_code
            var codeToolMethod = typeof(TestResourceWithDefault).GetMethod("GetResource");
            var codeToolDescriptor = new ToolDescriptor
            {
                Name = "editor_execute_code",
                Description = "Execute code in editor",
                Example = "editor_execute_code(\"Debug.Log('Hello')\")",
                ContainerType = typeof(TestResourceWithDefault),
                MethodInfo = codeToolMethod,
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, ParameterDescriptor>
                    {
                        { "param1", new ParameterDescriptor { Description = "Code to execute", Type = "string", Required = false } }
                    }
                },
                OutputSchema = new Schema()
            };
            MCPRegistry.Instance.RegisterTool(codeToolDescriptor);
            
            // Arrange - Using just the args format with a string
            var parameters = new Dictionary<string, object>
            {
                { "args", "Debug.Log(\"Hello from cursor\");" },
                { "kwargs", new Dictionary<string, object>() } // Empty kwargs
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("editor_execute_code", parameters);
            
            // Assert - The string should map to param1
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"Debug.Log(\\\"Hello from cursor\\\");\"}", result);
        }
        
        /// <summary>
        /// Test array args mapping to parameters in order
        /// </summary>
        [Test]
        public void ArrayArgs_ShouldMapToParametersInOrder()
        {
            // Arrange - Using positional args as array for tool_with_params
            var jArray = Newtonsoft.Json.Linq.JArray.FromObject(new object[] { "test_value", 42 });
            var parameters = new Dictionary<string, object>
            {
                { "args", jArray }
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("tool_with_params", parameters);
            
            // Assert - Should map "test_value" to param1 and 42 to param2
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"test_value\", \"param2\": 42}", result);
        }
        
        /// <summary>
        /// Test kwargs overriding args when both are provided
        /// </summary>
        [Test]
        public void KwargsOverrideArgsWhenBothProvided()
        {
            // Arrange - conflicting value for param1
            var parameters = new Dictionary<string, object>
            {
                { "args", "original_value" }, // This would normally go to param1
                { "kwargs", Newtonsoft.Json.Linq.JObject.FromObject(new Dictionary<string, object> {
                    { "param1", "override_value" }, // This should override the args value
                    { "param2", 42 }
                })}
            };
            
            // Act
            var result = ToolInvoker.InvokeTool("tool_with_params", parameters);
            
            // Assert - kwargs value should win
            Assert.IsNotNull(result);
            Assert.AreEqual("{\"param1\": \"override_value\", \"param2\": 42}", result);
        }
    }
}