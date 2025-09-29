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
    /// Analyzer that detects environment-dependent operations in orchestrator functions.
    /// Identifies access to environment variables, system properties, and other environment-specific data.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0005EnvironmentAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.EnvironmentRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Dictionary to track reported Environment operations per orchestrator method to prevent duplicates
                // Use string key with location info to distinguish between different calls to the same method
                var reportedEnvironmentOperations = new ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, string OperationKey), bool>();
                
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeMemberAccess(ctx, reportedEnvironmentOperations), SyntaxKind.SimpleMemberAccessExpression);
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeInvocationExpression(ctx, reportedEnvironmentOperations), SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, string OperationKey), bool> reportedEnvironmentOperations)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(memberAccess, context.SemanticModel))
            {
                return;
            }

            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol is not IPropertySymbol)
            {
                return; // Skip method symbols - they will be handled by AnalyzeInvocationExpression
            }

            INamedTypeSymbol containingType = memberSymbol.ContainingType;
            if (containingType == null)
            {
                return;
            }

            // Check for Environment class operations
            if (IsEnvironmentOperation(containingType, memberSymbol.Name))
            {
                var orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    var operationKey = $"{memberSymbol.ToDisplayString()}@{memberAccess.GetLocation().GetLineSpan().StartLinePosition}";
                    var key = (orchestratorMethod, operationKey);
                    if (reportedEnvironmentOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.EnvironmentRule,
                            memberAccess.GetLocation(),
                            $"Environment operation '{memberAccess}' detected"));
                    }
                }
                return;
            }

            // Check for system properties
            if (IsSystemPropertyAccess(containingType, memberSymbol.Name))
            {
                var orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    var operationKey = $"{memberSymbol.ToDisplayString()}@{memberAccess.GetLocation().GetLineSpan().StartLinePosition}";
                    var key = (orchestratorMethod, operationKey);
                    if (reportedEnvironmentOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.EnvironmentRule,
                            memberAccess.GetLocation(),
                            $"System property access '{memberAccess}' detected"));
                    }
                }
                return;
            }

            // Check for operating system specific operations
            if (IsOperatingSystemOperation(containingType, memberSymbol.Name))
            {
                var orchestratorMethod = GetContainingOrchestratorMethod(memberAccess);
                if (orchestratorMethod != null)
                {
                    var operationKey = $"{memberSymbol.ToDisplayString()}@{memberAccess.GetLocation().GetLineSpan().StartLinePosition}";
                    var key = (orchestratorMethod, operationKey);
                    if (reportedEnvironmentOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.EnvironmentRule,
                            memberAccess.GetLocation(),
                            $"Operating system operation '{memberAccess}' detected"));
                    }
                }
            }
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context, ConcurrentDictionary<(MethodDeclarationSyntax OrchestratorMethod, string OperationKey), bool> reportedEnvironmentOperations)
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

            // Check for various environment-dependent operations
            if (IsEnvironmentOperation(containingType, methodSymbol.Name) ||
                IsSystemPropertyAccess(containingType, methodSymbol.Name) ||
                IsOperatingSystemOperation(containingType, methodSymbol.Name) ||
                IsProcessOperation(containingType, methodSymbol.Name) ||
                IsRegistryOperation(containingType, methodSymbol.Name))
            {
                var orchestratorMethod = GetContainingOrchestratorMethod(invocation);
                if (orchestratorMethod != null)
                {
                    // Use a unique key based on the method symbol and invocation location to distinguish multiple calls
                    var operationKey = $"{methodSymbol.ToDisplayString()}@{invocation.GetLocation().GetLineSpan().StartLinePosition}";
                    var key = (orchestratorMethod, operationKey);
                    if (reportedEnvironmentOperations.TryAdd(key, true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.EnvironmentRule,
                            invocation.GetLocation(),
                            $"Environment-dependent operation '{GetInvocationDisplayName(invocation)}' detected"));
                    }
                }
            }
        }

        private static bool IsEnvironmentOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Environment operations
            return typeName == "Environment" && namespaceName == "System"
                ? memberName is "GetEnvironmentVariable" or "GetEnvironmentVariables" or
                       "SetEnvironmentVariable" or "ExpandEnvironmentVariables" or
                       "MachineName" or "UserName" or "UserDomainName" or
                       "OSVersion" or "Version" or "Is64BitOperatingSystem" or
                       "Is64BitProcess" or "ProcessorCount" or "SystemDirectory" or
                       "CurrentDirectory" or "GetFolderPath" or "GetLogicalDrives" or
                       "SystemPageSize" or "TickCount" or "TickCount64" or
                       "WorkingSet" or "HasShutdownStarted" or "ExitCode"
                : false;
        }

        private static bool IsSystemPropertyAccess(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.IO.Directory operations that access system properties
            if (typeName == "Directory" && namespaceName == "System.IO")
            {
                return memberName is "GetCurrentDirectory";
            }

            // System.IO.Path operations that might be environment dependent
            if (typeName == "Path" && namespaceName == "System.IO")
            {
                return memberName is "GetTempPath";
            }

            // System.Reflection.Assembly operations
            if (typeName == "Assembly" && namespaceName == "System.Reflection")
            {
                return memberName is "GetExecutingAssembly" or "GetCallingAssembly" or "GetEntryAssembly" or
                       "Location" or "CodeBase";
            }

            // System.AppDomain operations (if still using .NET Framework)
            return typeName == "AppDomain" && namespaceName == "System"
                ? memberName is "CurrentDomain" or "BaseDirectory" or "RelativeSearchPath" or
                       "DynamicDirectory"
                : false;
        }

        private static bool IsOperatingSystemOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.OperatingSystem operations
            if (typeName == "OperatingSystem" && namespaceName == "System")
            {
                return memberName is "IsWindows" or "IsLinux" or "IsMacOS" or "IsFreeBSD" or
                       "IsAndroid" or "IsIOS" or "IsTvOS" or "IsWatchOS" or "IsBrowser" or
                       "IsWasi" or "Platform" or "Version";
            }

            // System.Runtime.InteropServices.RuntimeInformation operations
            return typeName == "RuntimeInformation" && namespaceName == "System.Runtime.InteropServices"
                ? memberName is "IsOSPlatform" or "OSDescription" or "OSArchitecture" or
                       "ProcessArchitecture" or "FrameworkDescription"
                : false;
        }

        private static bool IsProcessOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // System.Diagnostics.Process operations
            return typeName == "Process" && namespaceName == "System.Diagnostics"
                ? memberName is "GetCurrentProcess" or "GetProcesses" or "GetProcessById" or
                       "GetProcessesByName" or "Start"
                : false;
        }

        private static bool IsRegistryOperation(INamedTypeSymbol containingType, string memberName)
        {
            string typeName = containingType.Name;
            string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // Microsoft.Win32.Registry operations (Windows-specific)
            if (namespaceName == "Microsoft.Win32" &&
                (typeName == "Registry" || typeName == "RegistryKey"))
            {
                return true; // All registry operations are environment dependent
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
            // This handles cases where Environment operations are in helper methods within the orchestrator class
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
