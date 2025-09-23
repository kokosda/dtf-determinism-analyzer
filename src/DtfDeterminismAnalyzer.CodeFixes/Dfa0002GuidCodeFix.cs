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
    /// Code fix provider for DFA0002: Replaces non-deterministic GUID generation with orchestration context alternatives.
    /// Provides automatic fixes for Guid.NewGuid() method calls in Durable Task Framework orchestrator functions.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Dfa0002GuidCodeFix)), Shared]
    public class Dfa0002GuidCodeFix : CodeFixProvider
    {
        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can handle.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create("DFA0002");

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
                if (root.FindNode(diagnosticSpan) is not InvocationExpressionSyntax invocation)
                {
                    continue;
                }

                // Check if this is a Guid.NewGuid() method call
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    string memberName = memberAccess.Name.Identifier.ValueText;
                    string? typeName = GetTypeNameFromMemberAccess(memberAccess);

                    if (typeName == "Guid" && memberName == "NewGuid")
                    {
                        RegisterGuidNewGuidCodeFix(context, diagnostic, root, invocation);
                    }
                }
            }
        }

        /// <summary>
        /// Registers a code fix for Guid.NewGuid() method calls.
        /// </summary>
        private static void RegisterGuidNewGuidCodeFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode root,
            InvocationExpressionSyntax invocation)
        {
            string title = "Replace Guid.NewGuid() with context.NewGuid()";

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => FixGuidNewGuidUsage(context.Document, root, invocation, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Fixes Guid.NewGuid() method calls by replacing with context.NewGuid().
        /// </summary>
        private static async Task<Document> FixGuidNewGuidUsage(Document document, SyntaxNode root,
            InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            string? orchestratorContextParameter = await FindOrchestratorContextParameter(document, invocation, cancellationToken);
            if (orchestratorContextParameter == null)
            {
                return document;
            }

            // Create the replacement: context.NewGuid()
            InvocationExpressionSyntax replacement = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(orchestratorContextParameter),
                    SyntaxFactory.IdentifierName("NewGuid")))
                .WithArgumentList(SyntaxFactory.ArgumentList()); // NewGuid() takes no arguments

            SyntaxNode newRoot = root.ReplaceNode(invocation, replacement);
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
