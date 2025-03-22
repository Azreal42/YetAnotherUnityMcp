# MCP Container-Based Approach

## Overview

This document describes the new container-based approach for organizing MCP tools and resources in YetAnotherUnityMcp. The container-based approach reduces code duplication and improves organization by allowing multiple related tools and resources to be defined as methods within a single class.

## Key Concepts

### MCPContainer Attribute

The `MCPContainer` attribute marks a class as a container for MCP tools and resources. It allows for:

- Grouping related functionality within a single class
- Applying a common name prefix to all tools and resources in the container
- Providing a class-level description

Example:
```csharp
[MCPContainer("editor_", "Container for Unity Editor-related MCP functionality")]
public static class EditorMcpContainer
{
    // Tools and resources go here
}
```

### Method-Level MCPTool and MCPResource Attributes

In the container-based approach, `MCPTool` and `MCPResource` attributes are applied to methods rather than classes:

```csharp
[MCPTool("execute_code", "Execute C# code in Unity", "editor_execute_code(\"Debug.Log(\\\"Hello\\\"); return 42;\")")]
public static string ExecuteCode([MCPParameter("code", "C# code to execute", "string", true)] string code)
{
    // Implementation here
}

[MCPResource("unity_info", "Get information about the Unity environment", "unity://info", "unity://info")]
public static string GetUnityInfo()
{
    // Implementation here
}
```

### Container Organization

Containers should be organized to group related functionality:

- `EditorMcpContainer`: Editor-related tools and resources (screenshots, code execution, etc.)
- `ObjectMcpContainer`: GameObject-related tools and resources (modification, inspection, etc.)
- `SceneMcpContainer`: Scene-related tools and resources (loading, saving, etc.)

## Using Containers

### Creating a new Container

1. Create a new class file in the appropriate namespace
2. Apply the `MCPContainer` attribute to the class
3. Add static methods with `MCPTool` or `MCPResource` attributes
4. Use regions to organize related functionality

Example:
```csharp
using YetAnotherUnityMcp.Editor.Models;

namespace YetAnotherUnityMcp.Editor.Commands
{
    [MCPContainer("scene_", "Tools and resources for scene management")]
    public static class SceneMcpContainer
    {
        #region Tools

        [MCPTool("load", "Load a scene by name", "scene_load(scene_name=\"MyScene\")")]
        public static string LoadScene(
            [MCPParameter("scene_name", "Name of the scene to load", "string", true)] string sceneName)
        {
            // Implementation
        }

        #endregion

        #region Resources

        [MCPResource("list", "Get a list of available scenes", "unity://scene/list", "unity://scene/list")]
        public static string GetSceneList()
        {
            // Implementation
        }

        #endregion
    }
}
```

### Backward Compatibility

The system maintains backward compatibility with the existing class-based approach. Both approaches can coexist, but the recommended approach is to use containers for new functionality.

For backward compatibility, legacy class-based tools and resources now forward to their container-based equivalents. This allows for a gradual transition to the container-based approach.

## Registration Process

The `MCPRegistry` class has been enhanced to scan for and register both class-based and method-based tools and resources:

1. Class-based registration: Classes with `MCPTool` or `MCPResource` attributes
2. Method-based registration: Methods with `MCPTool` or `MCPResource` within classes with `MCPContainer` attributes

The registry tracks:
- For class-based: The class type
- For method-based: The method info, container type, and container instance

## Invocation Process

The `ToolInvoker` and `ResourceInvoker` classes have been enhanced to support both approaches:

1. For method-based: Directly invoke the method from its method info and container instance
2. For class-based: Find the class, get the appropriate method (Execute/GetResource), and invoke it

## Best Practices

1. Group related tools and resources in a single container
2. Use descriptive method names that clearly indicate their purpose
3. Use regions to organize methods within a container
4. Apply consistent naming conventions for tools and resources
5. Use the container's name prefix to avoid name collisions
6. Document all tools, resources, and parameters thoroughly
7. Use parameters with clear types and descriptions

## Migration Guide

To migrate from class-based to container-based:

1. Create or identify an appropriate container class
2. Move the implementation from the Execute/GetResource method to a new method in the container
3. Apply the `MCPTool` or `MCPResource` attribute to the new method
4. Update the legacy class to forward to the new method (for backward compatibility)

Example:
```csharp
// Original class-based approach
[MCPTool("execute_code", "Execute C# code in Unity", "execute_code(\"Debug.Log(\\\"Hello\\\"); return 42;\")")]
public static class ExecuteCodeCommand
{
    public static string Execute([MCPParameter("code", "C# code to execute", "string", true)] string code)
    {
        // Original implementation
    }
}

// Migrated container-based approach
[MCPContainer("editor_", "Container for Unity Editor-related MCP functionality")]
public static class EditorMcpContainer
{
    [MCPTool("execute_code", "Execute C# code in Unity", "editor_execute_code(\"Debug.Log(\\\"Hello\\\"); return 42;\")")]
    public static string ExecuteCode([MCPParameter("code", "C# code to execute", "string", true)] string code)
    {
        // Implementation moved here
    }
}

// Updated legacy class for backward compatibility
public static class ExecuteCodeCommand
{
    public static string Execute([MCPParameter("code", "C# code to execute", "string", true)] string code)
    {
        // Forward to container-based implementation
        return EditorMcpContainer.ExecuteCode(code);
    }
}
```