# YetAnotherUnityMcp - Technical Details

## System Architecture 

The architecture is organized into two main processes (Unity and Python), each internally modular, following best practices of their ecosystem. The key difference from traditional MCP setups is that Unity acts as the TCP server rather than the client. The system now features a container-based organization pattern for tools and resources, which improves code structure and reduces duplication.

### Unity Server Structure

The Unity side is implemented as a typical Unity plugin with separate editor code. All functionality resides in the **Editor** scripts, which are only used in the Unity Editor. The core components on the Unity side are:

1. **TcpServer**: Manages TCP socket connections, provides events for connection management, and message routing.
2. **MCPTcpServer**: Primary high-level manager for the TCP server with MCP-specific functionality.
3. **MCPWindow**: Editor window for managing and monitoring the server.
5. **Command Classes**: Individual command implementations for each functionality (ExecuteCode, TakeScreenshot, etc.).
6. **TcpMessages**: Message types for handling communication (requests, responses, errors).
7. **MCPRegistry**: Registry for tools and resources with schema information and support for container-based organization.
8. **MCPToolSchema**: Schema models for tools and resources (ToolDescriptor, ResourceDescriptor, etc.).
9. **MCPInvokers**: Tool and resource invokers with dynamic parameter mapping and type conversion.
10. **MCPContainers**: Container classes that group related tools and resources with shared prefixes.

### Python Client Structure

The Python client uses the FastMCP framework to define the available actions and data endpoints in an organized way, with support for dynamic resource invocation and parameter mapping. Key components include:

1. **mcp_server.py**: The main MCP server implementation using FastMCP with lifespan management.
2. **unity_tcp_client.py**: The high-level client for communicating with Unity using TCP.
3. **websocket_client.py**: The low-level TCP client implementation (named for backward compatibility).
4. **unity_client_util.py**: Utility functions for standardized client operations.
5. **dynamic_tool_invoker.py**: System for dynamically creating and invoking tools based on schema.
6. **dynamic_tools.py**: Tool and resource manager for FastMCP integration.
7. **connection_manager.py**: Connection lifecycle management utilities.

The client can run in two modes:
- **MCP mode**: Using FastMCP's built-in server with STDIO transport (via `fastmcp run` or an MCP-enabled FastAPI app).
- **Direct mode**: A direct TCP client connection to Unity for testing and development.

## Communication Protocol (TCP & JSON)

All communication between the Unity server and the Python client uses a **TCP socket** connection with a simple framing protocol. This provides persistent, low-latency bidirectional messaging while avoiding WebSocket compatibility issues that can occur in Unity's .NET/Mono implementation.

### Message Framing Protocol

Messages are framed with a simple binary protocol:
- `STX` (Start of Text, 0x02) - marks the beginning of a message
- 4-byte little-endian message length
- Message content (UTF-8 encoded JSON)
- `ETX` (End of Text, 0x03) - marks the end of a message

This framing allows reliable message boundary detection without relying on WebSockets.

### Connection Establishment

1. Client connects to the TCP server
2. Client sends `YAUM_HANDSHAKE_REQUEST` as plain text  
3. Server responds with `YAUM_HANDSHAKE_RESPONSE` as plain text
4. Connection is now ready for framed message exchange

### Keep-Alive Mechanism

To maintain connection health:
- Client sends a `PING` message every 30 seconds
- Server responds with a `PONG` message
- If no response is received, the client attempts to reconnect

### Message Format

Within the framing protocol, every message is a JSON object containing at least a **command or response type**, a **unique ID** (to pair requests with responses), and **parameters or result object**. We define two primary message types:

#### Command Request

Sent from the Python client to Unity to request an action. It contains a `command` name and any parameters needed. For example: 

```json
{
  "id": "req_101",
  "command": "execute_code",
  "parameters": {
    "code": "Debug.Log(\"Hello from AI\");"
  },
  "client_timestamp": 1710956378123
}
```

This asks Unity to execute a snippet of C# code.

#### Command Response

Sent from Unity back to the client as a reply to a request. It echoes the same `id` and contains either a result or an error. For a successful response, we use `status: "success"` and include the `result` data. For errors, `status: "error"` and an `error` message field are used.

