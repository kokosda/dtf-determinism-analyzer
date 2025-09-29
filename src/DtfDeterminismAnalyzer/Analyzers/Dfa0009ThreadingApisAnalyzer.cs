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
        [
            "Thread", "ThreadPool", "Parallel", "Monitor", "Mutex", "AutoResetEvent",
            "ManualResetEvent", "ReaderWriterLock", "ReaderWriterLockSlim", "SynchronizationContext",
            "Interlocked", "SpinLock", "SpinWait", "Barrier", "CountdownEvent", "SemaphoreSlim", "Semaphore"
        ];

        /// <summary>
        /// Threading API method names that should be flagged.
        /// </summary>
        private static readonly string[] ProblematicThreadingMethods =
        [
            "Start", "QueueUserWorkItem", "ForEach", "For", "Enter", "Exit", "WaitOne",
            "ReleaseMutex", "AcquireReaderLock", "AcquireWriterLock", "ReleaseReaderLock",
            "ReleaseWriterLock", "Post", "Send", "Exchange", "CompareExchange", "Increment",
            "Decrement", "Add", "Register", "Set", "Reset", "Wait", "WaitAll", "WaitAny"
        ];

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
            {
                return;
            }

            ISymbol? targetSymbol = null;

            // Handle different types of invocation expressions
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                // Regular member access: obj.Method() or field.Method()
                targetSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;

                // If the direct symbol resolution didn't work, try getting the method symbol from the invocation
                if (targetSymbol == null)
                {
                    targetSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
                }

                // Special handling for field access patterns like _field.Method()
                if (targetSymbol == null && memberAccess.Expression != null)
                {
                    // Get the type of the expression being accessed (e.g., the type of _field)
                    var expressionTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
                    if (expressionTypeInfo.Type != null)
                    {
                        // Look for the method in the type's members
                        var methodName = memberAccess.Name.Identifier.ValueText;
                        var potentialMethods = expressionTypeInfo.Type.GetMembers(methodName).OfType<IMethodSymbol>();
                        targetSymbol = potentialMethods.FirstOrDefault();
                    }
                }
            }
            else
            {
                // Direct invocation or other patterns (including conditional access)
                targetSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
            }

            // Check if the resolved symbol is a threading API
            if (targetSymbol != null && IsThreadingApiCall(targetSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ThreadingApisRule,
                    invocation.GetLocation(),
                    "Threading API usage detected.");

                context.ReportDiagnostic(diagnostic);
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
            {
                return;
            }

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
            {
                return;
            }

            // Get type information for the object being created
            ITypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;
            if (typeSymbol == null)
            {
                return;
            }

            // Only report object creation for specific high-risk threading types
            // Most threading violations will be caught via method invocations instead
            if (IsHighRiskThreadingObjectCreation(typeSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ThreadingApisRule,
                    objectCreation.GetLocation(),
                    "Threading API usage detected.");

                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determines if the object creation represents a high-risk threading pattern.
        /// </summary>
        /// <param name="typeSymbol">The type being created.</param>
        /// <returns>True if this is a high-risk threading object creation.</returns>
        private static bool IsHighRiskThreadingObjectCreation(ITypeSymbol typeSymbol)
        {
            string typeName = typeSymbol.Name;
            string? namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();

            // Only flag object creation for types that are inherently problematic just by existing
            // Don't flag Thread creation since the problem is usually starting it, not creating it
            if (namespaceName == "System.Threading")
            {
                // These are problematic just by being created in orchestrator context
                return typeName == "Timer" || typeName == "CancellationTokenSource";
            }

            return false;
        }

        /// <summary>
        /// Determines if a symbol represents a threading API call.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns>True if the symbol represents a threading API call.</returns>
        private static bool IsThreadingApiCall(ISymbol symbol)
        {
            // Only check method symbols, not properties or fields
            if (symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            if (methodSymbol.ContainingType == null)
            {
                return false;
            }

            INamedTypeSymbol containingType = methodSymbol.ContainingType;
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // Enhanced semantic analysis for specific threading types
            return CheckSpecificThreadingApis(symbol, containingType, typeName, namespaceName);
        }

        /// <summary>
        /// Enhanced semantic analysis for specific threading API patterns.
        /// </summary>
        private static bool CheckSpecificThreadingApis(ISymbol symbol, INamedTypeSymbol containingType, string typeName, string? namespaceName)
        {
            // System.Threading namespace types
            if (namespaceName == "System.Threading")
            {
                // ThreadPool methods - only specific problematic methods
                if (typeName == "ThreadPool" && symbol.Name == "QueueUserWorkItem")
                {
                    return true;
                }

                // Thread methods - only problematic instance methods, not Sleep which can be used safely
                if (typeName == "Thread" && symbol.Name == "Start")
                {
                    return true;
                }

                // AutoResetEvent methods
                if (typeName == "AutoResetEvent" && (symbol.Name == "WaitOne" || symbol.Name == "Set" || symbol.Name == "Reset"))
                {
                    return true;
                }

                // ManualResetEvent methods
                if (typeName == "ManualResetEvent" && (symbol.Name == "WaitOne" || symbol.Name == "Set" || symbol.Name == "Reset"))
                {
                    return true;
                }

                // WaitHandle methods (base class for AutoResetEvent, ManualResetEvent, Mutex, etc.)
                if (typeName == "WaitHandle" && (symbol.Name == "WaitOne" || symbol.Name == "WaitAll" || symbol.Name == "WaitAny"))
                {
                    return true;
                }

                // Monitor methods
                if (typeName == "Monitor" && (symbol.Name == "Enter" || symbol.Name == "Exit" || symbol.Name == "Wait" || symbol.Name == "Pulse"))
                {
                    return true;
                }

                // Mutex methods
                if (typeName == "Mutex" && (symbol.Name == "WaitOne" || symbol.Name == "ReleaseMutex"))
                {
                    return true;
                }

                // SynchronizationContext methods
                if (typeName == "SynchronizationContext" && (symbol.Name == "Post" || symbol.Name == "Send"))
                {
                    return true;
                }

                // Interlocked methods
                if (typeName == "Interlocked" && (symbol.Name == "Exchange" || symbol.Name == "CompareExchange" || 
                    symbol.Name == "Increment" || symbol.Name == "Decrement" || symbol.Name == "Add"))
                {
                    return true;
                }

                // CancellationToken methods  
                if (typeName == "CancellationToken" && symbol.Name == "Register")
                {
                    return true;
                }

                // ReaderWriterLock methods
                if ((typeName == "ReaderWriterLock" || typeName == "ReaderWriterLockSlim") &&
                    (symbol.Name.Contains("Reader") || symbol.Name.Contains("Writer") || symbol.Name.Contains("Lock")))
                {
                    return true;
                }
            }

            // System.Threading.Tasks namespace types
            if (namespaceName == "System.Threading.Tasks")
            {
                // Parallel methods
                if (typeName == "Parallel" && (symbol.Name == "For" || symbol.Name == "ForEach" || symbol.Name == "Invoke"))
                {
                    return true;
                }

                // Task.Run and other problematic Task methods
                if (typeName == "Task" && symbol.Name == "Run")
                {
                    return true;
                }
            }

            // Fallback to original logic for any other cases
            if ((namespaceName == "System.Threading" || namespaceName == "System.Threading.Tasks") &&
                ProblematicThreadingTypes.Contains(typeName) &&
                ProblematicThreadingMethods.Contains(symbol.Name))
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
            string typeName = typeSymbol.Name;
            string? namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();

            return (namespaceName == "System.Threading" || namespaceName == "System.Threading.Tasks") &&
                   ProblematicThreadingTypes.Contains(typeName);
        }
    }
}
