using System.Collections.Immutable;
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
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
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

            // Check for Environment class operations
            if (IsEnvironmentOperation(containingType, memberSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EnvironmentRule,
                    memberAccess.GetLocation(),
                    $"Environment operation '{memberAccess}' detected"));
                return;
            }

            // Check for system properties
            if (IsSystemPropertyAccess(containingType, memberSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EnvironmentRule,
                    memberAccess.GetLocation(),
                    $"System property access '{memberAccess}' detected"));
                return;
            }

            // Check for operating system specific operations
            if (IsOperatingSystemOperation(containingType, memberSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EnvironmentRule,
                    memberAccess.GetLocation(),
                    $"Operating system operation '{memberAccess}' detected"));
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

            // Check for various environment-dependent operations
            if (IsEnvironmentOperation(containingType, methodSymbol.Name) ||
                IsSystemPropertyAccess(containingType, methodSymbol.Name) ||
                IsOperatingSystemOperation(containingType, methodSymbol.Name) ||
                IsProcessOperation(containingType, methodSymbol.Name) ||
                IsRegistryOperation(containingType, methodSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EnvironmentRule,
                    invocation.GetLocation(),
                    $"Environment-dependent operation '{GetInvocationDisplayName(invocation)}' detected"));
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
    }
}
