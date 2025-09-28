using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0007: Thread blocking detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects thread-blocking operations and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0007ThreadBlockingTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0007ThreadBlockingAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        private async Task VerifyDFA0007Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0007").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0007 diagnostic");
            var diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Thread-blocking call detected", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                "Diagnostic message should match expected message");
        }

        private async Task VerifyNoDiagnostics(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0007").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0007 diagnostics");
        }

        private async Task VerifyMultipleDFA0007Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0007").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0007 diagnostics");
            foreach (var diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Thread-blocking call detected", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task ThreadSleepInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Thread.Sleep(1000);
        await context.CallActivityAsync(""SomeActivity"", ""data"");
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task TaskWaitInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var task = context.CallActivityAsync(""SomeActivity"", ""data"");
        task.Wait();
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task TaskResultInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var task = context.CallActivityAsync<string>(""GetData"", ""input"");
        var result = task.Result;
        await context.CallActivityAsync(""ProcessResult"", result);
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task TaskWaitAllInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var task1 = context.CallActivityAsync(""Activity1"", ""data1"");
        var task2 = context.CallActivityAsync(""Activity2"", ""data2"");
        Task.WaitAll(task1, task2);
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task TaskWaitAnyInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var task1 = context.CallActivityAsync(""Activity1"", ""data1"");
        var task2 = context.CallActivityAsync(""Activity2"", ""data2"");
        var completedIndex = Task.WaitAny(task1, task2);
        await context.CallActivityAsync(""ProcessCompleted"", completedIndex);
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task ManualResetEventWaitOneInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly ManualResetEvent _event = new ManualResetEvent(false);

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        _event.WaitOne();
        await context.CallActivityAsync(""ProcessAfterEvent"", ""data"");
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task DurableTimerInOrchestratorShouldNotReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Durable timers should be allowed - they're replay-safe
        var deadline = context.CurrentUtcDateTime.AddMinutes(5);
        await context.CreateTimer(deadline, CancellationToken.None);
        await context.CallActivityAsync(""ProcessAfterDelay"", ""data"");
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task AwaitTasksInOrchestratorShouldNotReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Awaiting tasks should be allowed - this is the correct pattern
        var result = await context.CallActivityAsync<string>(""GetData"", ""input"");
        await context.CallActivityAsync(""ProcessResult"", result);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task ThreadSleepInActivityFunctionShouldNotReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string input)
    {
        // Thread.Sleep in activities should be allowed (though not recommended)
        Thread.Sleep(100);
        return ""processed: "" + input;
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleBlockingCallsInOrchestratorShouldReportMultipleDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Thread.Sleep(1000);
        var task = context.CallActivityAsync(""SomeActivity"", ""data"");
        task.Wait();
        
        await context.CallActivityAsync(""ProcessCompleted"", ""done"");
    }
}";

            await VerifyMultipleDFA0007Diagnostics(testCode, 2);
        }

        [Test]
        public async Task BlockingCallInNestedMethodInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        WaitForCompletion();
        await context.CallActivityAsync(""ProcessAfterWait"", ""data"");
    }

    private void WaitForCompletion()
    {
        // Should be detected even in helper methods within orchestrator class
        Thread.Sleep(2000);
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task SemaphoreWaitOneInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Semaphore _semaphore = new Semaphore(1, 1);

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        _semaphore.WaitOne();
        try
        {
            await context.CallActivityAsync(""CriticalSection"", ""data"");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }

        [Test]
        public async Task MonitorWaitInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lockObject = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        lock (_lockObject)
        {
            Monitor.Wait(_lockObject);
        }
        await context.CallActivityAsync(""ProcessAfterWait"", ""data"");
    }
}";

            await VerifyDFA0007Diagnostic(testCode);        }
    }
}
