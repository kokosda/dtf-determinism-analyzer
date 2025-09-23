using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0008NonDurableAsyncAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0008: Non-durable async operation detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-durable async operations and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0008NonDurableAsyncTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task TaskDelayInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.Delay is not replay-safe
        await {|#0:Task.Delay(TimeSpan.FromMinutes(5))|};
        await context.CallActivityAsync(""ProcessAfterDelay"", ""data"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task HttpClientGetAsyncInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Direct HTTP calls are not replay-safe
        var response = await {|#0:_httpClient.GetAsync(""https://api.example.com/data"")|};
        var content = await response.Content.ReadAsStringAsync();
        await context.CallActivityAsync(""ProcessData"", content);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task TaskRunInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.Run is not replay-safe
        var result = await {|#0:Task.Run(() => ""some computation"")|};
        await context.CallActivityAsync(""ProcessResult"", result);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task TaskFromResultInOrchestratorShouldNotReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.FromResult is deterministic and safe
        var result = await Task.FromResult(""immediate value"");
        await context.CallActivityAsync(""ProcessResult"", result);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task ConfigureAwaitFalseInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // ConfigureAwait(false) is not replay-safe in orchestrators
        await {|#0:context.CallActivityAsync(""GetData"", ""input"").ConfigureAwait(false)|};
        await context.CallActivityAsync(""ProcessData"", ""done"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task TaskWhenAllNonDurableTasksInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.WhenAll with non-durable tasks should be detected
        var task1 = _httpClient.GetAsync(""https://api1.example.com"");
        var task2 = _httpClient.GetAsync(""https://api2.example.com"");
        await {|#0:Task.WhenAll(task1, task2)|};
        await context.CallActivityAsync(""ProcessResults"", ""done"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task TaskWhenAllDurableTasksInOrchestratorShouldNotReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.WhenAll with durable tasks should be allowed
        var task1 = context.CallActivityAsync(""Activity1"", ""data1"");
        var task2 = context.CallActivityAsync(""Activity2"", ""data2"");
        await Task.WhenAll(task1, task2);
        await context.CallActivityAsync(""ProcessResults"", ""done"");
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task DurableCreateTimerInOrchestratorShouldNotReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Durable timers should be allowed - they're replay-safe
        var deadline = context.CurrentUtcDateTime.AddMinutes(5);
        await context.CreateTimer(deadline, System.Threading.CancellationToken.None);
        await context.CallActivityAsync(""ProcessAfterDelay"", ""data"");
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TaskDelayInActivityFunctionShouldNotReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string input)
    {
        // Task.Delay in activities should be allowed
        await Task.Delay(TimeSpan.FromSeconds(1));
        return ""processed: "" + input;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleNonDurableOperationsInOrchestratorShouldReportMultipleDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await {|#0:Task.Delay(1000)|};
        var response = await {|#1:_httpClient.GetAsync(""https://api.example.com"")|};
        var result = await {|#2:Task.Run(() => response.Content.ReadAsStringAsync())|};
        await context.CallActivityAsync(""ProcessResult"", ""done"");
    }
}";

            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic("DFA0008").WithLocation(0).WithMessage("Non-durable async operation detected."),
                VerifyCS.Diagnostic("DFA0008").WithLocation(1).WithMessage("Non-durable async operation detected."),
                VerifyCS.Diagnostic("DFA0008").WithLocation(2).WithMessage("Non-durable async operation detected.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task NonDurableOperationInNestedMethodInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await DelayThenProcess();
        await context.CallActivityAsync(""ProcessAfterDelay"", ""data"");
    }

    private async Task DelayThenProcess()
    {
        // Should be detected even in helper methods within orchestrator class
        await {|#0:Task.Delay(2000)|};
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task TaskYieldInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.Yield is not replay-safe
        await {|#0:Task.Yield()|};
        await context.CallActivityAsync(""ProcessAfterYield"", ""data"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task CustomAsyncMethodInOrchestratorShouldReportDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Custom async methods that aren't durable should be detected
        var result = await {|#0:GetDataFromExternalService()|};
        await context.CallActivityAsync(""ProcessResult"", result);
    }

    private async Task<string> GetDataFromExternalService()
    {
        // This method makes non-durable calls
        using var client = new HttpClient();
        var response = await client.GetAsync(""https://external.api.com/data"");
        return await response.Content.ReadAsStringAsync();
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0008")
                .WithLocation(0)
                .WithMessage("Non-durable async operation detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
