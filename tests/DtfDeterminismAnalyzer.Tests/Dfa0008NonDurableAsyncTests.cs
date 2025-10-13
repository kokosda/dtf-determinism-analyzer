using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0008: Non-durable async operation detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-durable async operations and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0008NonDurableAsyncTests : AnalyzerTestBase<Analyzers.Dfa0008NonDurableAsyncAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        private async Task VerifyDFA0008Diagnostic(string testCode)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0008").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0008 diagnostic");
            Microsoft.CodeAnalysis.Diagnostic diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Non-durable async operation detected", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                "Diagnostic message should match expected message");
        }

        private async Task VerifyNoDiagnostics(string testCode)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0008").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0008 diagnostics");
        }

        private async Task VerifyMultipleDFA0008Diagnostics(string testCode, int expectedCount)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0008").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0008 diagnostics");
            foreach (Microsoft.CodeAnalysis.Diagnostic? diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Non-durable async operation detected", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task RunAnalyzer_WithTaskDelayInOrchestrator_ReportsDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.Delay is not replay-safe
        await Task.Delay(TimeSpan.FromMinutes(5));
        await context.CallActivityAsync(""ProcessAfterDelay"", ""data"");
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithHttpClientGetAsyncInOrchestrator_ReportsDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Direct HTTP calls are not replay-safe
        var response = await _httpClient.GetAsync(""https://api.example.com/data"");
        var content = await response.Content.ReadAsStringAsync();
        await context.CallActivityAsync(""ProcessData"", content);
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithTaskRunInOrchestrator_ReportsDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.Run is not replay-safe
        var result = await Task.Run(() => ""some computation"");
        await context.CallActivityAsync(""ProcessResult"", result);
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithTaskFromResultInOrchestrator_DoesNotReportDFA0008()
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

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithConfigureAwaitFalseInOrchestrator_ReportsDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // ConfigureAwait(false) is not replay-safe in orchestrators
        await context.CallActivityAsync(""GetData"", ""input"").ConfigureAwait(false);
        await context.CallActivityAsync(""ProcessData"", ""done"");
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithTaskWhenAllNonDurableTasksInOrchestrator_ReportsDFA0008()
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
        await Task.WhenAll(task1, task2);
        await context.CallActivityAsync(""ProcessResults"", ""done"");
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithTaskWhenAllDurableTasksInOrchestrator_DoesNotReportDFA0008()
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

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithDurableCreateTimerInOrchestrator_DoesNotReportDFA0008()
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

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithTaskDelayInActivityFunction_DoesNotReportDFA0008()
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

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithMultipleNonDurableOperationsInOrchestrator_ReportsMultipleDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await Task.Delay(1000);
        var response = await _httpClient.GetAsync(""https://api.example.com"");
        var result = await Task.Run(() => response.Content.ReadAsStringAsync());
        await context.CallActivityAsync(""ProcessResult"", ""done"");
    }
}";

            await VerifyMultipleDFA0008Diagnostics(testCode, 3);        }

        [Test]
        public async Task RunAnalyzer_WithNonDurableOperationInNestedMethodInOrchestrator_ReportsDFA0008()
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
        await Task.Delay(2000);
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithTaskYieldInOrchestrator_ReportsDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Task.Yield is not replay-safe
        await Task.Yield();
        await context.CallActivityAsync(""ProcessAfterYield"", ""data"");
    }
}";

            await VerifyDFA0008Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithCustomAsyncMethodInOrchestrator_ReportsDFA0008()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Custom async methods that aren't durable should be detected
        var result = await GetDataFromExternalService();
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

            await VerifyDFA0008Diagnostic(testCode);        }

        #region TaskOrchestrationContext Tests (Core DTF)

        private const string TaskOrchestrationContextUsing = @"
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DurableTask;
";

        [Test]
        public async Task RunAnalyzer_WithTaskOrchestrationContext_TaskDelay_ReportsDFA0008()
        {
            string testCode = TaskOrchestrationContextUsing + @"
public class TestOrchestrator
{
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        await Task.Delay(1000);
        return $""Processed: {input}"";
    }
}";

            await VerifyDFA0008Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithTaskOrchestrationContext_HttpClientGetAsync_ReportsDFA0008()
        {
            string testCode = TaskOrchestrationContextUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        var response = await httpClient.GetStringAsync(""https://api.example.com/data"");
        return $""Input: {input}, Response: {response}"";
    }
}";

            await VerifyDFA0008Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithTaskOrchestrationContext_TaskRun_ReportsDFA0008()
        {
            string testCode = TaskOrchestrationContextUsing + @"
public class TestOrchestrator
{
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        var result = await Task.Run(() => ProcessInput(input));
        return result;
    }
    
    private static string ProcessInput(string input)
    {
        return $""Processed: {input}"";
    }
}";

            await VerifyDFA0008Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithTaskOrchestrationContext_TaskFromResult_ReportsNoDiagnostics()
        {
            string testCode = TaskOrchestrationContextUsing + @"
public class TestOrchestrator
{
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // Task.FromResult is synchronous and deterministic
        var result = await Task.FromResult($""Processed: {input}"");
        return result;
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithTaskOrchestrationContext_NoAsyncOperations_ReportsNoDiagnostics()
        {
            string testCode = TaskOrchestrationContextUsing + @"
public class TestOrchestrator
{
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // No async operations - should not trigger DFA0008
        return $""Processed: {input}"";
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        #endregion
    }
}