```json
{
  "id": "req_101",
  "type": "response",
  "status": "success",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Hello from AI"
      }
    ],
    "isError": false
  },
  "server_timestamp": 1710956378456,
  "client_timestamp": 1710956378123
}
```

For errors:

```json
{
  "id": "req_101",
  "type": "response",
  "status": "error",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "NullReferenceException at line 1 ..."
      }
    ],
    "isError": true
  },
  "error": "NullReferenceException at line 1 ...",
  "server_timestamp": 1710956378456,
  "client_timestamp": 1710956378123
}
```

### Image Data

For commands that produce image data (such as a screenshot capture tool), the Unity server can transmit images using file paths or base64 encoding within JSON:

```json
{
  "id": "req_103",
  "type": "response",
  "status": "success",
  "result": {
    "content": [
      {
        "type": "image",
        "image": {
          "url": "/path/to/screenshot.png",
          "mimeType": "image/png"
        }
      },
      {
        "type": "text",
        "text": "Screenshot captured with dimensions 1280x720"
      }
    ],
    "isError": false
  },
  "server_timestamp": 1710956379456,
  "client_timestamp": 1710956379123
}
```

### Supported Commands

By default, the system defines a set of commands that the Unity server will recognize and handle:

- `execute_code`: Execute an arbitrary C# code snippet in the Unity environment.
- `take_screenshot`: Capture a screenshot of the current Unity Editor view.
- `modify_object`: Modify properties of a Unity GameObject.
- `get_logs`: Get recent Unity console logs.
- `get_unity_info`: Get information about the Unity environment.
- `get_schema`: Retrieve information about all available tools and resources.
- `access_resource`: Generic command to access any registered resource by name.

The `access_resource` command deserves special mention as it provides a dynamic way to access any resource registered in the schema. It takes two parameters:
- `resource_name`: Name of the resource to access (e.g., "unity_info")
- `parameters`: Dictionary of parameters to pass to the resource (optional)

This command uses the `ResourceInvoker` behind the scenes to dynamically lookup the resource in the registry, find its handler type, and invoke the appropriate method with parameter mapping and type conversion.

## TCP Socket Implementation Details

### Unity TCP Server

The Unity TCP server has the following components:

1. **TcpServer**: Core server implementation that handles:
   - Managing client connections via TCP sockets using System.Net.Sockets.TcpListener
   - Message framing, parsing, and routing
   - Thread-safe message queue for processing on the main thread
   - Performance monitoring and latency tracking
   - Error handling and logging
   - Handshake and connection management
   - Ping/pong protocol for connection health

2. **MCPTcpServer**: High-level manager that provides:
   - Command processing for MCP tools
   - JSON serialization/deserialization
   - Client tracking with connection information
   - Logging and monitoring functions
   - Interface for other Unity scripts to use

3. **MCPWebSocketServer**: Legacy interface that delegates to MCPTcpServer for backward compatibility.

4. **MCPWindow**: Editor window that provides:
   - Server controls (start/stop)
   - Connected client list
   - Message log for debugging
   - Status information

### Python TCP Client

The Python client implementation includes:

1. **UnityTcpClient** (with WebSocketClient as alias): Low-level client that handles:
   - Connection to Unity server using asyncio TCP sockets with retry mechanism
   - Message framing, sending, and receiving with binary protocol
   - Request-response tracking with unique IDs
   - Event-based communication (connected, disconnected, message, error)
   - Async/await interface for Python
   - Automatic reconnection and keep-alive pings

2. **unity_tcp_client.py**: High-level client that provides:
   - Higher-level APIs for Unity commands (execute_code, take_screenshot, etc.)
   - Callback registration for status events
   - Error handling and logging

3. **unity_client_util**: Utility module that provides:
   - Standardized execution pattern for all operations
   - Automatic reconnection attempts
   - Consistent error handling and formatting
   - Context integration with FastMCP

## Error Handling & Security

The system is designed to handle errors gracefully and maintain security:

1. **Connection Handling**: Both server and client handle connection errors gracefully with appropriate logging and notifications.

2. **Message Validation**: All messages are validated for proper format and content.

3. **Thread Safety**: The Unity server processes all WebSocket messages on the main Unity thread to ensure thread safety.

4. **Timeout Handling**: Command execution has timeouts to prevent hanging operations.

5. **Connection Monitoring**: Performance metrics are tracked and logged for monitoring connection health.

## Thread-Local Context Management

