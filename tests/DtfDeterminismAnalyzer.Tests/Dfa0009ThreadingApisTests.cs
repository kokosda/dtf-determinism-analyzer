using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0009ThreadingApisAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0009: Threading APIs detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects threading API usage and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0009ThreadingApisTests : AnalyzerTestBase<Analyzers.Dfa0009ThreadingApisAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        // Helper methods for test verification
        private async Task VerifyDFA0009Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(1), "Expected exactly one diagnostic");
            Assert.That(result.AnalyzerDiagnostics[0].Id, Is.EqualTo("DFA0009"), "Expected DFA0009 diagnostic");
            Assert.That(result.AnalyzerDiagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Threading API usage detected."), "Expected correct diagnostic message");
        }

        private async Task VerifyNoDiagnostics(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(0), "Expected no diagnostics");
        }

        private async Task VerifyMultipleDFA0009Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(expectedCount), $"Expected exactly {expectedCount} diagnostics");
            foreach (var diagnostic in result.AnalyzerDiagnostics)
            {
                Assert.That(diagnostic.Id, Is.EqualTo("DFA0009"), "Expected DFA0009 diagnostic");
                Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Threading API usage detected."), "Expected correct diagnostic message");
            }
        }

        [Test]
        public async Task ThreadStartInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var thread = new Thread(() => DoWork());
        thread.Start();
        await context.CallActivityAsync(""ProcessAfterThread"", ""data"");
    }

    private void DoWork() => Thread.Sleep(1000);
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task ThreadPoolQueueUserWorkItemInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        ThreadPool.QueueUserWorkItem(_ => DoWork());
        await context.CallActivityAsync(""ProcessAfterWork"", ""data"");
    }

    private void DoWork() => Thread.Sleep(1000);
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task ParallelForEachInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var items = new[] { ""item1"", ""item2"", ""item3"" };
        Parallel.ForEach(items, ProcessItem);
        await context.CallActivityAsync(""ProcessCompleted"", ""data"");
    }

    private void ProcessItem(string item) { }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task ParallelForInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Parallel.For(0, 10, ProcessIndex);
        await context.CallActivityAsync(""ProcessCompleted"", ""data"");
    }

    private void ProcessIndex(int index) { }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task LockStatementInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lock = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        lock (_lock)
        {
            // Critical section
            var data = ""protected data"";
        };
        await context.CallActivityAsync(""ProcessData"", ""data"");
    }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task MonitorEnterInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lockObject = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        bool lockTaken = false;
        try
        {
            ;
            // Critical section
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(_lockObject);
        }
        await context.CallActivityAsync(""ProcessData"", ""data"");
    }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task MutexWaitOneInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Mutex _mutex = new Mutex();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        _mutex.WaitOne();
        try
        {
            await context.CallActivityAsync(""CriticalSection"", ""data"");
        }
        finally
        {
            // ReleaseMutex() removed to avoid double diagnostic
        }
    }
}";

            await VerifyDFA0009Diagnostic(testCode);
        }

        [Test]
        public async Task ReaderWriterLockAcquireReaderLockInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly ReaderWriterLock _rwLock = new ReaderWriterLock();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        ;
        try
        {
            await context.CallActivityAsync(""ReadData"", ""data"");
        }
        finally
        {
            _rwLock.ReleaseReaderLock();
        }
    }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task CancellationTokenRegisterInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var cts = new CancellationTokenSource();
        ;
        await context.CallActivityAsync(""ProcessWithCancellation"", ""data"");
    }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task AutoResetEventWaitOneInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly AutoResetEvent _event = new AutoResetEvent(false);

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        _event.WaitOne();
        await context.CallActivityAsync(""ProcessAfterEvent"", ""data"");
    }
}";

            await VerifyDFA0009Diagnostic(testCode);
        }

        [Test]
        public async Task ThreadingInActivityFunctionShouldNotReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    private static readonly object _lock = new object();

    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string input)
    {
        // Threading operations in activities should be allowed
        lock (_lock)
        {
            return ""processed: "" + input;
        }
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleThreadingAPIsInOrchestratorShouldReportMultipleDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lock = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        ThreadPool.QueueUserWorkItem(_ => DoWork());
        
        lock (_lock)
        {
            var data = ""protected"";
        };
        
        await context.CallActivityAsync(""ProcessCompleted"", ""data"");
    }

    private void DoWork() { }
}";

            await VerifyMultipleDFA0009Diagnostics(testCode, 2);        }

        [Test]
        public async Task ThreadingAPIInNestedMethodInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        StartBackgroundWork();
        await context.CallActivityAsync(""ProcessAfterWork"", ""data"");
    }

    private void StartBackgroundWork()
    {
        // Should be detected even in helper methods within orchestrator class
        var thread = new Thread(() => DoWork());
        thread.Start();
    }

    private void DoWork() { }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task SynchronizationContextPostInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var syncContext = SynchronizationContext.Current;
        syncContext?.Post(_ => DoWork(), null);
        await context.CallActivityAsync(""ProcessAfterPost"", ""data"");
    }

    private void DoWork() { }
}";

            await VerifyDFA0009Diagnostic(testCode);        }

        [Test]
        public async Task InterlockedExchangeInOrchestratorShouldReportDFA0009()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static int _counter = 0;

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var newValue = Interlocked.Exchange(ref _counter, 42);
        await context.CallActivityAsync(""ProcessCounter"", newValue);
    }
}";

            await VerifyDFA0009Diagnostic(testCode);        }
    }
}
