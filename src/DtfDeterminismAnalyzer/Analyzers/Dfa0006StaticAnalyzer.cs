using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using DtfDeterminismAnalyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects static field/property access in orchestrator functions.
    /// Identifies access to static mutable state that can cause non-deterministic behavior.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0006StaticAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.StaticStateRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // Use CompilationStartAction to maintain state across the compilation
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var reportedStaticFields = new ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol StaticField), bool>();
                
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeMemberAccess(ctx, reportedStaticFields), SyntaxKind.SimpleMemberAccessExpression);
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeIdentifierName(ctx, reportedStaticFields), SyntaxKind.IdentifierName);
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeAssignmentExpression(ctx, reportedStaticFields), SyntaxKind.SimpleAssignmentExpression);
            });
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol StaticField), bool> reportedStaticFields)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(memberAccess, context.SemanticModel))
            {
                return;
            }

            // Check the member being accessed (right side of the dot)
            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol != null && IsStaticMutableAccess(memberSymbol))
            {
                MethodDeclarationSyntax? orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    (MethodDeclarationSyntax orchestratorMethod, ISymbol memberSymbol) key = (orchestratorMethod, memberSymbol);
                    if (reportedStaticFields.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.StaticStateRule,
                            memberAccess.GetLocation()));
                    }
                }
            }

            // Also check the expression being accessed (left side of the dot) - this handles cases like _staticField.SomeProperty
            if (memberAccess.Expression is IdentifierNameSyntax identifier)
            {
                ISymbol? expressionSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if (expressionSymbol != null && IsStaticMutableAccess(expressionSymbol))
                {
                    MethodDeclarationSyntax? orchestratorMethod = GetContainingOrchestratorMethod(identifier);
                    if (orchestratorMethod != null)
                    {
                        (MethodDeclarationSyntax orchestratorMethod, ISymbol expressionSymbol) key = (orchestratorMethod, expressionSymbol);
                        if (reportedStaticFields.TryAdd(key, true))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.StaticStateRule,
                                identifier.GetLocation()));
                        }
                    }
                }
            }
        }

        private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol StaticField), bool> reportedStaticFields)
        {
            var identifier = (IdentifierNameSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(identifier, context.SemanticModel))
            {
                return;
            }

            // Skip if this identifier is part of a member access (handled by AnalyzeMemberAccess)
            if (identifier.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                // Only skip if this identifier is the Expression part (left side), not the Name part (right side)
                if (memberAccess.Expression == identifier)
                {
                    return;
                }
            }

            // Skip if this identifier is part of an assignment's left side (handled by AnalyzeAssignmentExpression)
            if (identifier.Parent is AssignmentExpressionSyntax assignment && assignment.Left == identifier)
            {
                return;
            }

            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
            if (symbolInfo.Symbol == null)
            {
                return;
            }

            // Check if this is direct static field or property access
            if (IsStaticMutableAccess(symbolInfo.Symbol))
            {
                MethodDeclarationSyntax? orchestratorMethod = GetContainingOrchestratorMethod(identifier);
                if (orchestratorMethod != null)
                {
                    (MethodDeclarationSyntax orchestratorMethod, ISymbol Symbol) key = (orchestratorMethod, symbolInfo.Symbol);
                    if (reportedStaticFields.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.StaticStateRule,
                            identifier.GetLocation()));
                    }
                }
            }
        }

        private static void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol StaticField), bool> reportedStaticFields)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(assignment, context.SemanticModel))
            {
                return;
            }

            ISymbol? leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (leftSymbol == null)
            {
                return;
            }

            // Check if assigning to static mutable state
            if (IsStaticMutableAccess(leftSymbol))
            {
                MethodDeclarationSyntax? orchestratorMethod = GetContainingOrchestratorMethod(assignment);
                if (orchestratorMethod != null)
                {
                    (MethodDeclarationSyntax orchestratorMethod, ISymbol leftSymbol) key = (orchestratorMethod, leftSymbol);
                    if (reportedStaticFields.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.StaticStateRule,
                            assignment.Left.GetLocation()));
                    }
                }
            }
        }

        private static bool IsStaticMutableAccess(ISymbol symbol)
        {
            return symbol switch
            {
                IFieldSymbol field => IsStaticMutableField(field),
                IPropertySymbol property => IsStaticMutableProperty(property),
                _ => false
            };
        }

        private static bool IsStaticMutableField(IFieldSymbol field)
        {
            // Must be static
            if (!field.IsStatic)
            {
                return false;
            }

            // Skip const fields (they are immutable by definition)
            if (field.IsConst)
            {
                return false;
            }

            // Skip readonly fields that are primitives or known immutable types
            if (field.IsReadOnly && IsKnownImmutableType(field.Type))
            {
                return false;
            }

            // Check if it's a well-known safe static field
            if (IsKnownSafeStaticField(field))
            {
                return false;
            }

            // All other static fields are potentially mutable and problematic
            return true;
        }

        private static bool IsStaticMutableProperty(IPropertySymbol property)
        {
            // Must be static
            if (!property.IsStatic)
            {
                return false;
            }

            // Check if it's a well-known safe static property
            if (IsKnownSafeStaticProperty(property))
            {
                return false;
            }

            // Properties with only get accessor and known immutable return types are safe
            if (property.SetMethod == null && IsKnownImmutableType(property.Type))
            {
                return false;
            }

            // All other static properties are potentially problematic
            return true;
        }

        private static bool IsKnownImmutableType(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            string typeName = type.Name;
            string? namespaceName = type.ContainingNamespace?.ToDisplayString();

            // Check for primitive types and known immutable types
            return typeName switch
            {
                "String" or "Int32" or "Int64" or "Double" or "Single" or "Boolean" or
                "Byte" or "SByte" or "Int16" or "UInt16" or "UInt32" or "UInt64" or
                "Char" or "Decimal" when namespaceName == "System" => true,

                "DateTime" or "DateTimeOffset" or "TimeSpan" or "Guid"
                when namespaceName == "System" => true,

                "Version" when namespaceName == "System" => true,

                // Readonly collection interfaces are considered immutable
                "IReadOnlyList" or "IReadOnlyCollection" or "IReadOnlyDictionary" or
                "IReadOnlySet" when namespaceName == "System.Collections.Generic" => true,

                "IEnumerable" when namespaceName == "System.Collections" => true,
                "IEnumerable" when namespaceName == "System.Collections.Generic" => true,

                _ => false
            };
        }

        private static bool IsKnownSafeStaticField(IFieldSymbol field)
        {
            INamedTypeSymbol? containingType = field.ContainingType;
            string? typeName = containingType?.Name;
            string? namespaceName = containingType?.ContainingNamespace?.ToDisplayString();

            // Known safe static fields that are commonly used
            if (typeName == "String" && namespaceName == "System")
            {
                return field.Name is "Empty";
            }

            if (typeName == "Guid" && namespaceName == "System")
            {
                return field.Name is "Empty";
            }

            if (typeName == "TimeSpan" && namespaceName == "System")
            {
                return field.Name is "Zero" or "MaxValue" or "MinValue";
            }

            if (typeName == "DateTime" && namespaceName == "System")
            {
                return field.Name is "MaxValue" or "MinValue";
            }

            // Add more known safe static fields as needed
            return false;
        }

        private static bool IsKnownSafeStaticProperty(IPropertySymbol property)
        {
            INamedTypeSymbol? containingType = property.ContainingType;
            string? typeName = containingType?.Name;
            string? namespaceName = containingType?.ContainingNamespace?.ToDisplayString();

            // Known safe static properties
            if (typeName == "Type" && namespaceName == "System")
            {
                return property.Name is "EmptyTypes";
            }

            if (typeName == "Array" && namespaceName == "System")
            {
                return property.Name is "Empty"; // Array.Empty<T>()
            }

            if (typeName == "Task" && namespaceName == "System.Threading.Tasks")
            {
                return property.Name is "CompletedTask";
            }

            // CancellationToken static properties are generally safe
            if (typeName == "CancellationToken" && namespaceName == "System.Threading")
            {
                return property.Name is "None";
            }

            // Add more known safe static properties as needed
            return false;
        }

        /// <summary>
        /// Finds the orchestrator method that contains the given syntax node.
        /// An orchestrator method is one that has the [OrchestrationTrigger] attribute.
        /// </summary>
        private static MethodDeclarationSyntax? GetContainingOrchestratorMethod(SyntaxNode node)
        {
            // First find any containing method
            MethodDeclarationSyntax? containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
            {
                return null;
            }

            // Check if this method has the [OrchestrationTrigger] attribute
            if (HasOrchestrationTriggerAttribute(containingMethod))
            {
                return containingMethod;
            }

            // If not, look for other methods in the same class that have the [OrchestrationTrigger] attribute
            // This handles cases where static field access is in helper methods within the orchestrator class
            ClassDeclarationSyntax? containingClass = containingMethod.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass != null)
            {
                foreach (MethodDeclarationSyntax method in containingClass.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (HasOrchestrationTriggerAttribute(method))
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a method has the [OrchestrationTrigger] attribute
        /// </summary>
        private static bool HasOrchestrationTriggerAttribute(MethodDeclarationSyntax method)
        {
            foreach (ParameterSyntax parameterList in method.ParameterList.Parameters)
            {
                foreach (AttributeListSyntax attributeList in parameterList.AttributeLists)
                {
                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        string attributeName = attribute.Name.ToString();
                        if (attributeName.Contains("OrchestrationTrigger"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
