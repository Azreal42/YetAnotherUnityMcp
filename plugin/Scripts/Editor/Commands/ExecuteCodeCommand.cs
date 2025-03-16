using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Microsoft.CSharp;
using System.Threading.Tasks;
using UnityEditor;

namespace YetAnotherUnityMcp.Editor.Commands
{
    /// <summary>
    /// Command to execute C# code at runtime in the Unity Editor
    /// </summary>
    public static class ExecuteCodeCommand
    {
        private const string ClassTemplate = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using Object = UnityEngine.Object;

namespace YetAnotherUnityMcp.Runtime
{{
    public class CodeExecutor
    {{
        public static object Execute()
        {{
            try
            {{
                {0}
            }}
            catch (Exception ex)
            {{
                return $""Error: {{ex.Message}}\nStackTrace: {{ex.StackTrace}}"";
            }}
            
            return ""Code executed successfully"";
        }}
    }}
}}";

        /// <summary>
        /// Execute C# code in the Unity Editor
        /// </summary>
        /// <param name="code">The C# code to execute</param>
        /// <returns>Result of the execution</returns>
        public static string Execute(string code)
        {
            try
            {
                // Prepare the code
                string codeToCompile = string.Format(ClassTemplate, code);
                
                // Create compiler parameters
                var parameters = new System.CodeDom.Compiler.CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false
                };
                
                // Add references to Unity assemblies
                parameters.ReferencedAssemblies.Add(typeof(GameObject).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(Debug).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(System.Linq.Enumerable).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(List<>).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(EditorWindow).Assembly.Location);
                
                // Compile the code
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerResults results = provider.CompileAssemblyFromSource(parameters, codeToCompile);
                
                // Check for compilation errors
                if (results.Errors.HasErrors)
                {
                    StringBuilder errorMessage = new StringBuilder("Compilation errors occurred:\n");
                    foreach (CompilerError error in results.Errors)
                    {
                        errorMessage.AppendLine($"Line {error.Line}: {error.ErrorText}");
                    }
                    return errorMessage.ToString();
                }
                
                // Execute the code
                Assembly assembly = results.CompiledAssembly;
                Type type = assembly.GetType("YetAnotherUnityMcp.Runtime.CodeExecutor");
                MethodInfo method = type.GetMethod("Execute");
                
                object result = method.Invoke(null, null);
                return result?.ToString() ?? "Code executed with null result";
            }
            catch (Exception ex)
            {
                return $"Error executing code: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }
    }
}