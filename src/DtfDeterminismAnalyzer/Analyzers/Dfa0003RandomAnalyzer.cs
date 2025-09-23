using System.Collections.Immutable;
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
        /// Initializes the analyzer by registering syntax node actions for object creation expressions.
        /// </summary>
        /// <param name="context">The analysis context to register actions with.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
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

            return false;
        }
    }
}
