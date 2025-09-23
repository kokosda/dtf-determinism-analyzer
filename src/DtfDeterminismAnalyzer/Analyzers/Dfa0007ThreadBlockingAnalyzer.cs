using System;
using System.Collections.Immutable;
using DtfDeterminismAnalyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects blocking operations in orchestrator functions.
    /// Identifies Thread.Sleep, blocking I/O, and other operations that block threads.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0007ThreadBlockingAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.ThreadBlockingRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
            {
                return;
            }

            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            INamedTypeSymbol containingType = methodSymbol.ContainingType;
            if (containingType == null)
            {
                return;
            }

            // Check for various blocking operations
            if (IsThreadBlockingOperation(containingType, methodSymbol.Name) ||
                IsBlockingTaskOperation(containingType, methodSymbol.Name, methodSymbol) ||
                IsBlockingSyncOperation(containingType, methodSymbol.Name) ||
                IsBlockingIoOperation(containingType, methodSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ThreadBlockingRule,
                    invocation.GetLocation(),
                    $"Blocking operation '{GetInvocationDisplayName(invocation)}' detected"));
            }
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

            INamedTypeSymbol containingType = memberSymbol.ContainingType;
            if (containingType == null)
            {
                return;
            }

            // Check for blocking property access (like Task.Result)
            if (IsBlockingPropertyAccess(containingType, memberSymbol.Name, memberSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ThreadBlockingRule,
                    memberAccess.GetLocation(),
                    $"Blocking property access '{memberAccess}' detected"));
            }
        }

        private static bool IsThreadBlockingOperation(INamedTypeSymbol containingType, string methodName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Threading.Thread blocking operations
            if (typeName == "Thread" && namespaceName == "System.Threading")
            {
                return methodName is "Sleep" or "Join";
            }

            // System.Threading.Monitor blocking operations
            if (typeName == "Monitor" && namespaceName == "System.Threading")
            {
                return methodName is "Enter" or "Wait" or "Pulse" or "PulseAll";
            }

            // System.Threading.Mutex blocking operations
            if (typeName == "Mutex" && namespaceName == "System.Threading")
            {
                return methodName is "WaitOne";
            }

            // System.Threading.Semaphore blocking operations
            if (typeName == "Semaphore" && namespaceName == "System.Threading")
            {
                return methodName is "WaitOne";
            }

            // System.Threading.AutoResetEvent/ManualResetEvent blocking operations
            return (typeName is "AutoResetEvent" or "ManualResetEvent") && namespaceName == "System.Threading" ? methodName is "WaitOne" : false;
        }

        private static bool IsBlockingTaskOperation(INamedTypeSymbol containingType, string methodName, IMethodSymbol methodSymbol)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Threading.Tasks.Task blocking operations
            if ((typeName is "Task" or "Task`1") && namespaceName == "System.Threading.Tasks")
            {
                return methodName is "Wait" or "RunSynchronously";
            }

            // Task.WaitAll and Task.WaitAny
            return typeName == "Task" && namespaceName == "System.Threading.Tasks" ? methodName is "WaitAll" or "WaitAny" : false;
        }

        private static bool IsBlockingPropertyAccess(INamedTypeSymbol containingType, string propertyName, ISymbol symbol)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // Task.Result property access (blocking)
            return (typeName is "Task`1") && namespaceName == "System.Threading.Tasks" && symbol is IPropertySymbol
                ? propertyName is "Result"
                : false;
        }

        private static bool IsBlockingSyncOperation(INamedTypeSymbol containingType, string methodName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Threading.Tasks.Parallel blocking operations
            if (typeName == "Parallel" && namespaceName == "System.Threading.Tasks")
            {
                return methodName is "For" or "ForEach" or "Invoke";
            }

            // System.Threading.WaitHandle blocking operations
            if (typeName == "WaitHandle" && namespaceName == "System.Threading")
            {
                return methodName is "WaitOne" or "WaitAll" or "WaitAny";
            }

            // System.Threading.CountdownEvent blocking operations
            if (typeName == "CountdownEvent" && namespaceName == "System.Threading")
            {
                return methodName is "Wait";
            }

            // System.Threading.Barrier blocking operations
            return typeName == "Barrier" && namespaceName == "System.Threading" ? methodName is "SignalAndWait" : false;
        }

        private static bool IsBlockingIoOperation(INamedTypeSymbol containingType, string methodName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // Console blocking operations
            if (typeName == "Console" && namespaceName == "System")
            {
                return methodName is "ReadLine" or "Read" or "ReadKey";
            }

            // Stream synchronous operations that may block
            if (namespaceName == "System.IO")
            {
                if (typeName is "Stream" or "FileStream" or "NetworkStream" or "MemoryStream")
                {
                    return methodName is "Read" or "Write" or "ReadByte" or "WriteByte" or
                           "CopyTo" or "Flush";
                }

                if (typeName is "StreamReader" or "StreamWriter" or "TextReader" or "TextWriter" or
                    "BinaryReader" or "BinaryWriter")
                {
                    return methodName is "Read" or "ReadLine" or "ReadToEnd" or "ReadBlock" or
                           "Write" or "WriteLine" or "Flush";
                }
            }

            // Network synchronous operations that may block
            if (namespaceName?.StartsWith("System.Net", StringComparison.Ordinal) == true)
            {
                // HttpClient synchronous methods (if any exist)
                if (typeName == "WebClient")
                {
                    return methodName is "DownloadData" or "DownloadString" or "UploadData" or "UploadString";
                }

                // Socket blocking operations
                if (typeName == "Socket")
                {
                    return methodName is "Accept" or "Connect" or "Receive" or "Send" or
                           "ReceiveFrom" or "SendTo";
                }

                // TcpClient/UdpClient blocking operations
                if (typeName is "TcpClient" or "UdpClient")
                {
                    return methodName is "Connect" or "Receive" or "Send";
                }
            }

            return false;
        }

        private static string GetInvocationDisplayName(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.ToString(),
                IdentifierNameSyntax identifier => identifier.ToString(),
                _ => invocation.ToString()
            };
        }
    }
}
