using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0003: Random usage detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic Random usage and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0003RandomTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0003RandomAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        /// <summary>
        /// Helper method to run analyzer test and verify DFA0003 diagnostic is reported.
        /// </summary>
        private async Task VerifyDFA0003Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0003").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0003 diagnostic");
            
            var diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Non-deterministic random used in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
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
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0003").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0003 diagnostics");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify multiple DFA0003 diagnostics.
        /// </summary>
        private async Task VerifyMultipleDFA0003Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0003").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0003 diagnostics");

            foreach (var diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Non-deterministic random used in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task RunAnalyzer_WithRandomConstructorNoSeedInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = new Random();
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomSharedFieldInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Random _random = new Random();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = _random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomNextMethodInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = new Random();
        var randomValue = random.Next();
        await context.CallActivityAsync(""ProcessRandom"", randomValue);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomNextDoubleMethodInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = new Random();
        var randomValue = random.NextDouble();
        await context.CallActivityAsync(""ProcessRandom"", randomValue);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomNextBytesMethodInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = new Random();
        var bytes = new byte[10];
        random.NextBytes(bytes);
        await context.CallActivityAsync(""ProcessBytes"", bytes);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomWithFixedSeedInOrchestrator_DoesNotReportDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Fixed seed makes Random deterministic - should be allowed
        var random = new Random(12345);
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithRandomWithContextBasedSeedInOrchestrator_DoesNotReportDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Deterministic seed based on context - should be allowed
        var seed = context.CurrentUtcDateTime.GetHashCode();
        var random = new Random(seed);
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithRandomInActivityFunction_DoesNotReportDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<int> RunActivity([ActivityTrigger] string input)
    {
        // Random usage in activities should be allowed
        var random = new Random();
        return random.Next(1, 100);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithRandomInRegularClass_DoesNotReportDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class RandomService
{
    private readonly Random _random = new Random();

    public int GetRandomValue()
    {
        // Random usage outside orchestrators should be allowed
        return _random.Next(1, 100);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithMultipleRandomInOrchestrator_ReportsMultipleDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random1 = new Random();
        var random2 = new Random();
        
        var value1 = random1.Next();
        var value2 = random2.NextDouble();
        
        await context.CallActivityAsync(""ProcessValues"", new { value1, value2 });
    }
}";

            await VerifyMultipleDFA0003Diagnostics(testCode, 2);
        }

        [Test]
        public async Task RunAnalyzer_WithRandomInNestedMethodInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = GenerateRandomValue();
        await context.CallActivityAsync(""SomeActivity"", value);
    }

    private int GenerateRandomValue()
    {
        var random = new Random(); // Should be detected even in helper methods within orchestrator class
        return random.Next(1, 100);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomSharedSeededInOrchestrator_DoesNotReportDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Random _random = new Random(42); // Fixed seed should be allowed

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = _random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithRandomInConditionalBranchInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var input = context.GetInput<bool>();
        var value = input ? new Random().Next() : 42;
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithRandomWithVariableSeedInOrchestrator_ReportsDFA0003()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Non-deterministic seed based on current time - should be flagged
        var seed = DateTime.Now.Millisecond;
        var random = new Random(seed);
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyDFA0003Diagnostic(testCode);        }
    }
}
