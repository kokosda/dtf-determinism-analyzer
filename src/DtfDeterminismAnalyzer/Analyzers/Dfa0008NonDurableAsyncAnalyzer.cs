using System;
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
    /// Analyzer that detects non-durable async operations in orchestrator functions.
    /// Identifies async operations that are not performed through Durable Functions APIs.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0008NonDurableAsyncAnalyzer : DiagnosticAnalyzer
    {
        // Deduplication tracking: prevents reporting the same non-durable async operation multiple times
        // Key: (OrchestratorMethod, OperationKey) - Operation key uses location for uniqueness
        private static readonly ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, string OperationKey), bool> _reportedOperations
            = new ConcurrentDictionary<(MethodDeclarationSyntax, string), bool>();

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
            {
                return;
            }

            // Get the containing orchestrator method for deduplication
            var orchestratorMethod = GetContainingOrchestratorMethod(awaitExpression);
            if (orchestratorMethod == null)
            {
                return;
            }

            // Get the awaited expression
            ExpressionSyntax awaitedExpression = awaitExpression.Expression;

            // Check if this is a non-durable async operation (inverse of IsDurableAsyncOperation)
            if (!IsDurableAsyncOperation(awaitedExpression, context.SemanticModel))
            {
                // Create unique operation key using location for deduplication
                string operationKey = CreateOperationKey(awaitedExpression, context.SemanticModel, awaitExpression.GetLocation());
                
                // Deduplication: only report each operation once per orchestrator method
                if (_reportedOperations.TryAdd((orchestratorMethod, operationKey), true))
                {
                    string operationName = GetAwaitedOperationName(awaitedExpression, context.SemanticModel);
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NonDurableAsyncRule,
                        awaitExpression.GetLocation(),
                        operationName));
                }
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
            {
                return;
            }

            // Get the containing orchestrator method for deduplication
            var orchestratorMethod = GetContainingOrchestratorMethod(invocation);
            if (orchestratorMethod == null)
            {
                return;
            }

            // Skip if this invocation is being awaited - the AnalyzeAwaitExpression will handle it
            if (invocation.Parent is AwaitExpressionSyntax)
            {
                return;
            }

            // Skip if this invocation will be used in Task.WhenAll/WhenAny - let the await analysis handle it
            if (IsUsedInTaskComposition(invocation))
            {
                return;
            }

            // Only analyze non-awaited async method calls (which are also problematic)
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            // Check if this is a non-awaited async method (fire-and-forget, which is bad)
            if (methodSymbol.ReturnType.Name.StartsWith("Task", StringComparison.Ordinal) && 
                IsNonDurableAsyncMethod(methodSymbol))
            {
                // Create unique operation key using location for deduplication
                string operationKey = CreateOperationKey(invocation, context.SemanticModel, invocation.GetLocation());
                
                // Deduplication: only report each operation once per orchestrator method
                if (_reportedOperations.TryAdd((orchestratorMethod, operationKey), true))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NonDurableAsyncRule,
                        invocation.GetLocation(),
                        $"Non-awaited async method '{GetMethodDisplayName(methodSymbol)}' detected"));
                }
            }
        }

        private static bool IsDurableAsyncOperation(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // Check if the expression is a method invocation
            if (expression is InvocationExpressionSyntax invocation)
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    // Special handling for Task.WhenAll and Task.WhenAny
                    if (methodSymbol.ContainingType?.Name == "Task" && 
                        methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks" &&
                        methodSymbol.Name is "WhenAll" or "WhenAny")
                    {
                        // Analyze the arguments to determine if they're all durable
                        return AreAllTasksInCompositionDurable(invocation, semanticModel);
                    }
                    
                    return IsDurableMethod(methodSymbol);
                }
            }

            // Check if the expression is a member access (property)
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    return IsDurableProperty(propertySymbol);
                }
            }

            // For other expressions (variables, etc.), we need to be more conservative
            // and assume they might not be durable
            return false;
        }

        private static bool AreAllTasksInCompositionDurable(InvocationExpressionSyntax whenAllInvocation, SemanticModel semanticModel)
        {
            // Get the containing method to analyze the full context
            var method = whenAllInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null) return false;

            string methodText = method.ToString();
            
            // Improved heuristic: check for patterns that indicate durable vs non-durable operations
            
            // If the method contains durable context calls, and WhenAll arguments are variables,
            // it's likely that those variables hold durable tasks
            bool hasDurableCalls = methodText.Contains("context.Call") || 
                                  methodText.Contains("context.Create") ||
                                  methodText.Contains("context.Wait");
                                  
            // If the method contains HTTP or other non-durable calls and WhenAll arguments are variables,
            // check which pattern dominates
            bool hasNonDurableCalls = methodText.Contains("HttpClient") || 
                                     methodText.Contains("GetAsync") ||
                                     methodText.Contains("PostAsync") ||
                                     methodText.Contains("File.Read") ||
                                     methodText.Contains("Task.Run");

            // Check the actual WhenAll arguments to see if they're variables or direct calls
            string whenAllArgs = whenAllInvocation.ArgumentList?.ToString() ?? "";
            
            // If arguments are simple identifiers (like task1, task2), rely on method context  
            if (!whenAllArgs.Contains('(') && hasDurableCalls && !hasNonDurableCalls)
            {
                return true; // Variables likely hold durable tasks
            }
            
            // If arguments contain direct method calls, analyze those
            if (whenAllArgs.Contains("context.Call"))
            {
                return true;
            }
            
            if (whenAllArgs.Contains("GetAsync") || whenAllArgs.Contains("HttpClient"))
            {
                return false;
            }
            
            // Default: if we see durable patterns in the method and no obvious non-durable patterns in args, assume durable
            return hasDurableCalls && !hasNonDurableCalls;
        }

        private static bool IsDurableMethod(IMethodSymbol methodSymbol)
        {
            INamedTypeSymbol? containingType = methodSymbol.ContainingType;
            string? typeName = containingType?.Name;
            string? namespaceName = containingType?.ContainingNamespace?.ToDisplayString();

            // Durable Functions context methods are considered durable
            if (typeName is "IDurableOrchestrationContext" or "DurableOrchestrationContext" or "IDurableActivityContext" or "DurableActivityContext")
            {
                return methodSymbol.Name is
                    "CallActivityAsync" or "CallActivityWithRetryAsync" or
                    "CallSubOrchestratorAsync" or "CallSubOrchestratorWithRetryAsync" or
                    "CreateTimer" or "WaitForExternalEvent" or
                    "CallEntityAsync" or "CallHttpAsync";
            }

            // Task.WhenAll and Task.WhenAny - their durability depends on their arguments
            if (typeName == "Task" && namespaceName == "System.Threading.Tasks")
            {
                if (methodSymbol.Name is "WhenAll" or "WhenAny")
                {
                    // For WhenAll/WhenAny, we cannot easily analyze the arguments here
                    // We'll handle this at the expression level instead
                    return false; // Let the expression analysis determine if it's problematic
                }
                
                // Task.FromResult is safe - it returns a completed task synchronously
                if (methodSymbol.Name == "FromResult")
                {
                    return true;
                }
            }

            // Task.Delay is allowed if it uses the orchestrator context's cancellation token
            if (typeName == "Task" && namespaceName == "System.Threading.Tasks" && methodSymbol.Name == "Delay")
            {
                // This is a simplification - in a more sophisticated analyzer, we would check
                // if the cancellation token parameter comes from the orchestrator context
                return false; // For now, assume Task.Delay is not durable unless proven otherwise
            }

            // HttpContent methods should be considered neutral (neither durable nor explicitly non-durable)
            // to avoid double-counting with the main HTTP call
            if (typeName == "HttpContent" && namespaceName == "System.Net.Http")
            {
                // These are typically used after an HTTP call and might be considered continuations
                // We'll treat them as neither durable nor non-durable to avoid double reporting
                return true; // Mark as "durable" to avoid reporting them as violations
            }

            return false;
        }

        private static bool IsDurableProperty(IPropertySymbol propertySymbol)
        {
            INamedTypeSymbol containingType = propertySymbol.ContainingType;
            string? typeName = containingType?.Name;

            // Durable Functions context properties are considered safe
            return typeName is "IDurableOrchestrationContext" or "DurableOrchestrationContext"
                ? propertySymbol.Name is
                    "CurrentUtcDateTime" or "NewGuid" or "IsReplaying" or
                    "InstanceId" or "ParentInstanceId"
                : false;
        }

        private static bool IsNonDurableAsyncMethod(IMethodSymbol methodSymbol)
        {
            INamedTypeSymbol? containingType = methodSymbol.ContainingType;
            string? typeName = containingType?.Name;
            string? namespaceName = containingType?.ContainingNamespace?.ToDisplayString();

            // HTTP client async methods
            if (typeName == "HttpClient" && namespaceName == "System.Net.Http")
            {
                return methodSymbol.Name is "GetAsync" or "PostAsync" or "PutAsync" or "DeleteAsync" or
                       "GetStringAsync" or "GetByteArrayAsync" or "GetStreamAsync" or "SendAsync";
            }

            // HttpContent methods are often continuations of HTTP operations
            // We'll be more conservative here to avoid double-counting with the main HTTP call
            if (typeName == "HttpContent" && namespaceName == "System.Net.Http")
            {
                // These are typically used after an HTTP call and might be considered continuations
                // Rather than separate violations. For now, we'll not flag them independently.
                return false;
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
                // Task.FromResult is safe - it returns a completed task synchronously
                // Task.Run is problematic as it uses the thread pool
                return methodSymbol.Name is "Run" or "Factory";
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

        private static string GetAwaitedOperationName(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    return $"Non-durable async method '{GetMethodDisplayName(methodSymbol)}' detected";
                }
            }
            
            return $"Non-durable async operation '{expression}' detected";
        }

        private static bool IsUsedInTaskComposition(InvocationExpressionSyntax invocation)
        {
            // Look for patterns where this invocation is stored in a variable that's later used in Task.WhenAll/WhenAny
            // For now, implement a simple heuristic: check if this invocation is in a variable assignment
            // and that variable might be used in a Task.WhenAll call
            
            // Check if this invocation is being assigned to a variable
            if (invocation.Parent is not EqualsValueClauseSyntax equalsValue ||
                equalsValue.Parent is not VariableDeclaratorSyntax)
            {
                return false;
            }

            // Look for Task.WhenAll or Task.WhenAny usage in the same method
            var method = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null) return false;

            // Simple heuristic: if the method contains Task.WhenAll or Task.WhenAny, assume this might be related
            var methodText = method.ToString();
            return methodText.Contains("Task.WhenAll") || methodText.Contains("Task.WhenAny");
        }

        /// <summary>
        /// Gets the containing orchestrator method for the given syntax node.
        /// Used for deduplication to ensure we only report each operation once per orchestrator method.
        /// </summary>
        private static MethodDeclarationSyntax? GetContainingOrchestratorMethod(SyntaxNode node)
        {
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            
            // Check if this method or any ancestor method has an OrchestrationTrigger parameter
            while (method != null)
            {
                if (method.ParameterList?.Parameters.Any(p => 
                    p.AttributeLists.SelectMany(al => al.Attributes)
                        .Any(attr => attr.Name.ToString().Contains("OrchestrationTrigger"))) == true)
                {
                    return method;
                }
                
                // Look for parent method in nested scenarios
                method = method.Parent?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            }
            
            return null;
        }

        /// <summary>
        /// Creates a unique operation key for deduplication.
        /// Uses method signature and location to distinguish between multiple calls to the same method.
        /// </summary>
        private static string CreateOperationKey(SyntaxNode node, SemanticModel semanticModel, Location location)
        {
            string baseKey = "unknown_operation";
            
            if (node is InvocationExpressionSyntax invocation)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    baseKey = $"{methodSymbol.ContainingType?.Name}.{methodSymbol.Name}";
                }
                else
                {
                    baseKey = invocation.Expression.ToString();
                }
            }
            else if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    baseKey = $"{methodSymbol.ContainingType?.Name}.{methodSymbol.Name}";
                }
                else
                {
                    baseKey = memberAccess.ToString();
                }
            }
            else
            {
                baseKey = node.ToString();
            }

            // Include location to distinguish multiple calls to the same method
            var linePosition = location.GetLineSpan().StartLinePosition;
            return $"{baseKey}@{linePosition.Line}:{linePosition.Character}";
        }
    }
}
