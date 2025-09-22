using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using DtfDeterminismAnalyzer.Utils;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects non-durable async operations in orchestrator functions.
    /// Identifies async operations that are not performed through Durable Functions APIs.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0008NonDurableAsyncAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(DiagnosticDescriptors.NonDurableAsyncRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
        {
            var awaitExpression = (AwaitExpressionSyntax)context.Node;
            
            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(awaitExpression, context.SemanticModel))
                return;

            // Get the awaited expression
            var awaitedExpression = awaitExpression.Expression;
            
            // Check if this is a durable operation
            if (!IsDurableAsyncOperation(awaitedExpression, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonDurableAsyncRule,
                    awaitExpression.GetLocation(),
                    $"Non-durable async operation '{awaitedExpression}' detected"));
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            
            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
                return;

            // Only analyze if this invocation is being awaited
            if (invocation.Parent is not AwaitExpressionSyntax)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return;

            // Check if this is a non-durable async operation
            if (IsNonDurableAsyncMethod(methodSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonDurableAsyncRule,
                    invocation.GetLocation(),
                    $"Non-durable async method '{GetMethodDisplayName(methodSymbol)}' detected"));
            }
        }

        private static bool IsDurableAsyncOperation(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // Check if the expression is a method invocation
            if (expression is InvocationExpressionSyntax invocation)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    return IsDurableMethod(methodSymbol);
                }
            }

            // Check if the expression is a member access (property)
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    return IsDurableProperty(propertySymbol);
                }
            }

            // For other expressions (variables, etc.), we need to be more conservative
            // and assume they might not be durable
            return false;
        }

        private static bool IsDurableMethod(IMethodSymbol methodSymbol)
        {
            var containingType = methodSymbol.ContainingType;
            var typeName = containingType?.Name;
            var namespaceName = containingType?.ContainingNamespace?.ToDisplayString();

            // Durable Functions context methods are considered durable
            if (typeName is "IDurableOrchestrationContext" or "DurableOrchestrationContext" or "IDurableActivityContext" or "DurableActivityContext")
            {
                return methodSymbol.Name is 
                    "CallActivityAsync" or "CallActivityWithRetryAsync" or 
                    "CallSubOrchestratorAsync" or "CallSubOrchestratorWithRetryAsync" or
                    "CreateTimer" or "WaitForExternalEvent" or
                    "CallEntityAsync" or "CallHttpAsync";
            }

            // Task.Delay is allowed if it uses the orchestrator context's cancellation token
            if (typeName == "Task" && namespaceName == "System.Threading.Tasks" && methodSymbol.Name == "Delay")
            {
                // This is a simplification - in a more sophisticated analyzer, we would check
                // if the cancellation token parameter comes from the orchestrator context
                return false; // For now, assume Task.Delay is not durable unless proven otherwise
            }

            return false;
        }

        private static bool IsDurableProperty(IPropertySymbol propertySymbol)
        {
            var containingType = propertySymbol.ContainingType;
            var typeName = containingType?.Name;

            // Durable Functions context properties are considered safe
            if (typeName is "IDurableOrchestrationContext" or "DurableOrchestrationContext")
            {
                return propertySymbol.Name is 
                    "CurrentUtcDateTime" or "NewGuid" or "IsReplaying" or
                    "InstanceId" or "ParentInstanceId";
            }

            return false;
        }

        private static bool IsNonDurableAsyncMethod(IMethodSymbol methodSymbol)
        {
            var containingType = methodSymbol.ContainingType;
            var typeName = containingType?.Name;
            var namespaceName = containingType?.ContainingNamespace?.ToDisplayString();

            // HTTP client async methods
            if (typeName == "HttpClient" && namespaceName == "System.Net.Http")
            {
                return methodSymbol.Name is "GetAsync" or "PostAsync" or "PutAsync" or "DeleteAsync" or
                       "GetStringAsync" or "GetByteArrayAsync" or "GetStreamAsync" or "SendAsync";
            }

            // File I/O async methods
            if (typeName == "File" && namespaceName == "System.IO")
            {
                return methodSymbol.Name is "ReadAllTextAsync" or "ReadAllLinesAsync" or "ReadAllBytesAsync" or
                       "WriteAllTextAsync" or "WriteAllLinesAsync" or "WriteAllBytesAsync" or
                       "AppendAllTextAsync" or "AppendAllLinesAsync";
            }

            // Stream async methods
            if (namespaceName == "System.IO")
            {
                if (typeName is "Stream" or "FileStream" or "NetworkStream" or "MemoryStream")
                {
                    return methodSymbol.Name is "ReadAsync" or "WriteAsync" or "CopyToAsync" or "FlushAsync";
                }

                if (typeName is "StreamReader" or "StreamWriter" or "TextReader" or "TextWriter")
                {
                    return methodSymbol.Name is "ReadAsync" or "ReadLineAsync" or "ReadToEndAsync" or
                           "WriteAsync" or "WriteLineAsync" or "FlushAsync";
                }
            }

            // Database async methods (common patterns)
            if (namespaceName?.StartsWith("System.Data", StringComparison.Ordinal) == true)
            {
                return methodSymbol.Name.EndsWith("Async", StringComparison.Ordinal);
            }

            // Entity Framework async methods
            if (namespaceName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
            {
                return methodSymbol.Name is "SaveChangesAsync" or "ToListAsync" or "FirstOrDefaultAsync" or
                       "SingleOrDefaultAsync" or "AnyAsync" or "CountAsync";
            }

            // General Task.Run or similar task creation methods
            if (typeName == "Task" && namespaceName == "System.Threading.Tasks")
            {
                return methodSymbol.Name is "Run" or "Factory" or "FromResult";
            }

            // Thread pool operations
            if (typeName == "ThreadPool" && namespaceName == "System.Threading")
            {
                return methodSymbol.Name is "QueueUserWorkItem";
            }

            // Timer operations
            if (typeName == "Timer" && namespaceName == "System.Threading")
            {
                return true; // Timer constructors and methods
            }

            // Raw Task.Delay without proper cancellation token
            if (typeName == "Task" && namespaceName == "System.Threading.Tasks" && methodSymbol.Name == "Delay")
            {
                // Check if it has the proper parameters for durable usage
                // This is simplified - a more sophisticated check would verify the cancellation token source
                return true; // For now, assume all Task.Delay calls are problematic
            }

            return false;
        }

        private static string GetMethodDisplayName(IMethodSymbol methodSymbol)
        {
            return $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}";
        }
    }
}