The system uses thread-local storage via the `ResourceContext` class to handle passing context objects across the resource access pipeline:

1. **Purpose and Role**:
   - `ResourceContext` provides thread-local storage for passing the `Context` object across resource handlers
   - It solves a specific architectural challenge: the FastMCP library requires resource handler functions to have signatures that exactly match URI parameters, but these handlers also need access to the `Context` object to log information and handle errors

2. **Implementation**:
   - Uses `threading.local()` for thread-safe storage of the current context
   - Provides `get_current_ctx()` and `set_current_ctx()` methods for accessing/setting the current context
   - Includes a context manager (`with_context()`) for setting and restoring context in a scoped manner
   - Supports nested contexts within the same thread

3. **Usage Pattern**:
   - When resource handlers are registered, they have signatures matching only the URI parameters
   - Before invoking the resource handler, the system sets the current context using the context manager
   - Within the resource handler, `ResourceContext.get_current_ctx()` provides access to the context without it appearing in the function signature
   - After the handler completes, the original context is automatically restored

This approach maintains compatibility with FastMCP's interface validation while providing access to important context information. While it introduces some indirection through thread-local storage, this is a common pattern in web frameworks and request handling systems where direct parameter passing isn't feasible.

## Schema System

The system includes a schema retrieval mechanism that provides self-documentation of available tools and resources. This allows clients to discover what operations are available without hardcoding knowledge of the API.

### Schema Structure

The schema information is organized as follows:

```json
{
  "tools": [
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
      "example": "execute_code(\"Debug.Log(\\\"Hello from AI\\\"); return 42;\")"
    }
  ],
  "resources": [
    {
      "name": "unity_info",
      "description": "Get information about the Unity environment",
      "uri": "unity://info",
      "mimeType": "application/json",
      "example": "unity://info"
    }
  ]
}
```

### Schema Registration and Discovery

The schema system uses C# attributes for automatic schema generation. This allows for a more maintainable and type-safe approach to schema definition. The key components are:

1. **Attribute-Based Schema Definition**:
   - `MCPContainerAttribute`: Marks a class as a container for multiple MCP tools and resources
   - `MCPToolAttribute`: Marks a class or method as an MCP tool with name, description, and example
   - `MCPResourceAttribute`: Marks a class or method as an MCP resource with name, description, URL pattern, and example
   - `MCPSchemaAttribute`: Marks nested classes as input or output schemas
   - `MCPParameterAttribute`: Defines parameters for inputs and outputs with type information

2. **Introspection**:
   The `MCPAttributeUtil` class provides methods to extract schema information from attributed classes through reflection:
   - `CreateToolDescriptorFromType`: Creates a tool descriptor from a class with attributes
   - `CreateResourceDescriptorFromType`: Creates a resource descriptor from a class with attributes
   - `GetSchemaFromType`: Extracts schema information from input/output model classes

3. **Registration**:
   Tools and resources are registered in the Unity `MCPRegistry` class, which maintains a singleton instance with all available operations. The registration happens automatically using reflection on attributed classes.
   
4. **Resource and Tool Invocation**:
   The system provides dynamic invocation of resources and tools through dedicated invoker classes:
   - `ResourceInvoker`: Dynamically invokes resources by name from the registry
   - `ToolInvoker`: Dynamically invokes tools by name from the registry
   - Both invokers handle parameter mapping, type conversion, and error handling
   - Registry provides lookup methods to find resources and tools by name

5. **Name Inference**:
   When the `name` parameter of an attribute is set to `null`, the system automatically infers the name by:
   - Converting the class or member name from CamelCase to snake_case
   - Removing suffix words like "Command" or "Resource" 
   - For example: `TakeScreenshotCommand` → `take_screenshot`

