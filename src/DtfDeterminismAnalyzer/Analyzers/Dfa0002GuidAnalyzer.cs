using System.Collections.Immutable;
using DtfDeterminismAnalyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer for DFA0002: Detects non-deterministic GUID generation in orchestrator functions.
    /// Reports diagnostics when Guid.NewGuid() is used within Durable Task Framework orchestrator methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0002GuidAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.GuidRule);

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions for member access expressions.
        /// </summary>
        /// <param name="context">The analysis context to register actions with.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// Analyzes invocation expressions to detect Guid.NewGuid() calls.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Check if this invocation is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
            {
                return;
            }

            // Check for Guid.NewGuid() calls
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
                if (memberSymbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Name == "NewGuid" &&
                    methodSymbol.ContainingType?.Name == "Guid" &&
                    methodSymbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System")
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.GuidRule,
                        invocation.GetLocation(),
                        "Non-deterministic GUID generated in orchestrator.");

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
