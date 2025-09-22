using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using DtfDeterminismAnalyzer.Utils;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects I/O operations in orchestrator functions.
    /// Identifies File I/O, HTTP requests, and other I/O operations that are non-deterministic.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0004IoAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(DiagnosticDescriptors.IoRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            
            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(memberAccess, context.SemanticModel))
                return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol is not IMethodSymbol and not IPropertySymbol)
                return;

            var containingType = memberSymbol.ContainingType;
            if (containingType == null)
                return;

            // Check for File I/O operations
            if (IsFileIoOperation(containingType, memberSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IoRule,
                    memberAccess.GetLocation(),
                    $"File I/O operation '{memberAccess}' detected"));
                return;
            }

            // Check for HTTP client operations
            if (IsHttpClientOperation(containingType, memberSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IoRule,
                    memberAccess.GetLocation(),
                    $"HTTP operation '{memberAccess}' detected"));
                return;
            }

            // Check for Directory operations
            if (IsDirectoryOperation(containingType, memberSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IoRule,
                    memberAccess.GetLocation(),
                    $"Directory operation '{memberAccess}' detected"));
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            
            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(invocation, context.SemanticModel))
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return;

            var containingType = methodSymbol.ContainingType;
            if (containingType == null)
                return;

            // Check for various I/O operations
            if (IsFileIoOperation(containingType, methodSymbol.Name) ||
                IsHttpClientOperation(containingType, methodSymbol.Name) ||
                IsDirectoryOperation(containingType, methodSymbol.Name) ||
                IsNetworkOperation(containingType, methodSymbol.Name) ||
                IsConsoleOperation(containingType, methodSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IoRule,
                    invocation.GetLocation(),
                    $"I/O operation '{GetInvocationDisplayName(invocation)}' detected"));
            }
        }

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            
            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(objectCreation, context.SemanticModel))
                return;

            var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);
            if (typeInfo.Type == null)
                return;

            var typeName = typeInfo.Type.Name;
            var containingNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();

            // Check for I/O related object creations
            if (containingNamespace != null && IsIoRelatedType(typeName, containingNamespace))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IoRule,
                    objectCreation.GetLocation(),
                    $"I/O object creation '{typeName}' detected"));
            }
        }

        private static bool IsFileIoOperation(INamedTypeSymbol containingType, string memberName)
        {
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.IO.File operations
            if (typeName == "File" && namespaceName == "System.IO")
            {
                return memberName is "ReadAllText" or "ReadAllLines" or "ReadAllBytes" or 
                       "WriteAllText" or "WriteAllLines" or "WriteAllBytes" or
                       "OpenRead" or "OpenWrite" or "Create" or "Open" or
                       "Copy" or "Move" or "Delete" or "Exists" or
                       "ReadLines" or "AppendAllText" or "AppendAllLines";
            }

            // System.IO.Path operations that might involve file system access
            if (typeName == "Path" && namespaceName == "System.IO")
            {
                return memberName is "GetTempFileName" or "GetTempPath";
            }

            return false;
        }

        private static bool IsHttpClientOperation(INamedTypeSymbol containingType, string memberName)
        {
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Net.Http.HttpClient operations
            if (typeName == "HttpClient" && namespaceName == "System.Net.Http")
            {
                return memberName is "GetAsync" or "PostAsync" or "PutAsync" or "DeleteAsync" or
                       "GetStringAsync" or "GetByteArrayAsync" or "GetStreamAsync" or
                       "SendAsync";
            }

            return false;
        }

        private static bool IsDirectoryOperation(INamedTypeSymbol containingType, string memberName)
        {
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.IO.Directory operations
            if (typeName == "Directory" && namespaceName == "System.IO")
            {
                return memberName is "Exists" or "CreateDirectory" or "Delete" or
                       "GetFiles" or "GetDirectories" or "GetFileSystemEntries" or
                       "EnumerateFiles" or "EnumerateDirectories" or
                       "GetCurrentDirectory" or "SetCurrentDirectory";
            }

            return false;
        }

        private static bool IsNetworkOperation(INamedTypeSymbol containingType, string memberName)
        {
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Net related operations
            if (namespaceName?.StartsWith("System.Net", StringComparison.Ordinal) == true)
            {
                return typeName is "WebClient" or "HttpWebRequest" or "FtpWebRequest" or
                       "TcpClient" or "UdpClient" or "Socket" or "NetworkStream";
            }

            return false;
        }

        private static bool IsConsoleOperation(INamedTypeSymbol containingType, string memberName)
        {
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Console operations
            if (typeName == "Console" && namespaceName == "System")
            {
                return memberName is "ReadLine" or "Read" or "ReadKey" or
                       "WriteLine" or "Write";
            }

            return false;
        }

        private static bool IsIoRelatedType(string typeName, string containingNamespace)
        {
            return typeName switch
            {
                "FileStream" or "StreamReader" or "StreamWriter" or "BinaryReader" or "BinaryWriter" or
                "TextReader" or "TextWriter" or "StringReader" or "StringWriter" or
                "MemoryStream" when containingNamespace == "System.IO" => true,
                
                "HttpClient" or "HttpRequestMessage" or "HttpResponseMessage" or
                "WebClient" when containingNamespace?.StartsWith("System.Net", StringComparison.Ordinal) == true => true,
                
                "TcpClient" or "UdpClient" or "Socket" or "NetworkStream" 
                when containingNamespace?.StartsWith("System.Net", StringComparison.Ordinal) == true => true,
                
                _ => false
            };
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