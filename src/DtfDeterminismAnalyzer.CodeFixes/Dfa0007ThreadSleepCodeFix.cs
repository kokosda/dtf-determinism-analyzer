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
    /// Code fix provider for DFA0007: Replaces Thread.Sleep calls with durable timer alternatives.
    /// Provides automatic fixes for Thread.Sleep(int) and Thread.Sleep(TimeSpan) calls 
    /// in Durable Task Framework orchestrator functions.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Dfa0007ThreadSleepCodeFix)), Shared]
    public class Dfa0007ThreadSleepCodeFix : CodeFixProvider
    {
        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can handle.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create("DFA0007");

        /// <summary>
        /// Gets the fix-all provider that can fix all instances of this diagnostic at once.
        /// </summary>
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <summary>
        /// Registers code fixes for the specified diagnostic reports.
        /// </summary>
        /// <param name="context">Context that contains the diagnostics to fix and allows registration of code actions.</param>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var invocation = root.FindNode(diagnosticSpan) as InvocationExpressionSyntax;
                if (invocation == null)
                    continue;

                // Check if this is a Thread.Sleep call
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var memberName = memberAccess.Name.Identifier.ValueText;
                    var typeName = GetTypeNameFromMemberAccess(memberAccess);

                    if (typeName == "Thread" && memberName == "Sleep")
                    {
                        await RegisterThreadSleepCodeFix(context, diagnostic, root, invocation);
                    }
                }
            }
        }

        /// <summary>
        /// Registers a code fix for Thread.Sleep calls.
        /// </summary>
        private static async Task RegisterThreadSleepCodeFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode root,
            InvocationExpressionSyntax invocation)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            if (semanticModel == null)
                return;

            // Determine the argument type to provide appropriate fix title
            var argumentList = invocation.ArgumentList;
            if (argumentList.Arguments.Count != 1)
                return;

            var argument = argumentList.Arguments[0];
            var argumentType = semanticModel.GetTypeInfo(argument.Expression, context.CancellationToken).Type;
            
            string title;
            if (argumentType?.Name == "Int32")
            {
                title = "Replace Thread.Sleep(int) with await context.CreateTimer()";
            }
            else if (argumentType?.Name == "TimeSpan")
            {
                title = "Replace Thread.Sleep(TimeSpan) with await context.CreateTimer()";
            }
            else
            {
                title = "Replace Thread.Sleep() with await context.CreateTimer()";
            }

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => FixThreadSleepUsage(context.Document, root, invocation, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Fixes Thread.Sleep calls by replacing with await context.CreateTimer pattern.
        /// </summary>
        private static async Task<Document> FixThreadSleepUsage(Document document, SyntaxNode root,
            InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var orchestratorContextParameter = await FindOrchestratorContextParameter(document, invocation, cancellationToken);
            if (orchestratorContextParameter == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
                return document;

            // Get the sleep argument
            var argumentList = invocation.ArgumentList;
            if (argumentList.Arguments.Count != 1)
                return document;

            var argument = argumentList.Arguments[0];
            var argumentType = semanticModel.GetTypeInfo(argument.Expression, cancellationToken).Type;

            ExpressionSyntax delayExpression;
            
            // Convert the delay argument to appropriate DateTime expression
            if (argumentType?.Name == "Int32")
            {
                // Thread.Sleep(milliseconds) -> context.CurrentUtcDateTime.AddMilliseconds(milliseconds)
                delayExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(orchestratorContextParameter),
                            SyntaxFactory.IdentifierName("CurrentUtcDateTime")),
                        SyntaxFactory.IdentifierName("AddMilliseconds")))
                    .WithArgumentList(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(argument)));
            }
            else if (argumentType?.Name == "TimeSpan")
            {
                // Thread.Sleep(timespan) -> context.CurrentUtcDateTime.Add(timespan)
                delayExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(orchestratorContextParameter),
                            SyntaxFactory.IdentifierName("CurrentUtcDateTime")),
                        SyntaxFactory.IdentifierName("Add")))
                    .WithArgumentList(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(argument)));
            }
            else
            {
                // Fallback: assume milliseconds
                delayExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(orchestratorContextParameter),
                            SyntaxFactory.IdentifierName("CurrentUtcDateTime")),
                        SyntaxFactory.IdentifierName("AddMilliseconds")))
                    .WithArgumentList(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(argument)));
            }

            // Create the replacement: await context.CreateTimer(delayExpression, CancellationToken.None)
            var createTimerCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(orchestratorContextParameter),
                    SyntaxFactory.IdentifierName("CreateTimer")))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(new[]
                    {
                        SyntaxFactory.Argument(delayExpression),
                        SyntaxFactory.Argument(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("CancellationToken"),
                                SyntaxFactory.IdentifierName("None")))
                    })));

            var awaitExpression = SyntaxFactory.AwaitExpression(createTimerCall);

            // Check if we need to wrap in an expression statement
            ExpressionSyntax replacement;
            if (invocation.Parent is ExpressionStatementSyntax)
            {
                // If Thread.Sleep was a standalone statement, replace with await expression
                replacement = awaitExpression;
            }
            else
            {
                // If Thread.Sleep was part of a larger expression, just replace with the await
                replacement = awaitExpression;
            }

            var newRoot = root.ReplaceNode(invocation, replacement);

            // Make sure the containing method is async
            newRoot = EnsureMethodIsAsync(newRoot, invocation, cancellationToken);

            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Ensures the containing method is marked as async and returns Task.
        /// </summary>
        private static SyntaxNode EnsureMethodIsAsync(SyntaxNode root, SyntaxNode originalNode, CancellationToken cancellationToken)
        {
            var method = originalNode.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return root;

            // Check if method is already async
            if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return root;

            // Add async modifier
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
            var newModifiers = method.Modifiers.Add(asyncModifier);

            // Update return type to Task if it's void
            TypeSyntax newReturnType = method.ReturnType;
            if (method.ReturnType is PredefinedTypeSyntax predefinedType && 
                predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                newReturnType = SyntaxFactory.IdentifierName("Task");
            }

            var newMethod = method
                .WithModifiers(newModifiers)
                .WithReturnType(newReturnType);

            return root.ReplaceNode(method, newMethod);
        }

        /// <summary>
        /// Finds the orchestrator context parameter in the containing method.
        /// </summary>
        private static async Task<string?> FindOrchestratorContextParameter(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
                return null;

            // Find the containing method
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return null;

            // Look for a parameter with orchestration context type
            foreach (var parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type == null)
                    continue;

                var typeInfo = semanticModel.GetTypeInfo(parameter.Type, cancellationToken);
                if (typeInfo.Type == null)
                    continue;

                var typeName = typeInfo.Type.ToDisplayString();
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