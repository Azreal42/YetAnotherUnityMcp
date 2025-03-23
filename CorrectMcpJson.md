# Correcting MCP JSON Schema Implementation Plan

[Official spec link](https://raw.githubusercontent.com/modelcontextprotocol/specification/refs/heads/main/schema/2024-11-05/schema.json)

## Current Issues

After analyzing the official MCP specification and our current implementation, I've identified several discrepancies that need to be addressed:

1. **Tool Schema Structure**: Our implementation doesn't fully align with the official MCP specification for tool definitions.
2. **Resource Schema Structure**: Our resources don't match the expected format in the MCP specification.
3. **Parameter Handling**: The way we define parameters requires updates to match the spec.
4. **Response Format**: Our tool responses don't follow the required structure with `content` arrays.

## Detailed Analysis

### Current Implementation vs. MCP Specification

#### Tool Definition:
- **Current**: We define tools with `name`, `description`, `inputSchema`, `outputSchema`, and `example`.
- **Spec**: The MCP spec requires `name` and `inputSchema`, with a different structure for responses.

#### Resource Definition:
- **Current**: We use `name`, `description`, `urlPattern`, `parameters`, `outputSchema`, and `example`.
- **Spec**: The MCP spec uses `name`, `uri`, and optional fields like `description` and `mimeType`.

#### Parameter Structure:
- **Current**: We place `required` as a property of each parameter descriptor.
- **Spec**: The `required` field should be an array at the schema level listing required parameter names.

#### Response Format:
- **Current**: We don't structure responses with a `content` array.
- **Spec**: Tool results must include a `content` array containing text, image, or embedded resources.

## Implementation Plan

### Phase 1: Update Schema Models (C#) - ✅ COMPLETED

1. **Update `MCPToolSchema.cs`**: ✅ COMPLETED
   - ✅ Modified `ToolDescriptor` to match the MCP specification 
   - ✅ Updated `ParameterDescriptor` to rename `Required` to `IsRequired` (kept for internal use, not serialized)
   - ✅ Added proper support for the `content` array in responses via new ContentItem classes
   - ✅ Added support for error reporting via `isError` field in MCPResponse

2. **Update `ResourceDescriptor`**: ✅ COMPLETED
   - ✅ Renamed `urlPattern` to `uri` with backward compatibility getter/setter
   - ✅ Added missing fields from the spec (`mimeType`, `size`)
   - ✅ Implemented the parameter handling to match the spec

3. **Create New Response Models**: ✅ COMPLETED
   - ✅ Implemented `MCPResponse` with support for the `content` array
   - ✅ Added `ContentItem`, `ImageContent`, and `EmbeddedContent` classes for content types

### Phase 2: Update Serialization & Deserialization (C#) - ✅ COMPLETED

1. **Modify `MCPRegistry.cs`**: ✅ COMPLETED
   - ✅ Updated schema generation to use the new model structure
   - ✅ Schema serialization now produces spec-compliant JSON

2. **Update Tool Invocation**: ✅ COMPLETED
   - ✅ Modified `MCPInvokers.cs` to handle the new parameter structure
   - ✅ Updated response formatting to use the `content` array structure in MCPTcpServer.cs

3. **Adjust Container Support**: ✅ COMPLETED
   - ✅ Updated container-based tools to work with the new schema format
   - ✅ Ensured parameter handling works correctly with the new structure

### Phase 3: Update Python Client - ✅ COMPLETED

1. **Update Dynamic Tool Generation**: ✅ COMPLETED
   - ✅ Modified `dynamic_tools.py` to handle the updated schema format
   - ✅ Ensured tool registration works with the new parameter structure
   - ✅ Updated resource URI handling from urlPattern to uri

2. **Update Response Handling**: ✅ COMPLETED
   - ✅ Modified response parsing to handle the `content` array format
   - ✅ Added support for the `isError` field in responses
   - ✅ Implemented `_extract_text_from_content` helper method

3. **Update Resource Access**: ✅ COMPLETED
   - ✅ Updated resource URL handling to match the new format
   - ✅ Added mimeType support for resources
   - ✅ Ensured parameter passing works with the updated structure

### Phase 4: Testing & Validation - 🟡 IN PROGRESS

1. **Update Unit Tests**: 🟡 IN PROGRESS
   - ⬜ Fix all unit tests to work with the new schema format
   - ⬜ Add tests specifically for MCP specification compliance

2. **Create Schema Validation**: ⬜ NOT STARTED
   - ⬜ Add validation against the official MCP schema
   - ⬜ Implement validation in both client and server

3. **Manual Testing**: 🟡 IN PROGRESS
   - ⬜ Test all tools and resources manually to ensure they work correctly
   - ⬜ Validate schema output against the official schema

### Phase 5: Documentation Update - ✅ COMPLETED

1. **Update Documentation**: ✅ COMPLETED
   - ✅ Updated README.md with the new schema format
   - ✅ Updated TECH_DETAILS.md with the implementation details
   - ✅ Updated JSON examples to show content array structure

2. **Add Migration Guide**: ✅ COMPLETED
   - ✅ Added MCP_MIGRATION_GUIDE.md for migrating from the old format to the new one
   - ✅ Included examples of before/after for tools and resources with migration steps

## Concrete Example of Expected Format

```json
// Tool definition per MCP spec
{
  "name": "execute_code",
  "inputSchema": {
    "type": "object",
    "properties": {
      "code": {
        "type": "string",
        "description": "C# code to execute"
      }
    },
    "required": ["code"]
  }
}

// Tool response per MCP spec
{
  "content": [
    {
      "type": "text",
      "text": "Code executed successfully. Result: 42"
    }
  ],
  "isError": false
}

// Resource definition per MCP spec
{
  "name": "unity_logs",
  "uri": "unity://logs/50",
  "description": "Get logs from the Unity Editor console",
  "mimeType": "application/json"
}
```

## Implementation Timeline

- **Phase 1** (Schema Models): 3 days
- **Phase 2** (Serialization): 2 days
- **Phase 3** (Python Client): 2 days
- **Phase 4** (Testing): 3 days
- **Phase 5** (Documentation): 2 days

Total estimated time: 12 working days

## Key Files to Modify

1. `/plugin/Scripts/Editor/Models/MCPToolSchema.cs`
2. `/plugin/Scripts/Editor/Models/MCPRegistry.cs`
3. `/plugin/Scripts/Editor/Models/MCPInvokers.cs`
4. `/server/dynamic_tools.py`
5. `/server/unity_client_util.py`

## Risks and Mitigation

1. **Breaking Changes**: This will introduce breaking changes for consumers of our MCP implementation.
   - Mitigation: Provide clear migration documentation and possibly a compatibility layer.

2. **Tool Response Format**: The most significant change is in the response format with the `content` array.
   - Mitigation: Implement helper methods to simplify response creation.

3. **Integration Testing**: Ensuring all tools work correctly with the new format.
   - Mitigation: Comprehensive test coverage and manual validation.