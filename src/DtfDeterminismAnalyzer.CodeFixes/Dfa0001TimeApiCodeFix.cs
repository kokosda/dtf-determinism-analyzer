using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DtfDeterminismAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for DFA0001: Replaces non-deterministic time APIs with orchestration context alternatives.
    /// Provides automatic fixes for DateTime.Now, DateTime.UtcNow, DateTime.Today, and Stopwatch usage 
    /// in Durable Task Framework orchestrator functions.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Dfa0001TimeApiCodeFix)), Shared]
    public class Dfa0001TimeApiCodeFix : CodeFixProvider
    {
        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can handle.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create("DFA0001");

        /// <summary>
        /// Gets the fix-all provider that can fix all instances of this diagnostic at once.
        /// </summary>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        /// Registers code fixes for the specified diagnostic reports.
        /// </summary>
        /// <param name="context">Context that contains the diagnostics to fix and allows registration of code actions.</param>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            foreach (Diagnostic? diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
            {
                Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
                SyntaxNode? node = root.FindNode(diagnosticSpan);

                // Handle both InvocationExpressionSyntax (for method calls like Stopwatch.GetTimestamp())
                // and MemberAccessExpressionSyntax (for property access like DateTime.Now)
                MemberAccessExpressionSyntax? memberAccess = node switch
                {
                    MemberAccessExpressionSyntax m => m,
                    InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax m => m,
                    _ => null
                };

                if (memberAccess == null)
                {
                    continue;
                }

                // Determine the type of time API being used and create appropriate fix
                string memberName = memberAccess.Name.Identifier.ValueText;
                string? typeName = GetTypeNameFromMemberAccess(memberAccess);

                if (typeName == "DateTime" && (memberName == "Now" || memberName == "UtcNow" || memberName == "Today"))
                {
                    RegisterDateTimeCodeFix(context, diagnostic, root, memberAccess, memberName);
                }
                else if (typeName == "Stopwatch" && (memberName == "StartNew" || memberName == "Start" || memberName == "GetTimestamp"))
                {
                    // For method invocations, we need to replace the entire invocation
                    SyntaxNode nodeToReplace = node is InvocationExpressionSyntax ? node : memberAccess;
                    RegisterStopwatchCodeFix(context, diagnostic, root, memberAccess, nodeToReplace, memberName);
                }
            }
        }

        /// <summary>
        /// Registers a code fix for DateTime API usage.
        /// </summary>
        private static void RegisterDateTimeCodeFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode root,
            MemberAccessExpressionSyntax memberAccess, string memberName)
        {
            string title = memberName switch
            {
                "Now" => "Replace DateTime.Now with context.CurrentUtcDateTime",
                "UtcNow" => "Replace DateTime.UtcNow with context.CurrentUtcDateTime",
                "Today" => "Replace DateTime.Today with context.CurrentUtcDateTime.Date",
                _ => $"Replace DateTime.{memberName} with context.CurrentUtcDateTime"
            };

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => FixDateTimeUsage(context.Document, root, memberAccess, memberName, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Registers a code fix for Stopwatch API usage.
        /// </summary>
        private static void RegisterStopwatchCodeFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode root,
            MemberAccessExpressionSyntax memberAccess, SyntaxNode nodeToReplace, string memberName)
        {
            string title = memberName switch
            {
                "StartNew" => "Replace Stopwatch.StartNew() with durable timer pattern",
                "Start" => "Replace Stopwatch.Start() with durable timer pattern",
                "GetTimestamp" => "Replace Stopwatch.GetTimestamp() with context.CurrentUtcDateTime",
                _ => $"Replace Stopwatch.{memberName} with durable alternative"
            };

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => FixStopwatchUsage(context.Document, root, memberAccess, nodeToReplace, memberName, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Fixes DateTime API usage by replacing with context.CurrentUtcDateTime.
        /// </summary>
        private static async Task<Document> FixDateTimeUsage(Document document, SyntaxNode root,
            MemberAccessExpressionSyntax memberAccess, string memberName, CancellationToken cancellationToken)
        {
            string? orchestratorContextParameter = await FindOrchestratorContextParameter(document, memberAccess, cancellationToken);
            if (orchestratorContextParameter == null)
            {
                return document;
            }

            MemberAccessExpressionSyntax replacement = memberName switch
            {
                "Now" or "UtcNow" => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(orchestratorContextParameter),
                    SyntaxFactory.IdentifierName("CurrentUtcDateTime")),
                "Today" => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(orchestratorContextParameter),
                        SyntaxFactory.IdentifierName("CurrentUtcDateTime")),
                    SyntaxFactory.IdentifierName("Date")),
                _ => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(orchestratorContextParameter),
                    SyntaxFactory.IdentifierName("CurrentUtcDateTime"))
            };

            SyntaxNode newRoot = root.ReplaceNode(memberAccess, replacement);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Fixes Stopwatch API usage by replacing with durable timer alternatives.
        /// </summary>
        private static async Task<Document> FixStopwatchUsage(Document document, SyntaxNode root,
            MemberAccessExpressionSyntax memberAccess, SyntaxNode nodeToReplace, string memberName, CancellationToken cancellationToken)
        {
            string? orchestratorContextParameter = await FindOrchestratorContextParameter(document, memberAccess, cancellationToken);
            if (orchestratorContextParameter == null)
            {
                return document;
            }

            ExpressionSyntax replacement;

            if (memberName == "GetTimestamp")
            {
                // Replace Stopwatch.GetTimestamp() with context.CurrentUtcDateTime.Ticks
                replacement = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(orchestratorContextParameter),
                        SyntaxFactory.IdentifierName("CurrentUtcDateTime")),
                    SyntaxFactory.IdentifierName("Ticks"));
            }
            else
            {
                // For StartNew() and Start(), suggest using CurrentUtcDateTime as the starting point
                // The user will need to manually implement timing logic using durable timers
                replacement = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(orchestratorContextParameter),
                    SyntaxFactory.IdentifierName("CurrentUtcDateTime"));
            }

            SyntaxNode newRoot = root.ReplaceNode(nodeToReplace, replacement);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Finds the orchestrator context parameter in the containing method.
        /// </summary>
        private static async Task<string?> FindOrchestratorContextParameter(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return null;
            }

            // Find the containing method
            MethodDeclarationSyntax? method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
            {
                return null;
            }

            // Look for a parameter with orchestration context type
            foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type == null)
                {
                    continue;
                }

                TypeInfo typeInfo = semanticModel.GetTypeInfo(parameter.Type, cancellationToken);
                if (typeInfo.Type == null)
                {
                    continue;
                }

                string typeName = typeInfo.Type.ToDisplayString();
                if (IsOrchestratorContextType(typeName))
                {
                    return parameter.Identifier.ValueText;
                }
            }

            // If no explicit parameter found, try common names
            return "context";
        }

        /// <summary>
        /// Determines if a type name represents an orchestrator context type.
        /// </summary>
        private static bool IsOrchestratorContextType(string typeName)
        {
            return typeName.Contains("IDurableOrchestrationContext") ||
                   typeName.Contains("DurableOrchestrationContext") ||
                   typeName.Contains("OrchestrationContext");
        }

        /// <summary>
        /// Gets the type name from a member access expression.
        /// </summary>
        private static string? GetTypeNameFromMemberAccess(MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax nestedMember => GetTypeNameFromMemberAccess(nestedMember),
                _ => null
            };
        }
    }
}
