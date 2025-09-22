using System;
using System.Threading;
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
    public class Dfa0009ThreadingApisTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task ThreadStartInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var thread = new Thread(() => DoWork());
        {|#0:thread.Start()|};
        await context.CallActivityAsync(""ProcessAfterThread"", ""data"");
    }

    private void DoWork() => Thread.Sleep(1000);
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ThreadPoolQueueUserWorkItemInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:ThreadPool.QueueUserWorkItem(_ => DoWork())|};
        await context.CallActivityAsync(""ProcessAfterWork"", ""data"");
    }

    private void DoWork() => Thread.Sleep(1000);
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ParallelForEachInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var items = new[] { ""item1"", ""item2"", ""item3"" };
        {|#0:Parallel.ForEach(items, item => ProcessItem(item))|};
        await context.CallActivityAsync(""ProcessCompleted"", ""data"");
    }

    private void ProcessItem(string item) { }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ParallelForInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:Parallel.For(0, 10, i => ProcessIndex(i))|};
        await context.CallActivityAsync(""ProcessCompleted"", ""data"");
    }

    private void ProcessIndex(int index) { }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task LockStatementInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lock = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:lock (_lock)
        {
            // Critical section
            var data = ""protected data"";
        }|};
        await context.CallActivityAsync(""ProcessData"", ""data"");
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task MonitorEnterInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lockObject = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        bool lockTaken = false;
        try
        {
            {|#0:Monitor.Enter(_lockObject, ref lockTaken)|};
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

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task MutexWaitOneInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Mutex _mutex = new Mutex();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:_mutex.WaitOne()|};
        try
        {
            await context.CallActivityAsync(""CriticalSection"", ""data"");
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ReaderWriterLockAcquireReaderLockInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly ReaderWriterLock _rwLock = new ReaderWriterLock();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:_rwLock.AcquireReaderLock(TimeSpan.FromSeconds(10))|};
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

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task CancellationTokenRegisterInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var cts = new CancellationTokenSource();
        {|#0:cts.Token.Register(() => Console.WriteLine(""Cancelled""))|};
        await context.CallActivityAsync(""ProcessWithCancellation"", ""data"");
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task AutoResetEventWaitOneInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly AutoResetEvent _event = new AutoResetEvent(false);

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:_event.WaitOne()|};
        await context.CallActivityAsync(""ProcessAfterEvent"", ""data"");
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ThreadingInActivityFunctionShouldNotReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleThreadingAPIsInOrchestratorShouldReportMultipleDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly object _lock = new object();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:ThreadPool.QueueUserWorkItem(_ => DoWork())|};
        
        {|#1:lock (_lock)
        {
            var data = ""protected"";
        }|};
        
        await context.CallActivityAsync(""ProcessCompleted"", ""data"");
    }

    private void DoWork() { }
}";

            var expected = new[]
            {
                VerifyCS.Diagnostic("DFA0009").WithLocation(0).WithMessage("Threading API usage detected."),
                VerifyCS.Diagnostic("DFA0009").WithLocation(1).WithMessage("Threading API usage detected.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task ThreadingAPIInNestedMethodInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
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
        {|#0:thread.Start()|};
    }

    private void DoWork() { }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task SynchronizationContextPostInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var syncContext = SynchronizationContext.Current;
        {|#0:syncContext?.Post(_ => DoWork(), null)|};
        await context.CallActivityAsync(""ProcessAfterPost"", ""data"");
    }

    private void DoWork() { }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task InterlockedExchangeInOrchestratorShouldReportDFA0009()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static int _counter = 0;

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var newValue = {|#0:Interlocked.Exchange(ref _counter, 42)|};
        await context.CallActivityAsync(""ProcessCounter"", newValue);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0009")
                .WithLocation(0)
                .WithMessage("Threading API usage detected.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}