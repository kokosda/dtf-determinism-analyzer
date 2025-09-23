using System.Collections.Immutable;
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
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeAssignmentExpression, SyntaxKind.SimpleAssignmentExpression);
        }

        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(memberAccess, context.SemanticModel))
            {
                return;
            }

            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol == null)
            {
                return;
            }

            // Check if this is static field or property access
            if (IsStaticMutableAccess(memberSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StaticStateRule,
                    memberAccess.GetLocation(),
                    $"Static state access '{memberAccess}' detected"));
            }
        }

        private void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifier = (IdentifierNameSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(identifier, context.SemanticModel))
            {
                return;
            }

            // Skip if this identifier is part of a member access (handled separately)
            if (identifier.Parent is MemberAccessExpressionSyntax)
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
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StaticStateRule,
                    identifier.GetLocation(),
                    $"Static state access '{identifier}' detected"));
            }
        }

        private void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context)
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
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StaticStateRule,
                    assignment.Left.GetLocation(),
                    $"Static state modification '{assignment.Left}' detected"));
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
    }
}
