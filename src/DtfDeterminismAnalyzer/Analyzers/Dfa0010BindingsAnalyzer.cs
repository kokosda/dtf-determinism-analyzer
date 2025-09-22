using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using DtfDeterminismAnalyzer.Utils;

namespace DtfDeterminismAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects direct binding access in orchestrator functions.
    /// Identifies access to trigger bindings, input/output bindings that should be accessed through activities.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Dfa0010BindingsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(DiagnosticDescriptors.BindingsRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        }

        private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameter = (ParameterSyntax)context.Node;
            
            // Only analyze parameters within orchestrator methods
            var method = parameter.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return;

            if (!OrchestratorContextDetector.IsOrchestratorMethod(method, context.SemanticModel))
                return;

            // Check if parameter has binding attributes
            foreach (var attributeList in parameter.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsBindingAttribute(attribute, context.SemanticModel))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.BindingsRule,
                            attribute.GetLocation(),
                            $"Binding attribute '{attribute.Name}' detected on orchestrator parameter"));
                    }
                }
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

            // Check for direct binding-related API calls
            if (IsBindingApiCall(containingType, methodSymbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.BindingsRule,
                    invocation.GetLocation(),
                    $"Direct binding API call '{GetInvocationDisplayName(invocation)}' detected"));
            }
        }

        private void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifier = (IdentifierNameSyntax)context.Node;
            
            // Skip analysis if not in orchestrator function
            if (!OrchestratorContextDetector.IsNodeWithinOrchestratorMethod(identifier, context.SemanticModel))
                return;

            // Skip if this identifier is part of a member access (handled separately)
            if (identifier.Parent is MemberAccessExpressionSyntax)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
            if (symbolInfo.Symbol is not IParameterSymbol parameterSymbol)
                return;

            // Check if this parameter has binding attributes
            if (HasBindingAttributes(parameterSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.BindingsRule,
                    identifier.GetLocation(),
                    $"Access to binding parameter '{identifier}' detected"));
            }
        }

        private static bool IsBindingAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(attribute);
            if (symbolInfo.Symbol is not IMethodSymbol attributeConstructor)
                return false;

            var attributeClass = attributeConstructor.ContainingType;
            var attributeName = attributeClass?.Name;
            var namespaceName = attributeClass?.ContainingNamespace?.ToDisplayString();

            // Check for Azure Functions binding attributes
            if (namespaceName == "Microsoft.Azure.WebJobs")
            {
                return attributeName is
                    // Storage bindings
                    "BlobAttribute" or "QueueAttribute" or "TableAttribute" or
                    
                    // HTTP bindings
                    "HttpTriggerAttribute" or
                    
                    // Service Bus bindings
                    "ServiceBusAttribute" or "ServiceBusTriggerAttribute" or
                    
                    // Event Hub bindings
                    "EventHubAttribute" or "EventHubTriggerAttribute" or
                    
                    // Cosmos DB bindings
                    "CosmosDBAttribute" or "CosmosDBTriggerAttribute" or
                    
                    // Timer bindings
                    "TimerTriggerAttribute" or
                    
                    // Generic bindings
                    "BindingAttribute" or
                    
                    // Other common bindings
                    "SendGridAttribute" or "TwilioSmsAttribute" or "NotificationHubAttribute" or
                    "MobileTableAttribute" or "DocumentDBAttribute" or
                    "EventGridTriggerAttribute" or "SignalRAttribute" or
                    "DurableClientAttribute";
            }

            // Check for other binding frameworks (if applicable)
            if (namespaceName?.StartsWith("Microsoft.Azure.Functions", StringComparison.Ordinal) == true ||
                namespaceName?.StartsWith("Microsoft.Azure.WebJobs", StringComparison.Ordinal) == true)
            {
                return attributeName?.EndsWith("Attribute", StringComparison.Ordinal) == true &&
                       (attributeName.Contains("Trigger") || attributeName.Contains("Binding") ||
                        IsKnownBindingAttributeName(attributeName));
            }

            return false;
        }

        private static bool IsKnownBindingAttributeName(string attributeName)
        {
            return attributeName is
                "BlobAttribute" or "QueueAttribute" or "TableAttribute" or
                "ServiceBusAttribute" or "EventHubAttribute" or "CosmosDBAttribute" or
                "HttpTriggerAttribute" or "TimerTriggerAttribute" or
                "SendGridAttribute" or "TwilioSmsAttribute" or "NotificationHubAttribute" or
                "SignalRAttribute" or "EventGridTriggerAttribute" or "DurableClientAttribute";
        }

        private static bool HasBindingAttributes(IParameterSymbol parameter)
        {
            return parameter.GetAttributes().Any(attr =>
            {
                var attributeClass = attr.AttributeClass;
                var attributeName = attributeClass?.Name;
                var namespaceName = attributeClass?.ContainingNamespace?.ToDisplayString();

                return namespaceName == "Microsoft.Azure.WebJobs" && 
                       attributeName != null &&
                       IsKnownBindingAttributeName(attributeName);
            });
        }

        private static bool IsBindingApiCall(INamedTypeSymbol containingType, string methodName)
        {
            var typeName = containingType.Name;
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

            // Azure Functions specific APIs that shouldn't be called directly in orchestrators
            if (namespaceName?.StartsWith("Microsoft.Azure.WebJobs", StringComparison.Ordinal) == true)
            {
                // IBinder interface methods
                if (typeName == "IBinder")
                {
                    return methodName is "BindAsync" or "Bind";
                }

                // Collector interfaces
                if (typeName.StartsWith("ICollector", StringComparison.Ordinal) || typeName.StartsWith("IAsyncCollector", StringComparison.Ordinal))
                {
                    return methodName is "Add" or "AddAsync" or "FlushAsync";
                }
            }

            // Storage client direct usage (should go through activities)
            if (namespaceName?.StartsWith("Azure.Storage", StringComparison.Ordinal) == true ||
                namespaceName?.StartsWith("Microsoft.Azure.Storage", StringComparison.Ordinal) == true)
            {
                // Any direct storage operations
                return typeName is "BlobClient" or "BlobContainerClient" or "QueueClient" or 
                       "TableClient" or "CloudBlob" or "CloudBlobContainer" or 
                       "CloudQueue" or "CloudTable";
            }

            // Service Bus client direct usage
            if (namespaceName?.StartsWith("Azure.Messaging.ServiceBus", StringComparison.Ordinal) == true ||
                namespaceName?.StartsWith("Microsoft.Azure.ServiceBus", StringComparison.Ordinal) == true)
            {
                return typeName is "ServiceBusClient" or "ServiceBusSender" or "ServiceBusReceiver" or
                       "MessageSender" or "MessageReceiver" or "QueueClient" or "TopicClient" or
                       "SubscriptionClient";
            }

            // Event Hub client direct usage
            if (namespaceName?.StartsWith("Azure.Messaging.EventHubs", StringComparison.Ordinal) == true ||
                namespaceName?.StartsWith("Microsoft.Azure.EventHubs", StringComparison.Ordinal) == true)
            {
                return typeName is "EventHubProducerClient" or "EventHubConsumerClient" or
                       "EventHubClient" or "PartitionSender" or "PartitionReceiver";
            }

            // Cosmos DB client direct usage
            if (namespaceName?.StartsWith("Microsoft.Azure.Cosmos", StringComparison.Ordinal) == true ||
                namespaceName?.StartsWith("Microsoft.Azure.Documents", StringComparison.Ordinal) == true)
            {
                return typeName is "CosmosClient" or "Database" or "Container" or
                       "DocumentClient" or "IDocumentClient";
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