6. **Dynamic Invocation and Parameter Mapping**:
   The system provides a standardized way to dynamically invoke resources and tools with robust parameter handling:
   - `ResourceInvoker.InvokeResource(string resourceName, Dictionary<string, object> parameters)`: Invokes a resource by name
   - `ToolInvoker.InvokeTool(string toolName, Dictionary<string, object> parameters)`: Invokes a tool by name
   - Both methods handle parameter mapping, type conversion, and proper error handling
   - Used by the TCP server to handle `access_resource` and other dynamic commands
   
   **Parameter Naming Convention Handling**:
   - The system automatically converts between different parameter naming conventions
   - Unity uses camelCase for parameters (e.g., `objectId`, `sceneName`)
   - Python client code uses snake_case (e.g., `object_id`, `scene_name`) as is standard in Python
   - The parameter mapping system handles this conversion automatically:
     - In Python code, use snake_case parameter names (e.g., `object_id`, `property_name`) 
     - When sending to Unity, these names are automatically converted to camelCase (e.g., `objectId`, `propertyName`)
     - This conversion happens in the `invoke_dynamic_resource` and `invoke_dynamic_tool` functions
   - This makes the API more ergonomic for Python developers while maintaining compatibility with Unity's expected parameter formats
   - Examples:
     - Python: `await invoke_dynamic_resource("object_properties", {"object_id": "Cube", "property_name": "position"})`
     - Converted to: `{"objectId": "Cube", "propertyName": "position"}` before sending to Unity

7. **Testing**:
   The schema and invocation systems are thoroughly tested using NUnit tests:
   - `MCPAttributeUtilTests`: Tests for the core schema generation functions
     - `ConvertCamelCaseToSnakeCase_ReturnsCorrectResults`: Tests the name inference algorithm
     - `GetTypeString_ReturnsCorrectTypeStrings`: Tests C# type conversion to schema types
     - `CreateToolDescriptorFromCommandType_WithExplicitName_ReturnsCorrectDescriptor`: Tests explicit name schema generation
     - `CreateToolDescriptorFromCommandType_WithInferredName_ReturnsCorrectDescriptor`: Tests name inference
     - Tests for complex output schema generation from different return types
   - `MCPRegistryTests`: Tests for the schema registration system
   - `MCPInvokersTests`: Tests for the dynamic resource and tool invocation
     - Tests for parameter mapping, type conversion, and error handling
     - Tests with different resource and tool types (parameterless, with parameters, with default values)
   - `GetSchemaCommandTests`: Integration tests for the entire schema retrieval pipeline

#### Example: Attribute-Based Command Definition

```csharp
/// <summary>
/// Command to execute C# code at runtime in the Unity Editor
/// </summary>
[MCPTool("execute_code", "Execute C# code in Unity", "execute_code(\"Debug.Log(\\\"Hello\\\"); return 42;\")")]
public static class ExecuteCodeCommand
{
    /// <summary>
    /// Execute C# code in the Unity Editor
    /// </summary>
    /// <param name="code">The C# code to execute</param>
    /// <returns>Result of the execution</returns>
    public static string Execute(
        [MCPParameter("code", "C# code to execute in the Unity environment", "string", true)] string code)
    {
        // Implementation...
    }
}
```

The system will automatically:
1. Convert the command class name from CamelCase to snake_case (e.g., `ExecuteCodeCommand` → `execute_code`)
2. Extract parameter information from the `Execute` method's parameters
3. Generate input schema based on parameter attributes
4. Infer output schema from the return type
5. Register the tool with the MCP registry

#### Example: Attribute-Based Model Definition (Legacy)

For backward compatibility, the system also supports model-based tool definitions:

