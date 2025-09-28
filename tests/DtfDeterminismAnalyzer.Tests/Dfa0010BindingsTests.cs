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
            Assert.That(result.AnalyzerDiagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Direct binding usage detected."), "Expected correct diagnostic message");
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
                Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Direct binding usage detected."), "Expected correct diagnostic message");
            }
        }

        [Test]
        public async Task BlobTriggerInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] Stream blob)
    {
        // Direct blob binding should not be used in orchestrators
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        await context.CallActivityAsync(""ProcessBlob"", content);
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task QueueTriggerInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] string queueItem)
    {
        // Direct queue binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessQueueItem"", queueItem);
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task ServiceBusTriggerInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] string message)
    {
        // Direct Service Bus binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessMessage"", message);
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task CosmosDBTriggerInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] string document)
    {
        // Direct Cosmos DB binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessDocument"", document);
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task HttpTriggerInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<IActionResult> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] HttpRequest req)
    {
        // Direct HTTP binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessRequest"", ""data"");
        return new OkResult();
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task TableBindingInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] IAsyncCollector<dynamic> table)
    {
        // Direct table binding should not be used in orchestrators
        await table.AddAsync(new { Data = ""test"" });
        await context.CallActivityAsync(""ProcessTable"", ""data"");
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task ILoggerInOrchestratorShouldNotReportDFA0010()
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
        public async Task ActivityTriggerInActivityFunctionShouldNotReportDFA0010()
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
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleBindingsInOrchestratorShouldReportMultipleDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] string queueItem,
        [ ] Stream blob,
        [ ] IAsyncCollector<dynamic> table)
    {
        // Multiple direct bindings should all be detected
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        
        await table.AddAsync(new { Data = queueItem, Content = content });
        await context.CallActivityAsync(""ProcessAll"", ""data"");
    }
}";

            await VerifyMultipleDFA0010Diagnostics(testCode, 3);        }

        [Test]
        public async Task CustomBindingInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] string customData)
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
        public async Task EventHubTriggerInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] string eventData)
    {
        // Direct Event Hub binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessEvent"", eventData);
    }
}";

            await VerifyDFA0010Diagnostic(testCode);        }

        [Test]
        public async Task SignalRBindingInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] IAsyncCollector<SignalRMessage> signalRMessages)
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
        public async Task SqlBindingInOrchestratorShouldReportDFA0010()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [ ] IEnumerable<dynamic> sqlData)
    {
        // Direct SQL binding should not be used in orchestrators
        var results = sqlData.ToList();
        await context.CallActivityAsync(""ProcessSqlResults"", results.Count);
    }
}

public class SqlAttribute : Attribute
{
    public SqlAttribute(string commandText, string connectionStringSetting) { }
}";

            await VerifyDFA0010Diagnostic(testCode);        }
    }
}
