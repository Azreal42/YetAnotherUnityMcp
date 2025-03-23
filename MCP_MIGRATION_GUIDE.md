# Migration Guide: MCP Schema Specification Compliance

This guide documents the breaking changes made to bring YetAnotherUnityMcp into compliance with the [official Model Context Protocol (MCP) specification](https://modelcontextprotocol.io).

## Overview of Changes

The following major changes were made to ensure compliance with the MCP specification:

1. **Response Format**: Responses now use `content` arrays instead of direct values
2. **Tool Schema Structure**: Tool definitions now follow the official MCP schema format
3. **Resource Schema Structure**: Resources now use `uri` instead of `urlPattern` with additional metadata
4. **Parameter Descriptors**: `required` is now an array at the schema level rather than a property flag

## Breaking Changes

### 1. Response Format

**Before:**
```json
{
  "id": "req_101",
  "type": "response",
  "status": "success",
  "result": "Command executed successfully"
}
```

**After:**
```json
{
  "id": "req_101",
  "type": "response",
  "status": "success",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Command executed successfully"
      }
    ],
    "isError": false
  }
}
```

### 2. Tool Schema Structure

**Before:**
```json
{
  "name": "execute_code",
  "description": "Execute C# code in Unity",
  "inputSchema": {
    "type": "object",
    "properties": {
      "code": {
        "type": "string",
        "description": "C# code to execute",
        "required": true
      }
    }
  },
  "outputSchema": {
    "type": "object",
    "properties": {
      "output": {
        "type": "string",
        "description": "Execution output",
        "required": true
      }
    }
  },
  "example": "execute_code(\"Debug.Log(\\\"Hello\\\");\")"
}
```

**After:**
```json
{
  "name": "execute_code",
  "description": "Execute C# code in Unity",
  "inputSchema": {
    "type": "object",
    "properties": {
      "code": {
        "type": "string",
        "description": "C# code to execute"
      }
    },
    "required": ["code"]
  },
  "example": "execute_code(\"Debug.Log(\\\"Hello\\\");\")"
}
```

### 3. Resource Schema Structure

**Before:**
```json
{
  "name": "unity_logs",
  "description": "Get logs from the Unity Editor console",
  "urlPattern": "unity://logs/{max_logs}",
  "parameters": {
    "max_logs": {
      "type": "number",
      "description": "Maximum number of logs to return",
      "required": false
    }
  },
  "example": "unity://logs/50"
}
```

**After:**
```json
{
  "name": "unity_logs",
  "description": "Get logs from the Unity Editor console",
  "uri": "unity://logs/{max_logs}",
  "mimeType": "application/json",
  "parameters": {
    "max_logs": {
      "type": "number",
      "description": "Maximum number of logs to return"
    }
  },
  "example": "unity://logs/50"
}
```

## Migration Steps for Clients

If you are consuming this library, here are the steps to migrate your code:

### 1. Handling Responses

Update your response handling code to extract data from the content array:

```python
# Before
result = response.get("result")
print(f"Result: {result}")

# After
result = response.get("result")
if isinstance(result, dict) and "content" in result:
    # Extract text content
    content_array = result["content"]
    text_content = ""
    for item in content_array:
        if item.get("type") == "text":
            text_content += item.get("text", "")
    print(f"Result: {text_content}")
    
    # Check for errors
    if result.get("isError", False):
        print("Error in response")
else:
    # Backward compatibility for older responses
    print(f"Result: {result}")
```

### 2. Parsing Tool Schema

Update your schema parsing code to handle the new format:

```python
# Before
required_params = []
for param_name, param_desc in tool_schema.get("inputSchema", {}).get("properties", {}).items():
    if param_desc.get("required", False):
        required_params.append(param_name)

# After
required_params = tool_schema.get("inputSchema", {}).get("required", [])
```

### 3. Accessing Resources

Update resource access code to use the new uri field:

```python
# Before
url_pattern = resource_schema.get("urlPattern")
if not url_pattern:
    print("Resource has no URL pattern")

# After
uri = resource_schema.get("uri")
if not uri:
    # Try fallback for backward compatibility
    uri = resource_schema.get("urlPattern")
if not uri:
    print("Resource has no URI")
```

## Helper Functions

We've added several helper functions in the MCPResponse class to simplify creating compliant responses:

```csharp
// Create a simple text response
var response = MCPResponse.CreateTextResponse("Hello, world!");

// Create an error response
var errorResponse = MCPResponse.CreateErrorResponse("An error occurred");

// Create an image response
var imageResponse = MCPResponse.CreateImageResponse("http://example.com/image.png", "image/png");

// Create an embedded resource response
var embeddedResponse = MCPResponse.CreateEmbeddedResponse("unity://resource/123");
```

## Python Client Updates

The Python client has been updated to handle the new response format automatically. When consuming responses from Unity:

1. The `low_level_tcp_client.py` extracts and processes content arrays
2. The `unity_client_util.py` provides logging for content types
3. The `dynamic_tools.py` handles the updated schema format with uri and required arrays

## Testing Your Integration

To test your integration with the updated MCP schema:

1. Start the Unity server and ensure it's running with the latest version
2. Run a basic tool command (e.g., get_unity_info) and inspect the response format
3. Verify that your client code correctly extracts content from the response
4. Test error handling by intentionally causing an error and verifying the isError flag
5. Validate resource access with the new uri format