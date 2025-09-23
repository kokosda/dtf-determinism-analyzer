using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0001: DateTime API detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic time APIs and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0001TimeApiTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task DateTimeNowInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var now = {|#0:DateTime.Now|};
        await context.CallActivityAsync(""SomeActivity"", now);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DateTimeUtcNowInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var utcNow = {|#0:DateTime.UtcNow|};
        await context.CallActivityAsync(""SomeActivity"", utcNow);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DateTimeTodayInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var today = {|#0:DateTime.Today|};
        await context.CallActivityAsync(""SomeActivity"", today);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DateTimeOffsetNowInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var offsetNow = {|#0:DateTimeOffset.Now|};
        await context.CallActivityAsync(""SomeActivity"", offsetNow);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DateTimeOffsetUtcNowInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var offsetUtcNow = {|#0:DateTimeOffset.UtcNow|};
        await context.CallActivityAsync(""SomeActivity"", offsetUtcNow);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task StopwatchStartNewInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
using System.Diagnostics;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var stopwatch = {|#0:Stopwatch.StartNew()|};
        await context.CallActivityAsync(""SomeActivity"", stopwatch.ElapsedMilliseconds);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task StopwatchConstructorInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
using System.Diagnostics;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var stopwatch = {|#0:new Stopwatch()|};
        stopwatch.Start();
        await context.CallActivityAsync(""SomeActivity"", stopwatch.ElapsedMilliseconds);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DateTimeNowInActivityFunctionShouldNotReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<DateTime> RunActivity([ActivityTrigger] string input)
    {
        var now = DateTime.Now; // This should be allowed in activities
        return now;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task DateTimeUtcNowInRegularClassShouldNotReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class RegularClass
{
    public DateTime GetCurrentTime()
    {
        return DateTime.UtcNow; // This should be allowed outside orchestrators
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task ContextCurrentUtcDateTimeInOrchestratorShouldNotReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var currentTime = context.CurrentUtcDateTime; // This should be allowed - it's deterministic
        await context.CallActivityAsync(""SomeActivity"", currentTime);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleDateTimeViolationsInOrchestratorShouldReportMultipleDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var now = {|#0:DateTime.Now|};
        var utcNow = {|#1:DateTime.UtcNow|};
        var today = {|#2:DateTime.Today|};
        
        await context.CallActivityAsync(""SomeActivity"", new { now, utcNow, today });
    }
}";

            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic("DFA0001").WithLocation(0).WithMessage("Non-deterministic time API used in orchestrator."),
                VerifyCS.Diagnostic("DFA0001").WithLocation(1).WithMessage("Non-deterministic time API used in orchestrator."),
                VerifyCS.Diagnostic("DFA0001").WithLocation(2).WithMessage("Non-deterministic time API used in orchestrator.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DateTimeNowInNestedMethodInOrchestratorShouldReportDFA0001()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var time = GetCurrentTime();
        await context.CallActivityAsync(""SomeActivity"", time);
    }

    private DateTime GetCurrentTime()
    {
        return {|#0:DateTime.Now|}; // Should be detected even in helper methods within orchestrator class
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0001")
                .WithLocation(0)
                .WithMessage("Non-deterministic time API used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
