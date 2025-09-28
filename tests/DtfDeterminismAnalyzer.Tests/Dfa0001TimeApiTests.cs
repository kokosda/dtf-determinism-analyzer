using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0001: DateTime API usage detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic time APIs and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0001TimeApiTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>
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
        var now = DateTime.Now;
        await context.CallActivityAsync(""SomeActivity"", now);
    }
}";

            // Use AnalyzerTestBase methods to run the test
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0001").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0001 diagnostic");
            
            var diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Non-deterministic time API used in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                "Diagnostic message should match expected message");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify expected DFA0001 diagnostic.
        /// </summary>
        private async Task VerifyDFA0001Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0001").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0001 diagnostic");
            
            var diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Non-deterministic time API used in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                "Diagnostic message should match expected message");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify no diagnostics are reported.
        /// </summary>
        private async Task VerifyNoDiagnostics(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify no analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0001").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0001 diagnostics");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify multiple DFA0001 diagnostics.
        /// </summary>
        private async Task VerifyMultipleDFA0001Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0001").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0001 diagnostics");
            
            // Verify all diagnostics have the expected message
            foreach (var diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Non-deterministic time API used in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
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
        var utcNow = DateTime.UtcNow;
        await context.CallActivityAsync(""SomeActivity"", utcNow);
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
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
        var today = DateTime.Today;
        await context.CallActivityAsync(""SomeActivity"", today);
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
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
        var offsetNow = DateTimeOffset.Now;
        await context.CallActivityAsync(""SomeActivity"", offsetNow);
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
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
        var offsetUtcNow = DateTimeOffset.UtcNow;
        await context.CallActivityAsync(""SomeActivity"", offsetUtcNow);
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
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
        var stopwatch = Stopwatch.StartNew();
        await context.CallActivityAsync(""SomeActivity"", stopwatch.ElapsedMilliseconds);
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
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
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await context.CallActivityAsync(""SomeActivity"", stopwatch.ElapsedMilliseconds);
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
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

            await VerifyNoDiagnostics(testCode);
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

            await VerifyNoDiagnostics(testCode);
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

            await VerifyNoDiagnostics(testCode);
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
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var today = DateTime.Today;
        
        await context.CallActivityAsync(""SomeActivity"", new { now, utcNow, today });
    }
}";

            await VerifyMultipleDFA0001Diagnostics(testCode, 3);
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
        return DateTime.Now; // Should be detected even in helper methods within orchestrator class
    }
}";

            await VerifyDFA0001Diagnostic(testCode);
        }
    }
}
