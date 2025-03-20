using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Tests
{
    /// <summary>
    /// Test class for MCPAttributeUtil
    /// </summary>
    public class MCPAttributeUtilTests
    {
        [Test]
        public void ConvertCamelCaseToSnakeCase_ReturnsCorrectResults()
        {
            // Arrange
            string[] testCases = new string[]
            {
                "TestString",
                "TestStringWithMultipleWords",
                "Test",
                "",
                "TestURLString",
                "testString",
                "test_string" // Already snake case
            };

            string[] expectedResults = new string[]
            {
                "test_string",
                "test_string_with_multiple_words",
                "test",
                "",
                "test_url_string",
                "test_string",
                "test_string"
            };

            // Act & Assert
            for (int i = 0; i < testCases.Length; i++)
            {
                string result = MCPAttributeUtil.ConvertCamelCaseToSnakeCase(testCases[i]);
                Assert.AreEqual(expectedResults[i], result, $"Failed for test case: {testCases[i]}");
            }
        }

        [Test]
        public void GetTypeString_ReturnsCorrectTypeStrings()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("string", MCPAttributeUtil.GetTypeString(typeof(string)));
            Assert.AreEqual("number", MCPAttributeUtil.GetTypeString(typeof(int)));
            Assert.AreEqual("number", MCPAttributeUtil.GetTypeString(typeof(float)));
            Assert.AreEqual("number", MCPAttributeUtil.GetTypeString(typeof(double)));
            Assert.AreEqual("boolean", MCPAttributeUtil.GetTypeString(typeof(bool)));
            Assert.AreEqual("array", MCPAttributeUtil.GetTypeString(typeof(int[])));
            Assert.AreEqual("array", MCPAttributeUtil.GetTypeString(typeof(List<string>)));
            Assert.AreEqual("object", MCPAttributeUtil.GetTypeString(typeof(Vector3)));
            Assert.AreEqual("object", MCPAttributeUtil.GetTypeString(typeof(object)));
        }

        // Mock classes for testing
        [MCPTool("test_tool", "Test tool description", "test_tool(value: 123)")]
        public static class MockToolCommand
        {
            public static int Execute(
                [MCPParameter("value", "Test parameter", "number", true)] int value)
            {
                return value * 2;
            }
        }

        [MCPTool(null, "Tool with inferred name", "inferred_tool(value: 'hello')")]
        public static class InferredNameCommand
        {
            public static string Execute(
                [MCPParameter(null, "Inferred parameter", "string", true)] string value)
            {
                return value.ToUpper();
            }
        }

        [MCPResource("test_resource", "Test resource description", "test/{id}", "test/123")]
        public static class MockResourceHandler
        {
            public static string GetResource(
                [MCPParameter("id", "Resource ID", "string", true)] string id)
            {
                return $"Resource content for {id}";
            }
        }

        [MCPResource(null, "Resource with inferred name", "inferred/{id}", null)]
        public static class InferredNameResource
        {
            public static Dictionary<string, object> GetResource(string id)
            {
                return new Dictionary<string, object>
                {
                    { "id", id },
                    { "name", "Inferred resource" }
                };
            }
        }

        [Test]
        public void CreateToolDescriptorFromCommandType_WithExplicitName_ReturnsCorrectDescriptor()
        {
            // Arrange
            Type type = typeof(MockToolCommand);

            // Act
            ToolDescriptor descriptor = MCPAttributeUtil.CreateToolDescriptorFromCommandType(type);

            // Assert
            Assert.IsNotNull(descriptor, "Descriptor should not be null");
            Assert.AreEqual("test_tool", descriptor.Name, "Name should match attribute");
            Assert.AreEqual("Test tool description", descriptor.Description, "Description should match attribute");
            Assert.AreEqual("test_tool(value: 123)", descriptor.Example, "Example should match attribute");
            
            // Input schema
            Assert.IsNotNull(descriptor.InputSchema, "Input schema should not be null");
            Assert.IsTrue(descriptor.InputSchema.Properties.ContainsKey("value"), "Input schema should have 'value' property");
            Assert.AreEqual("number", descriptor.InputSchema.Properties["value"].Type, "Parameter type should be 'number'");
            Assert.AreEqual("Test parameter", descriptor.InputSchema.Properties["value"].Description, "Parameter description should match");
            Assert.IsTrue(descriptor.InputSchema.Properties["value"].Required, "Parameter should be required");
            
            // Output schema
            Assert.IsNotNull(descriptor.OutputSchema, "Output schema should not be null");
            Assert.IsTrue(descriptor.OutputSchema.Properties.ContainsKey("result"), "Output schema should have 'result' property");
            Assert.AreEqual("number", descriptor.OutputSchema.Properties["result"].Type, "Output type should be 'number'");
        }

        [Test]
        public void CreateToolDescriptorFromCommandType_WithInferredName_ReturnsCorrectDescriptor()
        {
            // Arrange
            Type type = typeof(InferredNameCommand);

            // Act
            ToolDescriptor descriptor = MCPAttributeUtil.CreateToolDescriptorFromCommandType(type);

            // Assert
            Assert.IsNotNull(descriptor, "Descriptor should not be null");
            Assert.AreEqual("inferred_name", descriptor.Name, "Name should be inferred from class name");
            Assert.AreEqual("Tool with inferred name", descriptor.Description, "Description should match attribute");
            
            // Input schema
            Assert.IsNotNull(descriptor.InputSchema, "Input schema should not be null");
            Assert.IsTrue(descriptor.InputSchema.Properties.ContainsKey("value"), "Input schema should have 'value' property");
            Assert.AreEqual("string", descriptor.InputSchema.Properties["value"].Type, "Parameter type should be 'string'");
            Assert.AreEqual("Inferred parameter", descriptor.InputSchema.Properties["value"].Description, "Parameter description should match");
            Assert.IsTrue(descriptor.InputSchema.Properties["value"].Required, "Parameter should be required");
            
            // Output schema
            Assert.IsNotNull(descriptor.OutputSchema, "Output schema should not be null");
            Assert.IsTrue(descriptor.OutputSchema.Properties.ContainsKey("result"), "Output schema should have 'result' property");
            Assert.AreEqual("string", descriptor.OutputSchema.Properties["result"].Type, "Output type should be 'string'");
        }

        [Test]
        public void CreateResourceDescriptorFromHandlerType_WithExplicitName_ReturnsCorrectDescriptor()
        {
            // Arrange
            Type type = typeof(MockResourceHandler);

            // Act
            ResourceDescriptor descriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(type);

            // Assert
            Assert.IsNotNull(descriptor, "Descriptor should not be null");
            Assert.AreEqual("test_resource", descriptor.Name, "Name should match attribute");
            Assert.AreEqual("Test resource description", descriptor.Description, "Description should match attribute");
            Assert.AreEqual("test/{id}", descriptor.UrlPattern, "URL pattern should match attribute");
            Assert.AreEqual("test/123", descriptor.Example, "Example should match attribute");
            
            // Parameters
            Assert.IsNotNull(descriptor.Parameters, "Parameters should not be null");
            Assert.IsTrue(descriptor.Parameters.ContainsKey("id"), "Parameters should have 'id' parameter");
            Assert.AreEqual("string", descriptor.Parameters["id"].Type, "Parameter type should be 'string'");
            Assert.AreEqual("Resource ID", descriptor.Parameters["id"].Description, "Parameter description should match");
            Assert.IsTrue(descriptor.Parameters["id"].Required, "Parameter should be required");
            
            // Output schema
            Assert.IsNotNull(descriptor.OutputSchema, "Output schema should not be null");
            Assert.IsTrue(descriptor.OutputSchema.Properties.ContainsKey("result"), "Output schema should have 'result' property");
            Assert.AreEqual("string", descriptor.OutputSchema.Properties["result"].Type, "Output type should be 'string'");
        }

        [Test]
        public void CreateResourceDescriptorFromHandlerType_WithInferredName_ReturnsCorrectDescriptor()
        {
            // Arrange
            Type type = typeof(InferredNameResource);

            // Act
            ResourceDescriptor descriptor = MCPAttributeUtil.CreateResourceDescriptorFromHandlerType(type);

            // Assert
            Assert.IsNotNull(descriptor, "Descriptor should not be null");
            Assert.AreEqual("inferred_name", descriptor.Name, "Name should be inferred from class name");
            Assert.AreEqual("Resource with inferred name", descriptor.Description, "Description should match attribute");
            Assert.AreEqual("inferred/{id}", descriptor.UrlPattern, "URL pattern should match attribute");
            
            // Parameters
            Assert.IsNotNull(descriptor.Parameters, "Parameters should not be null");
            Assert.IsTrue(descriptor.Parameters.ContainsKey("id"), "Parameters should have 'id' parameter");
            
            // Output schema for dictionary
            Assert.IsNotNull(descriptor.OutputSchema, "Output schema should not be null");
            Assert.AreEqual("object", descriptor.OutputSchema.Properties["result"].Type, "Output type should be 'object' for Dictionary");
        }

        [Test]
        public void GetSchemaFromType_WithNonSchemaType_ReturnsEmptySchema()
        {
            // Arrange
            Type type = typeof(string); // A type with no schema attributes

            // Act
            Schema schema = MCPAttributeUtil.GetSchemaFromType(type);

            // Assert
            Assert.IsNotNull(schema, "Schema should not be null");
            Assert.AreEqual(0, schema.Properties.Count, "Schema should have no properties");
            Assert.AreEqual(0, schema.Required.Count, "Schema should have no required properties");
        }

        // Mock schema class for testing GetSchemaFromType
        [MCPSchema("Test schema description", "input")]
        public class MockSchema
        {
            [MCPParameter("prop1", "Property one", "string", true)]
            public string Property1 { get; set; }

            [MCPParameter("prop2", "Property two", "number", false)]
            public int Property2 { get; set; }

            [MCPParameter(null, "Property with inferred name", "boolean", true)]
            public bool Flag { get; set; }

            // Property without attribute - should be ignored
            public string Ignored { get; set; }
        }

        [Test]
        public void GetSchemaFromType_WithSchemaType_ReturnsCorrectSchema()
        {
            // Arrange
            Type type = typeof(MockSchema);

            // Act
            Schema schema = MCPAttributeUtil.GetSchemaFromType(type);

            // Assert
            Assert.IsNotNull(schema, "Schema should not be null");
            
            // Check properties
            Assert.AreEqual(3, schema.Properties.Count, "Schema should have 3 properties");
            
            // Check prop1
            Assert.IsTrue(schema.Properties.ContainsKey("prop1"), "Schema should have 'prop1' property");
            Assert.AreEqual("string", schema.Properties["prop1"].Type, "Property1 type should be 'string'");
            Assert.AreEqual("Property one", schema.Properties["prop1"].Description, "Property1 description should match");
            Assert.IsTrue(schema.Properties["prop1"].Required, "Property1 should be required");
            
            // Check prop2
            Assert.IsTrue(schema.Properties.ContainsKey("prop2"), "Schema should have 'prop2' property");
            Assert.AreEqual("number", schema.Properties["prop2"].Type, "Property2 type should be 'number'");
            Assert.AreEqual("Property two", schema.Properties["prop2"].Description, "Property2 description should match");
            Assert.IsFalse(schema.Properties["prop2"].Required, "Property2 should not be required");
            
            // Check Flag (inferred name)
            Assert.IsTrue(schema.Properties.ContainsKey("Flag"), "Schema should have 'Flag' property");
            Assert.AreEqual("boolean", schema.Properties["Flag"].Type, "Flag type should be 'boolean'");
            Assert.AreEqual("Property with inferred name", schema.Properties["Flag"].Description, "Flag description should match");
            Assert.IsTrue(schema.Properties["Flag"].Required, "Flag should be required");
            
            // Check required list
            Assert.AreEqual(2, schema.Required.Count, "Schema should have 2 required properties");
            Assert.IsTrue(schema.Required.Contains("prop1"), "Required list should contain 'prop1'");
            Assert.IsTrue(schema.Required.Contains("Flag"), "Required list should contain 'Flag'");
        }
    }
}