```csharp
/// <summary>
/// Execute code in Unity MCP tool
/// </summary>
[MCPTool("execute_code", "Execute C# code in Unity", "execute_code(\"Debug.Log(\\\"Hello\\\"); return 42;\")")]
public class ExecuteCodeTool
{
    /// <summary>
    /// Input schema for execute_code command
    /// </summary>
    [MCPSchema("Input parameters for the execute_code command")]
    public class InputModel
    {
        /// <summary>
        /// C# code to execute
        /// </summary>
        [MCPParameter("code", "C# code to execute", "string", true)]
        public string Code { get; set; }
    }
    
    /// <summary>
    /// Output schema for execute_code command
    /// </summary>
    [MCPSchema("Output results from the execute_code command", "output")]
    public class OutputModel
    {
        /// <summary>
        /// String representation of the return value
        /// </summary>
        [MCPParameter("output", "String representation of the return value", "string", true)]
        public string Output { get; set; }
        
        // Other output parameters...
    }
}

### Schema Retrieval

Clients can retrieve the schema using the `get_schema` command, which returns a complete list of all available tools and resources with their input parameters, descriptions, and URL patterns.

### Dynamic Tool Registration

The Python MCP client can dynamically register tools and resources based on the schema received from Unity. This allows the Python side to automatically adapt to changes in the Unity API without requiring code changes. The dynamic registration process works as follows:

1. **Connection Event**: When the client successfully connects to Unity, it automatically retrieves the schema.

2. **Schema Processing**: The `DynamicToolManager` processes the schema to extract tool and resource information. This includes handling multiple possible schema formats:
   - Direct schema with tools and resources at the top level
   - Schema wrapped in a result object
   - Schema embedded in a content array as a text element
   - Various other MCP-compliant response formats

3. **Tool Registration**: For each tool in the schema, a dynamic function is created and registered with FastMCP.
   - Tools are registered with their proper input parameters and descriptions
   - The system creates properly-typed tool handlers that respect the MCP interface

4. **Resource Registration**: For each resource in the schema, it's stored in the manager's registry.
   - Resource URIs and parameters are extracted and stored
   - This allows tools to reference and use resources by name

5. **Parameter Mapping**: The dynamic functions automatically map parameters from the schema to the function signature.
   - Tool parameters are mapped from the schema definition to the actual function call
   - Type conversions are handled automatically

6. **Command Execution**: When a dynamic tool is invoked, the parameters are mapped to the command and sent to Unity.
   - The unified access_resource command is used for all resource access
   - This provides a consistent interface regardless of resource complexity

This system provides several benefits:

- **Automatic API Adaptation**: The Python client automatically adapts to changes in the Unity API.
- **No Code Generation**: No code generation or manual updates are required when new tools are added to Unity.
- **Self-Documenting**: The schema provides complete documentation of available tools and their parameters.
- **Type Safety**: Parameter types from the schema are used to validate inputs.
- **Robust Schema Parsing**: The system can handle various schema formats and extract tools/resources correctly.
- **Error Resilience**: The registration process can continue even if some tools or resources fail to register.


# All tools from Unity are now available in the MCP instance
```

## Extensibility

The system is designed to be extensible using two approaches: the traditional class-based approach and the newer container-based approach.

### Class-Based Approach

1. **Adding New Commands**: To add a new command using the traditional approach:
   - Create a new static command class ending with "Command" (e.g., `MyNewCommand`)
   - Add the `[MCPTool]` attribute with name, description, and example
   - Implement a static `Execute` method with appropriate parameters
   - Add `[MCPParameter]` attributes to method parameters for better documentation
   - Add command handling in MCPWebSocketServer.cs ProcessCommandRequest method
   - Add a matching command method in UnityWebSocketClient
   - Create a new tool implementation file in server/mcp/tools/
   - Use the unified execution pattern with execute_unity_operation
   - Register the tool in mcp/tools/__init__.py
   - The schema will be automatically generated and registered

2. **Adding New Resources**: To add a new resource using the traditional approach:
   - Create a new resource handler class ending with "Resource" (e.g., `MyNewResource`)
   - Add the `[MCPResource]` attribute with name, description, URL pattern, and example
   - Implement a static `GetResource` method with parameters matching URL pattern variables
   - Add `[MCPParameter]` attributes to method parameters for better documentation
   - Implement the resource handler in server/mcp/resources/
   - Use the unified execution pattern with execute_unity_operation
   - Register the resource in mcp/resources/__init__.py
   - The schema will be automatically generated and registered

### Container-Based Approach

The newer, more efficient container-based approach allows you to group related functionality:

1. **Creating a Container**: To create a new container for related functionality:
   - Create a static container class (e.g., `MyDomainContainer`)
   - Add the `[MCPContainer]` attribute with a prefix and description
   - Implement static methods for tools and resources in this domain
   - Each method will be registered with the container prefix
   
2. **Adding Tools to a Container**:
   - Add static methods with the `[MCPTool]` attribute
   - Implement the tool functionality directly in the method
   - Add `[MCPParameter]` attributes to method parameters
   - The tools will be automatically registered with the container prefix

3. **Adding Resources to a Container**:
   - Add static methods with the `[MCPResource]` attribute
   - Implement the resource functionality directly in the method
   - Add `[MCPParameter]` attributes to method parameters
   - The resources will be automatically registered with the container prefix

For example:

