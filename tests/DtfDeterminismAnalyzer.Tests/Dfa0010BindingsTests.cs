using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0010BindingsAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0010: Direct bindings usage detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects direct binding usage and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0010BindingsTests : AnalyzerTestBase<Analyzers.Dfa0010BindingsAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
";

        // Helper methods for test verification
        private async Task VerifyDFA0010Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(1), "Expected exactly one diagnostic");
            Assert.That(result.AnalyzerDiagnostics[0].Id, Is.EqualTo("DFA0010"), "Expected DFA0010 diagnostic");
            Assert.That(result.AnalyzerDiagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Direct binding usage detected in orchestrator"), "Expected correct diagnostic message");
        }

        private async Task VerifyNoDiagnostics(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(0), "Expected no diagnostics");
        }

        private async Task VerifyMultipleDFA0010Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(expectedCount), $"Expected exactly {expectedCount} diagnostics");
            foreach (var diagnostic in result.AnalyzerDiagnostics)
            {
                Assert.That(diagnostic.Id, Is.EqualTo("DFA0010"), "Expected DFA0010 diagnostic");
                Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Direct binding usage detected in orchestrator"), "Expected correct diagnostic message");
            }
        }

        [Test]
        public async Task RunAnalyzer_WithBlobTriggerInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [BlobTrigger(""container/{name}"")] Stream blob)
    {
        // Direct blob binding should not be used in orchestrators
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        await context.CallActivityAsync(""ProcessBlob"", content);
    }
}

public class BlobTriggerAttribute : Attribute
{
    public string Path { get; }
    public BlobTriggerAttribute(string path) => Path = path;
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithQueueTriggerInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [QueueTrigger(""myqueue"")] string queueItem)
    {
        // Direct queue binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessQueueItem"", queueItem);
    }
}

public class QueueTriggerAttribute : Attribute
{
    public string QueueName { get; }
    public QueueTriggerAttribute(string queueName) => QueueName = queueName;
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithServiceBusTriggerInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ServiceBusTrigger(""myqueue"")] string message)
    {
        // Direct Service Bus binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessMessage"", message);
    }
}

public class ServiceBusTriggerAttribute : Attribute
{
    public string QueueName { get; }
    public ServiceBusTriggerAttribute(string queueName) => QueueName = queueName;
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithCosmosDBTriggerInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [CosmosDBTrigger(""databaseName"", ""collectionName"")] string document)
    {
        // Direct Cosmos DB binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessDocument"", document);
    }
}

public class CosmosDBTriggerAttribute : Attribute
{
    public string DatabaseName { get; }
    public string CollectionName { get; }
    public CosmosDBTriggerAttribute(string databaseName, string collectionName)
    {
        DatabaseName = databaseName;
        CollectionName = collectionName;
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithHttpTriggerInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<IActionResult> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [HttpTrigger(AuthorizationLevel.Function, ""get"", ""post"")] HttpRequest req)
    {
        // Direct HTTP binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessRequest"", ""data"");
        return new OkResult();
    }
}

public class HttpTriggerAttribute : Attribute
{
    public AuthorizationLevel AuthLevel { get; }
    public string[] Methods { get; }
    public HttpTriggerAttribute(AuthorizationLevel authLevel, params string[] methods)
    {
        AuthLevel = authLevel;
        Methods = methods;
    }
}

public enum AuthorizationLevel
{
    Anonymous,
    User,
    Function,
    System,
    Admin
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithTableBindingInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [Table(""mytable"")] IAsyncCollector<dynamic> table)
    {
        // Direct table binding should not be used in orchestrators
        await table.AddAsync(new { Data = ""test"" });
        await context.CallActivityAsync(""ProcessTable"", ""data"");
    }
}

public class TableAttribute : Attribute
{
    public string TableName { get; }
    public TableAttribute(string tableName) => TableName = tableName;
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithILoggerInOrchestrator_DoesNotReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        // ILogger should be allowed in orchestrators (though with caveats)
        log.LogInformation(""Orchestrator started"");
        await context.CallActivityAsync(""ProcessData"", ""data"");
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithActivityTriggerInActivityFunction_DoesNotReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity(
        [ActivityTrigger] string input,
        [Blob(""container/blob.txt"")] Stream blob,
        ILogger log)
    {
        // Direct bindings in activities should be allowed
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        log.LogInformation($""Processing: {input}"");
        return ""processed: "" + content;
    }
}

public class BlobAttribute : Attribute
{
    public string Path { get; }
    public BlobAttribute(string path) => Path = path;
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithMultipleBindingsInOrchestrator_ReportsMultipleDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [QueueTrigger(""queue1"")] string queueItem,
        [BlobTrigger(""container/{name}"")] Stream blob,
        [Table(""mytable"")] IAsyncCollector<dynamic> table)
    {
        // Multiple direct bindings should all be detected
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        
        await table.AddAsync(new { Data = queueItem, Content = content });
        await context.CallActivityAsync(""ProcessAll"", ""data"");
    }
}

public class QueueTriggerAttribute : Attribute
{
    public string QueueName { get; }
    public QueueTriggerAttribute(string queueName) => QueueName = queueName;
}

public class BlobTriggerAttribute : Attribute
{
    public string Path { get; }
    public BlobTriggerAttribute(string path) => Path = path;
}

public class TableAttribute : Attribute
{
    public string TableName { get; }
    public TableAttribute(string tableName) => TableName = tableName;
}";

            await VerifyMultipleDFA0010Diagnostics(testCode, 3);        }

        [Test]
        public async Task RunAnalyzer_WithCustomBindingInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [CustomBinding(""config"")] string customData)
    {
        // Custom bindings should also be detected
        await context.CallActivityAsync(""ProcessCustom"", customData);
    }
}

public class CustomBindingAttribute : Attribute
{
    public CustomBindingAttribute(string config) { }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEventHubTriggerInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [EventHubTrigger(""eventhub"", Connection = ""EventHubConnection"")] string eventData)
    {
        // Direct Event Hub binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessEvent"", eventData);
    }
}

public class EventHubTriggerAttribute : Attribute
{
    public string EventHubName { get; }
    public string Connection { get; set; }
    public EventHubTriggerAttribute(string eventHubName) => EventHubName = eventHubName;
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithSignalRBindingInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [SignalR(HubName = ""testhub"")] IAsyncCollector<SignalRMessage> signalRMessages)
    {
        // Direct SignalR binding should not be used in orchestrators
        await signalRMessages.AddAsync(new SignalRMessage { Target = ""notify"", Arguments = new[] { ""test"" } });
        await context.CallActivityAsync(""ProcessNotification"", ""data"");
    }
}

public class SignalRAttribute : Attribute
{
    public string HubName { get; set; }
}

public class SignalRMessage
{
    public string Target { get; set; }
    public object[] Arguments { get; set; }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithSqlBindingInOrchestrator_ReportsDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [Sql(""SELECT * FROM table"", ""SqlConnection"")] string sqlData)
    {
        // Direct SQL binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessSqlResults"", sqlData);
    }
}

public class SqlAttribute : Attribute
{
    public string CommandText { get; }
    public string ConnectionStringSetting { get; }
    public SqlAttribute(string commandText, string connectionStringSetting) 
    { 
        CommandText = commandText;
        ConnectionStringSetting = connectionStringSetting;
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }
    }
}
