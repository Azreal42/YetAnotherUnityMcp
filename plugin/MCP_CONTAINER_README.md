# Container-Based MCP Implementation

## Overview

The YetAnotherUnityMcp plugin now supports a container-based approach for organizing MCP tools and resources. This approach allows related functionality to be grouped together in container classes, reducing code duplication and improving organization.

## Key Benefits

- **Reduced Boilerplate**: Define multiple tools and resources in a single container class
- **Improved Organization**: Group related functionality logically
- **Prefix Support**: Automatically prefix tool/resource names with container name
- **Clean Separation**: Separate implementation from registration
- **Compatible**: Works alongside the existing class-based approach

## How It Works

The container-based approach uses reflection to scan classes marked with the `[MCPContainer]` attribute and automatically register methods marked with `[MCPTool]` or `[MCPResource]` attributes.

### Container Attribute

```csharp
[MCPContainer("editor", "Editor-related tools and resources")]
public static class EditorMcpContainer
{
    // Tool and resource methods go here
}
```

The `MCPContainer` attribute takes two parameters:
- **Name**: Prefix for all tools and resources in this container
- **Description**: Description of the container's purpose

### Tool Methods

```csharp
[MCPTool("execute_code", "Execute C# code in Unity", "editor_execute_code(\"Debug.Log(\\\"Hello\\\");\")")]
public static string ExecuteCode(
    [MCPParameter("code", "C# code to execute", "string", true)] string code)
{
    // Implementation
}
```

### Resource Methods

```csharp
[MCPResource("logs", "Get Unity logs", "unity://logs?level={level}", "unity://logs?level=Warning")]
public static string GetLogs(
    [MCPParameter("level", "Log level filter", "string", false)] string level = "All")
{
    // Implementation
}
```

## Usage Example

Here's a complete example of a container class:

```csharp
using System;
using UnityEngine;
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Containers
{
    [MCPContainer("scene", "Scene-related tools and resources")]
    public static class SceneMcpContainer
    {
        [MCPResource("active", "Get active scene info", "unity://scene/active", "unity://scene/active")]
        public static string GetActiveScene()
        {
            // Implementation
        }
        
        [MCPTool("load", "Load a scene", "scene_load(\"MainScene\")")]
        public static string LoadScene(
            [MCPParameter("name", "Scene name", "string", true)] string sceneName)
        {
            // Implementation
        }
    }
}
```

In this example:
- The container prefix is "scene"
- The tools will be registered as "scene_active" and "scene_load"
- The resource URI pattern will be "unity://scene/active"

## Migration Guide

### From Class-Based to Container-Based

1. Create a container class with the `[MCPContainer]` attribute
2. Move your implementation from `Execute` and `GetResource` methods to container methods
3. Add `[MCPTool]` or `[MCPResource]` attributes to methods
4. Add `[MCPParameter]` attributes to method parameters

### Legacy Support

The existing class-based approach continues to work, allowing for a gradual migration:

```csharp
// Legacy class-based approach
[MCPTool("execute_code", "Execute C# code in Unity", "execute_code(\"Debug.Log(\\\"Hello\\\");\")")]
public static class ExecuteCodeCommand
{
    public static string Execute(string code)
    {
        // Implementation
    }
}
```

## Best Practices

1. **Organize by Domain**: Group related tools and resources in the same container
2. **Use Clear Prefixes**: Choose container names that make the tool/resource purpose clear
3. **Namespace Organization**:
   - Put resource containers in the `Models` namespace
   - Put tool containers in the `Commands` namespace
4. **Keep Methods Focused**: Each method should do one thing well
5. **Proper Error Handling**: Handle exceptions and return formatted error responses
6. **Rich Documentation**: Use XML documentation for all container classes and methods

## Examples in the Codebase

- `EditorMcpContainer`: Editor-specific tools and resources
- `ObjectMcpContainer`: GameObject manipulation tools and resources
- `SceneMcpContainer`: Scene management tools and resources

## Implementation Details

### Workflow

1. The `MCPRegistry` scans for container classes on initialization
2. For each container, it extracts tool and resource methods
3. It registers descriptors with prefixed names
4. During invocation, the appropriate method is called directly

### Technical Components

- `MCPContainerAttribute`: Marks a class as an MCP container
- `RegisterMethodsFromContainer`: Scans and registers methods from a container
- `MCPToolAttribute` and `MCPResourceAttribute`: Now support both class and method targets
- `MCPParameterAttribute`: Documents method parameters

## Testing

The container-based approach includes comprehensive unit tests:
- `MCPContainerTests.cs`: Tests for container registration and invocation
- `MCPRegistryTests.cs`: Tests for registry functionality
- `MCPInvokersTests.cs`: Tests for invoker functionality

## Limitations

1. Currently only supports static methods
2. Container name must be unique across the application
3. Method names must be unique within a container

## Future Enhancements

1. Instance method support
2. Dependency injection for containers
3. Async/await support for long-running operations
4. Auto-generation of Python client code