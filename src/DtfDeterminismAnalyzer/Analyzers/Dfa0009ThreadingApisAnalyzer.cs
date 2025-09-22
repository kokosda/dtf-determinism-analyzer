using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using DtfDeterminismAnalyzer.Utils;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer for DFA0009: Detects threading API usage in orchestrator functions.
    /// Reports diagnostics when threading APIs like Thread, ThreadPool, Parallel, lock statements, etc.
    /// are used within Durable Task Framework orchestrator methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0009ThreadingApisAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Threading API types that should be flagged in orchestrators.
        /// </summary>
        private static readonly string[] ProblematicThreadingTypes = 
        {
            "Thread", "ThreadPool", "Parallel", "Monitor", "Mutex", "AutoResetEvent", 
            "ManualResetEvent", "ReaderWriterLock", "ReaderWriterLockSlim", "SynchronizationContext",
            "Interlocked", "SpinLock", "SpinWait", "Barrier", "CountdownEvent", "SemaphoreSlim", "Semaphore"
        };

        /// <summary>
        /// Threading API method names that should be flagged.
        /// </summary>
        private static readonly string[] ProblematicThreadingMethods = 
        {
            "Start", "QueueUserWorkItem", "ForEach", "For", "Enter", "Exit", "WaitOne", 
            "ReleaseMutex", "AcquireReaderLock", "AcquireWriterLock", "ReleaseReaderLock", 
            "ReleaseWriterLock", "Post", "Send", "Exchange", "CompareExchange", "Increment", 
            "Decrement", "Add", "Register", "Set", "Reset", "Wait", "WaitAll", "WaitAny"
        };

        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(DiagnosticDescriptors.ThreadingApisRule);

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions.
        /// </summary>
        /// <param name="context">The analysis context to register actions with.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        /// <summary>
        /// Analyzes invocation expressions to detect threading API method calls.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Check if this invocation is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
                return;

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
                if (memberSymbol != null && IsThreadingApiCall(memberSymbol))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ThreadingApisRule,
                        invocation.GetLocation(),
                        "Threading API usage detected.");

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Analyzes lock statements.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatement = (LockStatementSyntax)context.Node;

            // Check if this lock statement is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(lockStatement, context.SemanticModel))
                return;

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ThreadingApisRule,
                lockStatement.GetLocation(),
                "Threading API usage detected.");

            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Analyzes object creation expressions to detect threading object instantiation.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            // Check if this object creation is within an orchestrator method
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(objectCreation, context.SemanticModel))
                return;

            // Get type information for the object being created
            var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;
            if (typeSymbol == null)
                return;

            // Check if this is a threading-related type
            if (IsThreadingType(typeSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ThreadingApisRule,
                    objectCreation.GetLocation(),
                    "Threading API usage detected.");

                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determines if a symbol represents a threading API call.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns>True if the symbol represents a threading API call.</returns>
        private static bool IsThreadingApiCall(ISymbol symbol)
        {
            var containingType = symbol.ContainingType;
            if (containingType == null)
                return false;

            // Check if it's a method from a threading type
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();
            
            if ((namespaceName == "System.Threading" || namespaceName == "System.Threading.Tasks") &&
                ProblematicThreadingTypes.Contains(typeName) &&
                ProblematicThreadingMethods.Contains(symbol.Name))
            {
                return true;
            }

            // Special case for CancellationToken.Register (in System namespace)
            if (containingType.Name == "CancellationToken" &&
                containingType.ContainingNamespace?.ToDisplayString() == "System.Threading" &&
                symbol.Name == "Register")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a type is a threading-related type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check.</param>
        /// <returns>True if the type is threading-related.</returns>
        private static bool IsThreadingType(ITypeSymbol typeSymbol)
        {
            var typeName = typeSymbol.Name;
            var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
            
            return (namespaceName == "System.Threading" || namespaceName == "System.Threading.Tasks") &&
                   ProblematicThreadingTypes.Contains(typeName);
        }
    }
}
