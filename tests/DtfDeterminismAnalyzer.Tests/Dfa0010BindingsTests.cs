using System;
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
    public class Dfa0010BindingsTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
";

        [Test]
        public async Task BlobTriggerInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:Blob(""container/blob.txt"")|} ] Stream blob)
    {
        // Direct blob binding should not be used in orchestrators
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        await context.CallActivityAsync(""ProcessBlob"", content);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task QueueTriggerInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:Queue(""myqueue"")|} ] string queueItem)
    {
        // Direct queue binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessQueueItem"", queueItem);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ServiceBusTriggerInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:ServiceBusTrigger(""mytopic"", ""mysubscription"")|} ] string message)
    {
        // Direct Service Bus binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessMessage"", message);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task CosmosDBTriggerInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:CosmosDBTrigger(databaseName: ""mydb"", collectionName: ""mycoll"")|} ] string document)
    {
        // Direct Cosmos DB binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessDocument"", document);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task HttpTriggerInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<IActionResult> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:HttpTrigger(AuthorizationLevel.Function, ""get"", ""post"")|} ] HttpRequest req)
    {
        // Direct HTTP binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessRequest"", ""data"");
        return new OkResult();
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task TableBindingInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:Table(""MyTable"")|} ] IAsyncCollector<dynamic> table)
    {
        // Direct table binding should not be used in orchestrators
        await table.AddAsync(new { Data = ""test"" });
        await context.CallActivityAsync(""ProcessTable"", ""data"");
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ILoggerInOrchestratorShouldNotReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task ActivityTriggerInActivityFunctionShouldNotReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleBindingsInOrchestratorShouldReportMultipleDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:Queue(""queue1"")|} ] string queueItem,
        [{|#1:Blob(""container/blob.txt"")|} ] Stream blob,
        [{|#2:Table(""MyTable"")|} ] IAsyncCollector<dynamic> table)
    {
        // Multiple direct bindings should all be detected
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        
        await table.AddAsync(new { Data = queueItem, Content = content });
        await context.CallActivityAsync(""ProcessAll"", ""data"");
    }
}";

            var expected = new[]
            {
                VerifyCS.Diagnostic("DFA0010").WithLocation(0).WithMessage("Direct binding usage detected in orchestrator."),
                VerifyCS.Diagnostic("DFA0010").WithLocation(1).WithMessage("Direct binding usage detected in orchestrator."),
                VerifyCS.Diagnostic("DFA0010").WithLocation(2).WithMessage("Direct binding usage detected in orchestrator.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task CustomBindingInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:CustomBinding(""config"")|} ] string customData)
    {
        // Custom bindings should also be detected
        await context.CallActivityAsync(""ProcessCustom"", customData);
    }
}

public class CustomBindingAttribute : Attribute
{
    public CustomBindingAttribute(string config) { }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EventHubTriggerInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:EventHubTrigger(""myeventhub"")|} ] string eventData)
    {
        // Direct Event Hub binding should not be used in orchestrators
        await context.CallActivityAsync(""ProcessEvent"", eventData);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task SignalRBindingInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:SignalR(HubName = ""chat"")|} ] IAsyncCollector<SignalRMessage> signalRMessages)
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

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task SqlBindingInOrchestratorShouldReportDFA0010()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [{|#0:Sql(""SELECT * FROM MyTable"", ""SqlConnectionString"")|} ] IEnumerable<dynamic> sqlData)
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

            var expected = VerifyCS.Diagnostic("DFA0010")
                .WithLocation(0)
                .WithMessage("Direct binding usage detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}