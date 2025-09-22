using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DtfDeterminismAnalyzer.Utils
{
    /// <summary>
    /// Utility class for detecting orchestrator functions in Durable Task Framework code.
    /// Provides methods to identify methods that should be subject to determinism rules.
    /// </summary>
    public static class OrchestratorContextDetector
    {
        /// <summary>
        /// Known orchestration trigger attribute names that identify orchestrator functions.
        /// </summary>
        private static readonly string[] OrchestrationTriggerAttributeNames = 
        {
            "OrchestrationTrigger",
            "OrchestrationTriggerAttribute",
            "Microsoft.Azure.WebJobs.Extensions.DurableTask.OrchestrationTrigger",
            "Microsoft.Azure.WebJobs.Extensions.DurableTask.OrchestrationTriggerAttribute"
        };

        /// <summary>
        /// Known orchestration context types that indicate an orchestrator function.
        /// </summary>
        private static readonly string[] OrchestrationContextTypeNames = 
        {
            "IDurableOrchestrationContext",
            "DurableOrchestrationContextBase",
            "Microsoft.Azure.WebJobs.Extensions.DurableTask.IDurableOrchestrationContext",
            "Microsoft.Azure.WebJobs.Extensions.DurableTask.DurableOrchestrationContextBase",
            "TaskOrchestrationContext",
            "Microsoft.DurableTask.TaskOrchestrationContext"
        };

        /// <summary>
        /// Known activity trigger attribute names that identify activity functions (not orchestrators).
        /// </summary>
        private static readonly string[] ActivityTriggerAttributeNames = 
        {
            "ActivityTrigger",
            "ActivityTriggerAttribute",
            "Microsoft.Azure.WebJobs.Extensions.DurableTask.ActivityTrigger",
            "Microsoft.Azure.WebJobs.Extensions.DurableTask.ActivityTriggerAttribute"
        };

        /// <summary>
        /// Determines if a method declaration represents an orchestrator function.
        /// An orchestrator function is identified by having a parameter with the [OrchestrationTrigger] attribute
        /// or a parameter of type TaskOrchestrationContext or IDurableOrchestrationContext.
        /// </summary>
        /// <param name="methodDeclaration">The method declaration to analyze.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>True if the method is an orchestrator function, false otherwise.</returns>
        public static bool IsOrchestratorMethod(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            if (methodDeclaration?.ParameterList?.Parameters == null)
                return false;

            // Check each parameter for orchestration trigger attribute or orchestration context type
            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                // Check for [OrchestrationTrigger] attribute (Azure Functions)
                if (HasOrchestrationTriggerAttribute(parameter, semanticModel))
                {
                    return true;
                }

                // Check for TaskOrchestrationContext or IDurableOrchestrationContext parameter type
                if (HasOrchestrationContextType(parameter, semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a method declaration represents an activity function.
        /// An activity function is identified by having a parameter with the [ActivityTrigger] attribute.
        /// </summary>
        /// <param name="methodDeclaration">The method declaration to analyze.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>True if the method is an activity function, false otherwise.</returns>
        public static bool IsActivityMethod(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            if (methodDeclaration?.ParameterList?.Parameters == null)
                return false;

            // Check each parameter for activity trigger attribute
            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                if (HasActivityTriggerAttribute(parameter, semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the orchestration context parameter from an orchestrator method.
        /// </summary>
        /// <param name="methodDeclaration">The orchestrator method declaration.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>The parameter that represents the orchestration context, or null if not found.</returns>
        public static ParameterSyntax? GetOrchestrationContextParameter(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            if (methodDeclaration?.ParameterList?.Parameters == null)
                return null;

            // Find the parameter with OrchestrationTrigger attribute or orchestration context type
            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                if (HasOrchestrationTriggerAttribute(parameter, semanticModel) ||
                    HasOrchestrationContextType(parameter, semanticModel))
                {
                    return parameter;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a method is within an orchestrator function's call stack.
        /// This includes the orchestrator method itself and any methods called from within it.
        /// </summary>
        /// <param name="methodDeclaration">The method declaration to check.</param>
        /// <param name="semanticModel">The semantic model for analysis.</param>
        /// <returns>True if the method should be subject to orchestrator determinism rules.</returns>
        public static bool IsWithinOrchestratorContext(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            // First check if this method itself is an orchestrator
            if (IsOrchestratorMethod(methodDeclaration, semanticModel))
            {
                return true;
            }

            // For helper methods within orchestrator classes, we need to check if they're called from orchestrator methods
            // This is a simplified check - in a full implementation, you might want call graph analysis
            var containingClass = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass != null)
            {
                // Check if the containing class has any orchestrator methods
                var methods = containingClass.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    if (IsOrchestratorMethod(method, semanticModel))
                    {
                        // If there's an orchestrator method in the same class, this could be a helper method
                        // For now, we'll be conservative and assume helper methods in orchestrator classes
                        // should also follow determinism rules
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a syntax node is within an orchestrator method's scope.
        /// </summary>
        /// <param name="node">The syntax node to check.</param>
        /// <param name="semanticModel">The semantic model for analysis.</param>
        /// <returns>True if the node is within an orchestrator method.</returns>
        public static bool IsNodeWithinOrchestratorMethod(SyntaxNode node, SemanticModel semanticModel)
        {
            // Walk up the syntax tree to find the containing method
            var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
                return false;

            return IsWithinOrchestratorContext(containingMethod, semanticModel);
        }

        /// <summary>
        /// Checks if a parameter has an orchestration context type (TaskOrchestrationContext or IDurableOrchestrationContext).
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>True if the parameter has an orchestration context type.</returns>
        private static bool HasOrchestrationContextType(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            if (parameter.Type == null)
                return false;

            var typeInfo = semanticModel.GetTypeInfo(parameter.Type);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null)
                return false;

            return IsOrchestrationContextType(typeSymbol);
        }

        /// <summary>
        /// Checks if a parameter has an OrchestrationTrigger attribute.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>True if the parameter has an OrchestrationTrigger attribute.</returns>
        private static bool HasOrchestrationTriggerAttribute(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            if (parameter.AttributeLists.Count == 0)
                return false;

            foreach (var attributeList in parameter.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                    if (symbolInfo.Symbol is IMethodSymbol constructor)
                    {
                        var attributeType = constructor.ContainingType;
                        var attributeTypeName = attributeType.ToDisplayString();
                        
                        // Check both short name and full name
                        if (OrchestrationTriggerAttributeNames.Any(name => 
                            attributeTypeName.EndsWith(name, StringComparison.Ordinal) ||
                            attributeTypeName.Equals(name, StringComparison.Ordinal)))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Fallback to name-based checking if symbol resolution fails
                        var attributeName = attribute.Name.ToString();
                        if (OrchestrationTriggerAttributeNames.Any(name => 
                            attributeName.Equals(name, StringComparison.Ordinal) ||
                            attributeName.Equals(name.Replace("Attribute", ""), StringComparison.Ordinal)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a parameter has an ActivityTrigger attribute.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>True if the parameter has an ActivityTrigger attribute.</returns>
        private static bool HasActivityTriggerAttribute(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            if (parameter.AttributeLists.Count == 0)
                return false;

            foreach (var attributeList in parameter.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                    if (symbolInfo.Symbol is IMethodSymbol constructor)
                    {
                        var attributeType = constructor.ContainingType;
                        var attributeTypeName = attributeType.ToDisplayString();
                        
                        // Check both short name and full name
                        if (ActivityTriggerAttributeNames.Any(name => 
                            attributeTypeName.EndsWith(name, StringComparison.Ordinal) ||
                            attributeTypeName.Equals(name, StringComparison.Ordinal)))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Fallback to name-based checking if symbol resolution fails
                        var attributeName = attribute.Name.ToString();
                        if (ActivityTriggerAttributeNames.Any(name => 
                            attributeName.Equals(name, StringComparison.Ordinal) ||
                            attributeName.Equals(name.Replace("Attribute", ""), StringComparison.Ordinal)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a type is an orchestration context type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check.</param>
        /// <returns>True if the type is an orchestration context type.</returns>
        public static bool IsOrchestrationContextType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
                return false;

            var typeName = typeSymbol.ToDisplayString();
            
            // Check if it's a known orchestration context type
            if (OrchestrationContextTypeNames.Any(name => 
                typeName.Equals(name, StringComparison.Ordinal) ||
                typeName.EndsWith(name, StringComparison.Ordinal)))
            {
                return true;
            }

            // Check if it implements IDurableOrchestrationContext
            return typeSymbol.AllInterfaces.Any(i => 
                OrchestrationContextTypeNames.Any(name => 
                    i.ToDisplayString().Equals(name, StringComparison.Ordinal) ||
                    i.ToDisplayString().EndsWith(name, StringComparison.Ordinal)));
        }

        /// <summary>
        /// Gets diagnostic location information for a method that should be subject to orchestrator rules.
        /// </summary>
        /// <param name="methodDeclaration">The method declaration.</param>
        /// <returns>The location to report diagnostics for the method.</returns>
        public static Location GetMethodDiagnosticLocation(MethodDeclarationSyntax methodDeclaration)
        {
            // Report diagnostic on the method identifier
            return methodDeclaration.Identifier.GetLocation();
        }
    }
}