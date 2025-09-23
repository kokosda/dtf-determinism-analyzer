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
            {
                return;
            }

            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol is not IMethodSymbol and not IPropertySymbol)
            {
                return;
            }

            INamedTypeSymbol containingType = memberSymbol.ContainingType;
            if (containingType == null)
            {
                return;
            }

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
            {
                return;
            }

            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);
            if (typeInfo.Type == null)
            {
                return;
            }

            string typeName = typeInfo.Type.Name;
            string? containingNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();

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
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

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
            return typeName == "Path" && namespaceName == "System.IO" ? memberName is "GetTempFileName" or "GetTempPath" : false;
        }

        private static bool IsHttpClientOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Net.Http.HttpClient operations
            return typeName == "HttpClient" && namespaceName == "System.Net.Http"
                ? memberName is "GetAsync" or "PostAsync" or "PutAsync" or "DeleteAsync" or
                       "GetStringAsync" or "GetByteArrayAsync" or "GetStreamAsync" or
                       "SendAsync"
                : false;
        }

        private static bool IsDirectoryOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.IO.Directory operations
            return typeName == "Directory" && namespaceName == "System.IO"
                ? memberName is "Exists" or "CreateDirectory" or "Delete" or
                       "GetFiles" or "GetDirectories" or "GetFileSystemEntries" or
                       "EnumerateFiles" or "EnumerateDirectories" or
                       "GetCurrentDirectory" or "SetCurrentDirectory"
                : false;
        }

        private static bool IsNetworkOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Net related operations
            return namespaceName?.StartsWith("System.Net", StringComparison.Ordinal) == true
                ? typeName is "WebClient" or "HttpWebRequest" or "FtpWebRequest" or
                       "TcpClient" or "UdpClient" or "Socket" or "NetworkStream"
                : false;
        }

        private static bool IsConsoleOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Console operations
            return typeName == "Console" && namespaceName == "System"
                ? memberName is "ReadLine" or "Read" or "ReadKey" or
                       "WriteLine" or "Write"
                : false;
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
