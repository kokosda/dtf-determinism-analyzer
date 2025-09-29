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
    /// Analyzer for DFA0001: Detects non-deterministic DateTime API usage in orchestrator functions.
    /// Reports diagnostics when DateTime.Now, DateTime.UtcNow, DateTime.Today, or Stopwatch APIs 
    /// are used within Durable Task Framework orchestrator methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0001TimeApiAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Non-deterministic DateTime member names that should be flagged in orchestrators.
        /// </summary>
        private static readonly string[] ProblematicDateTimeMembers =
        [
            "Now",
            "UtcNow",
            "Today"
        ];

        /// <summary>
        /// Non-deterministic Stopwatch member names that should be flagged in orchestrators.
        /// </summary>
        private static readonly string[] ProblematicStopwatchMembers =
        [
            "StartNew",
            "Start",
            "GetTimestamp"
        ];

        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.TimeApiRule);

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions for member access expressions.
        /// </summary>
        /// <param name="context">The analysis context to register actions with.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// Analyzes member access expressions to detect problematic DateTime and Stopwatch usage.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Check if this member access is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(memberAccess, context.SemanticModel))
            {
                return;
            }

            // Get the symbol information for the member access
            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol == null)
            {
                return;
            }

            // Check for problematic DateTime or DateTimeOffset members
            if (IsDateTimeMember(memberSymbol) || IsDateTimeOffsetMember(memberSymbol))
            {
                string memberName = memberSymbol.Name;
                if (ProblematicDateTimeMembers.Contains(memberName))
                {
                    string typeName = memberSymbol.ContainingType?.Name ?? "DateTime";
                    Diagnostic diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.TimeApiRule,
                        memberAccess.GetLocation(),
                        $"{typeName}.{memberName}");

                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Check for Stopwatch properties like ElapsedMilliseconds (not method calls)
            if (IsStopwatchMember(memberSymbol) && memberSymbol is IPropertySymbol)
            {
                string memberName = memberSymbol.Name;
                if (IsStopwatchTimingProperty(memberName))
                {
                    Diagnostic diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.TimeApiRule,
                        memberAccess.GetLocation(),
                        $"Stopwatch.{memberName}");

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Analyzes invocation expressions to detect problematic Stopwatch method calls.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

            // Check if this invocation is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
            {
                return;
            }

            // Check for Stopwatch method calls (both static and instance)
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.ContainingType?.Name == "Stopwatch" &&
                    methodSymbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System.Diagnostics")
                {
                    string methodName = methodSymbol.Name;
                    if (ProblematicStopwatchMembers.Contains(methodName))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.TimeApiRule,
                            invocation.GetLocation(),
                            $"Stopwatch.{methodName}");

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a symbol represents a DateTime member.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns>True if the symbol is a DateTime member.</returns>
        private static bool IsDateTimeMember(ISymbol symbol)
        {
            return symbol.ContainingType?.Name == "DateTime" &&
                   symbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System";
        }

        /// <summary>
        /// Determines if a symbol represents a DateTimeOffset member.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns>True if the symbol is a DateTimeOffset member.</returns>
        private static bool IsDateTimeOffsetMember(ISymbol symbol)
        {
            return symbol.ContainingType?.Name == "DateTimeOffset" &&
                   symbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System";
        }

        /// <summary>
        /// Determines if a symbol represents a Stopwatch member.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns>True if the symbol is a Stopwatch member.</returns>
        private static bool IsStopwatchMember(ISymbol symbol)
        {
            return symbol.ContainingType?.Name == "Stopwatch" &&
                   symbol.ContainingType.ContainingNamespace?.ToDisplayString() == "System.Diagnostics";
        }

        /// <summary>
        /// Determines if a member name represents a Stopwatch timing property that is non-deterministic.
        /// </summary>
        /// <param name="memberName">The member name to check.</param>
        /// <returns>True if the member represents timing information.</returns>
        private static bool IsStopwatchTimingProperty(string memberName)
        {
            return memberName is "ElapsedMilliseconds" or
                   "ElapsedTicks" or
                   "Elapsed";
        }
    }
}
