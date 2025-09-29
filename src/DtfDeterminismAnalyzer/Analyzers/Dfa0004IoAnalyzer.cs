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
            
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Dictionary to track reported I/O operations per orchestrator method to prevent duplicates
                var reportedIoOperations = new ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol IoOperation), bool>();
                
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeMemberAccess(ctx, reportedIoOperations), SyntaxKind.SimpleMemberAccessExpression);
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeInvocationExpression(ctx, reportedIoOperations), SyntaxKind.InvocationExpression);
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeObjectCreationExpression(ctx, reportedIoOperations), SyntaxKind.ObjectCreationExpression);
            });
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol IoOperation), bool> reportedIoOperations)
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
                var orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    var key = (orchestratorMethod, memberSymbol);
                    if (reportedIoOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IoRule,
                            memberAccess.GetLocation(),
                            $"File I/O operation '{memberAccess}' detected"));
                    }
                }
                return;
            }

            // Check for HTTP client operations
            if (IsHttpClientOperation(containingType, memberSymbol.Name))
            {
                var orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    var key = (orchestratorMethod, memberSymbol);
                    if (reportedIoOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IoRule,
                            memberAccess.GetLocation(),
                            $"HTTP operation '{memberAccess}' detected"));
                    }
                }
                return;
            }

            // Check for Directory operations
            if (IsDirectoryOperation(containingType, memberSymbol.Name))
            {
                var orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    var key = (orchestratorMethod, memberSymbol);
                    if (reportedIoOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IoRule,
                            memberAccess.GetLocation(),
                            $"Directory operation '{memberAccess}' detected"));
                    }
                }
            }
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol IoOperation), bool> reportedIoOperations)
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
                var orchestratorMethod = GetContainingOrchestratorMethod(invocation);
                if (orchestratorMethod != null)
                {
                    var key = (orchestratorMethod, (ISymbol)methodSymbol);
                    if (reportedIoOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IoRule,
                            invocation.GetLocation(),
                            $"I/O operation '{GetInvocationDisplayName(invocation)}' detected"));
                    }
                }
            }
        }

        private static void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, ISymbol IoOperation), bool> reportedIoOperations)
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
                var orchestratorMethod = GetContainingOrchestratorMethod(objectCreation);
                if (orchestratorMethod != null)
                {
                    var key = (orchestratorMethod, (ISymbol)typeInfo.Type);
                    if (reportedIoOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IoRule,
                            objectCreation.GetLocation(),
                            $"I/O object creation '{typeName}' detected"));
                    }
                }
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

        /// <summary>
        /// Gets the containing orchestrator method for a given node, even if the node is in a helper method.
        /// This ensures we deduplicate diagnostics per orchestrator method, not per helper method.
        /// </summary>
        private static MethodDeclarationSyntax? GetContainingOrchestratorMethod(SyntaxNode node)
        {
            // First find any containing method
            var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
            {
                return null;
            }

            // Check if this method has the [OrchestrationTrigger] attribute
            if (HasOrchestrationTriggerAttribute(containingMethod))
            {
                return containingMethod;
            }

            // If not, look for other methods in the same class that have the [OrchestrationTrigger] attribute
            // This handles cases where I/O operations are in helper methods within the orchestrator class
            var containingClass = containingMethod.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass != null)
            {
                foreach (var method in containingClass.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (HasOrchestrationTriggerAttribute(method))
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a method has the [OrchestrationTrigger] attribute
        /// </summary>
        private static bool HasOrchestrationTriggerAttribute(MethodDeclarationSyntax method)
        {
            foreach (var parameterList in method.ParameterList.Parameters)
            {
                foreach (var attributeList in parameterList.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attributeName = attribute.Name.ToString();
                        if (attributeName.Contains("OrchestrationTrigger"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
