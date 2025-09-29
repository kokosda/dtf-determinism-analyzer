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
    /// Analyzer for DFA0003: Detects non-deterministic Random usage in orchestrator functions.
    /// Reports diagnostics when Random() is constructed without a fixed seed within Durable Task Framework orchestrator methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0003RandomAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.RandomRule);

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions for object creation expressions and member access expressions.
        /// </summary>
        /// <param name="context">The analysis context to register actions with.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        /// <summary>
        /// Analyzes object creation expressions to detect non-deterministic Random instantiation.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            // Check if this object creation is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(objectCreation, context.SemanticModel))
            {
                return;
            }

            // Get type information for the object being created
            ITypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;
            if (typeSymbol == null)
            {
                return;
            }

            // Check if this is a Random type
            if (typeSymbol.Name == "Random" &&
                typeSymbol.ContainingNamespace?.ToDisplayString() == "System")
            {
                // Check if the constructor has no arguments (non-deterministic)
                if (objectCreation.ArgumentList == null ||
                    objectCreation.ArgumentList.Arguments.Count == 0)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.RandomRule,
                        objectCreation.GetLocation(),
                        "Non-deterministic random used in orchestrator.");

                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    // Check if the seed argument is non-deterministic (e.g., DateTime.Now.Millisecond)
                    ArgumentSyntax firstArgument = objectCreation.ArgumentList.Arguments[0];
                    if (IsNonDeterministicSeed(firstArgument, context.SemanticModel))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.RandomRule,
                            objectCreation.GetLocation(),
                            "Non-deterministic random used in orchestrator.");

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a seed argument is non-deterministic.
        /// </summary>
        /// <param name="argument">The argument expression to check.</param>
        /// <param name="semanticModel">The semantic model for analysis.</param>
        /// <returns>True if the seed is non-deterministic.</returns>
        private static bool IsNonDeterministicSeed(ArgumentSyntax argument, SemanticModel semanticModel)
        {
            // Check for DateTime.Now, DateTime.UtcNow usage in seed
            if (argument.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                ISymbol? memberSymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
                if (memberSymbol?.ContainingType?.Name == "DateTime" &&
                    memberSymbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System")
                {
                    string memberName = memberSymbol.Name;
                    if (memberName is "Now" or "UtcNow" or "Today")
                    {
                        return true;
                    }
                }
            }

            // Check for chained calls like DateTime.Now.Millisecond
            if (argument.Expression is MemberAccessExpressionSyntax chainedAccess &&
                chainedAccess.Expression is MemberAccessExpressionSyntax parentAccess)
            {
                ISymbol? parentSymbol = semanticModel.GetSymbolInfo(parentAccess).Symbol;
                if (parentSymbol?.ContainingType?.Name == "DateTime" &&
                    parentSymbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System")
                {
                    string parentName = parentSymbol.Name;
                    if (parentName is "Now" or "UtcNow" or "Today")
                    {
                        return true;
                    }
                }
            }

            // Check for identifier that could be a variable with non-deterministic value
            // For example: var seed = DateTime.Now.Millisecond; var random = new Random(seed);
            if (argument.Expression is IdentifierNameSyntax identifier)
            {
                var identifierSymbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                if (identifierSymbol is ILocalSymbol localSymbol)
                {
                    // Try to find the local variable declaration and check its initializer
                    var syntaxReference = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxReference != null)
                    {
                        var declarationSyntax = syntaxReference.GetSyntax();
                        if (declarationSyntax is VariableDeclaratorSyntax variableDeclarator &&
                            variableDeclarator.Initializer?.Value != null)
                        {
                            // Check if the initializer involves DateTime.Now.Millisecond
                            return IsNonDeterministicExpression(variableDeclarator.Initializer.Value, semanticModel);
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if an expression is non-deterministic (e.g., DateTime.Now.Millisecond).
        /// </summary>
        /// <param name="expression">The expression to check.</param>
        /// <param name="semanticModel">The semantic model for analysis.</param>
        /// <returns>True if the expression is non-deterministic.</returns>
        private static bool IsNonDeterministicExpression(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // Check for DateTime.Now.Millisecond pattern
            if (expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is MemberAccessExpressionSyntax parentAccess)
            {
                ISymbol? parentSymbol = semanticModel.GetSymbolInfo(parentAccess).Symbol;
                if (parentSymbol?.ContainingType?.Name == "DateTime" &&
                    parentSymbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System")
                {
                    string parentName = parentSymbol.Name;
                    if (parentName is "Now" or "UtcNow" or "Today")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Analyzes member access expressions to detect non-deterministic Random method calls.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Check if this member access is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(memberAccess, context.SemanticModel))
            {
                return;
            }

            // Get symbol information for the member being accessed
            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            // Check if this is a Random type method (Next, NextDouble, NextBytes, etc.)
            if (methodSymbol.ContainingType?.Name == "Random" &&
                methodSymbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System")
            {
                string methodName = methodSymbol.Name;
                if (methodName is "Next" or "NextDouble" or "NextBytes" or "NextSingle" or "NextInt64")
                {
                    // Check if this is accessing a static/instance field that may be non-deterministic
                    if (memberAccess.Expression is IdentifierNameSyntax identifier)
                    {
                        var identifierSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                        if (identifierSymbol is IFieldSymbol fieldSymbol)
                        {
                            // Check if this Random field was initialized non-deterministically
                            // We need to find the field declaration and check its initializer
                            if (IsFieldNonDeterministic(fieldSymbol, context.SemanticModel))
                            {
                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.RandomRule,
                                    memberAccess.GetLocation(),
                                    "Non-deterministic random used in orchestrator.");

                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a Random field was initialized non-deterministically.
        /// </summary>
        /// <param name="fieldSymbol">The field symbol to check.</param>
        /// <param name="semanticModel">The semantic model for analysis.</param>
        /// <returns>True if the field is non-deterministic.</returns>
        private static bool IsFieldNonDeterministic(IFieldSymbol fieldSymbol, SemanticModel semanticModel)
        {
            // Get the field declaration syntax
            var syntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference == null)
                return false;

            var fieldDeclarationSyntax = syntaxReference.GetSyntax();
            if (fieldDeclarationSyntax is not VariableDeclaratorSyntax variableDeclarator)
                return false;

            // Check if the field has an initializer
            if (variableDeclarator.Initializer?.Value is ObjectCreationExpressionSyntax objectCreation)
            {
                // Check if this is Random() constructor with no arguments (non-deterministic)
                if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count == 0)
                {
                    return true;
                }
                
                // Check if the seed argument is non-deterministic
                if (objectCreation.ArgumentList.Arguments.Count > 0)
                {
                    ArgumentSyntax firstArgument = objectCreation.ArgumentList.Arguments[0];
                    return IsNonDeterministicSeed(firstArgument, semanticModel);
                }
            }

            return false;
        }
    }
}
