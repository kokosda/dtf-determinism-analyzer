using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0007ThreadBlockingAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0007: Thread blocking detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects thread-blocking operations and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0007ThreadBlockingTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task ThreadSleepInOrchestratorShouldReportDFA0007()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:Thread.Sleep(1000)|};
        await context.CallActivityAsync(""SomeActivity"", ""data"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:task.Wait()|};
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        var result = {|#0:task.Result|};
        await context.CallActivityAsync(""ProcessResult"", result);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:Task.WaitAll(task1, task2)|};
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        var completedIndex = {|#0:Task.WaitAny(task1, task2)|};
        await context.CallActivityAsync(""ProcessCompleted"", completedIndex);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:_event.WaitOne()|};
        await context.CallActivityAsync(""ProcessAfterEvent"", ""data"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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
        {|#0:Thread.Sleep(1000)|};
        var task = context.CallActivityAsync(""SomeActivity"", ""data"");
        {|#1:task.Wait()|};
        
        await context.CallActivityAsync(""ProcessCompleted"", ""done"");
    }
}";

            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic("DFA0007").WithLocation(0).WithMessage("Thread-blocking call detected."),
                VerifyCS.Diagnostic("DFA0007").WithLocation(1).WithMessage("Thread-blocking call detected.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
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
        {|#0:Thread.Sleep(2000)|};
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:_semaphore.WaitOne()|};
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

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
            {|#0:Monitor.Wait(_lockObject)|};
        }
        await context.CallActivityAsync(""ProcessAfterWait"", ""data"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0007")
                .WithLocation(0)
                .WithMessage("Thread-blocking call detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