```csharp
[MCPContainer("physics", "Physics-related tools and resources")]
public static class PhysicsMcpContainer
{
    [MCPTool("raycast", "Cast a ray in the scene", "physics_raycast(0, 0, 0, 0, 1, 0, 10)")]
    public static string Raycast(
        [MCPParameter("origin_x", "Origin X coordinate", "number", true)] float originX,
        [MCPParameter("origin_y", "Origin Y coordinate", "number", true)] float originY,
        [MCPParameter("origin_z", "Origin Z coordinate", "number", true)] float originZ,
        [MCPParameter("direction_x", "Direction X component", "number", true)] float directionX,
        [MCPParameter("direction_y", "Direction Y component", "number", true)] float directionY,
        [MCPParameter("direction_z", "Direction Z component", "number", true)] float directionZ,
        [MCPParameter("max_distance", "Maximum ray distance", "number", false)] float maxDistance = 100f)
    {
        // Implementation...
    }

    [MCPResource("collision_layers", "Get collision layer information", 
                 "unity://physics/layers", "unity://physics/layers")]
    public static string GetCollisionLayers()
    {
        // Implementation...
    }
    
    // Example of a resource with parameters
    [MCPResource("raycast_layers", "Get raycast information for specific layers", 
                 "unity://physics/raycast?layerMask={layer_mask}&maxDistance={max_distance}", 
                 "unity://physics/raycast?layerMask=1&maxDistance=100")]
    public static string GetRaycastLayers(
        [MCPParameter("layer_mask", "Layer mask for raycasting", "number", true)] int layerMask,
        [MCPParameter("max_distance", "Maximum ray distance", "number", false)] float maxDistance = 100f)
    {
        // Implementation...
    }
}
```

For more details on the container-based approach, see the [MCP Container Documentation](plugin/MCP_CONTAINER_README.md).

## Architecture Diagram

The following diagram illustrates the components of the system and the communication flow between them:

```mermaid
sequenceDiagram
    participant AI as AI Client (MCP Consumer)
    participant Python as Python MCP Client
    participant Unity as Unity Plugin (TCP Server)
    AI->>Python: (1) MCP Tool/Resource Request (JSON over STDIO)<br/>e.g. execute_code_in_unity, unity://logs
    
    rect rgb(230, 245, 255)
        Python->>Unity: (2a) TCP Connection + Handshake Request<br/>"YAUM_HANDSHAKE_REQUEST"
        Unity-->>Python: (2b) Handshake Response<br/>"YAUM_HANDSHAKE_RESPONSE"
    end
    
    Python-->>Unity: (2c) Send command via TCP<br/>Framed JSON message with command & parameters
    note over Python,Unity: TCP connection with custom framing protocol (STX+LENGTH+DATA+ETX)
    Unity-->>Unity: (3) Execute command in Unity (main thread)<br/>e.g. run code or gather data
    Unity-->>Python: (4) Send Response via TCP<br/>Framed JSON result or error (incl. image data if any)
    Python-->>AI: (5) MCP Response to AI (JSON/JSON+image)<br/>Returns result or error back to caller
    
    alt Image Data Returned
        Unity-->>Python: (4b) Response with file path to saved image<br/>(Or base64 image inside JSON)
        Python-->>AI: (5b) Respond with image object in MCP format
    end
    
    rect rgba(200, 255, 200, 0.1)
        Note over AI: AI (e.g. Claude) uses MCP spec to ask Python for Unity info.
        Note over Python: Python client connects to Unity TCP server with retry mechanism.
        Note over Unity: Unity plugin hosts TCP server with simple framing protocol.
    end
    
    rect rgb(255, 240, 230)
        Python->>Unity: Periodic PING (every 30s)
        Unity-->>Python: PONG response
    end
```

In this workflow:
1. The AI client sends a request to the Python MCP client.
2. The Python client establishes a TCP connection with the Unity server:
   a. Performs a handshake exchange
   b. Sends the command as a framed JSON message
3. Unity executes the command on the main thread.
4. Unity sends the response back to the Python client via the TCP connection.
5. The Python client formats the response and sends it back to the AI client.
6. The connection is maintained with periodic ping/pong messages.

This architecture ensures clean separation of concerns and allows for real-time communication between Unity and the AI client, with Unity as the central authority hosting the TCP server. The simplified TCP-based approach avoids WebSocket implementation issues in Unity's Mono runtime while maintaining the same functionality and message flow.