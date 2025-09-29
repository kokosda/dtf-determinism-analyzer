using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0002: GUID generation detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic GUID generation and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0002GuidTests : AnalyzerTestBase<Analyzers.Dfa0002GuidAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        /// <summary>
        /// Helper method to run analyzer test and verify DFA0002 diagnostic is reported.
        /// </summary>
        private async Task VerifyDFA0002Diagnostic(string testCode)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0002").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0002 diagnostic");

            Microsoft.CodeAnalysis.Diagnostic diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Non-deterministic GUID generated in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                "Diagnostic message should match expected message");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify no diagnostics are reported.
        /// </summary>
        private async Task VerifyNoDiagnostics(string testCode)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify no analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0002").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0002 diagnostics");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify multiple DFA0002 diagnostics.
        /// </summary>
        private async Task VerifyMultipleDFA0002Diagnostics(string testCode, int expectedCount)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0002").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0002 diagnostics");

            foreach (Microsoft.CodeAnalysis.Diagnostic? diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Non-deterministic GUID generated in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInOrchestrator_ReportsDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var newGuid = Guid.NewGuid();
        await context.CallActivityAsync(""SomeActivity"", newGuid);
    }
}";

            await VerifyDFA0002Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidAssignedToVariableInOrchestrator_ReportsDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Guid correlationId = Guid.NewGuid();
        var input = new { Id = correlationId, Data = ""test"" };
        await context.CallActivityAsync(""ProcessData"", input);
    }
}";

            await VerifyDFA0002Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInMethodCallInOrchestrator_ReportsDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await context.CallActivityAsync(""SomeActivity"", Guid.NewGuid());
    }
}";

            await VerifyDFA0002Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInPropertyInitializerInOrchestrator_ReportsDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var request = new { RequestId = Guid.NewGuid(), Data = ""test"" };
        await context.CallActivityAsync(""ProcessRequest"", request);
    }
}";

            await VerifyDFA0002Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInActivityFunction_DoesNotReportDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<Guid> RunActivity([ActivityTrigger] string input)
    {
        var newGuid = Guid.NewGuid(); // This should be allowed in activities
        return newGuid;
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInRegularClass_DoesNotReportDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class RegularService
{
    public Guid GenerateId()
    {
        return Guid.NewGuid(); // This should be allowed outside orchestrators
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithContextNewGuidInOrchestrator_DoesNotReportDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var deterministicGuid = context.NewGuid(); // This should be allowed - it's deterministic
        await context.CallActivityAsync(""SomeActivity"", deterministicGuid);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithMultipleGuidNewGuidInOrchestrator_ReportsMultipleDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var request = new { Id1 = guid1, Id2 = guid2, Id3 = Guid.NewGuid() };
        
        await context.CallActivityAsync(""ProcessMultipleIds"", request);
    }
}";

            await VerifyMultipleDFA0002Diagnostics(testCode, 3);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInNestedMethodInOrchestrator_ReportsDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = GenerateCorrelationId();
        await context.CallActivityAsync(""SomeActivity"", id);
    }

    private Guid GenerateCorrelationId()
    {
        return Guid.NewGuid(); // Should be detected even in helper methods within orchestrator class
    }
}";

            await VerifyDFA0002Diagnostic(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidConstructorWithByteArrayInOrchestrator_DoesNotReportDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Creating GUID from deterministic byte array should be allowed
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var deterministicGuid = new Guid(bytes);
        await context.CallActivityAsync(""SomeActivity"", deterministicGuid);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidParseInOrchestrator_DoesNotReportDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Parsing deterministic GUID string should be allowed
        var parsedGuid = Guid.Parse(""12345678-1234-1234-1234-123456789012"");
        await context.CallActivityAsync(""SomeActivity"", parsedGuid);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidEmptyInOrchestrator_DoesNotReportDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Using Guid.Empty should be allowed - it's deterministic
        var emptyGuid = Guid.Empty;
        await context.CallActivityAsync(""SomeActivity"", emptyGuid);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithGuidNewGuidInConditionalExpressionInOrchestrator_ReportsDFA0002()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var input = context.GetInput<string>();
        var id = string.IsNullOrEmpty(input) ? Guid.NewGuid() : Guid.Parse(input);
        await context.CallActivityAsync(""SomeActivity"", id);
    }
}";

            await VerifyDFA0002Diagnostic(testCode);
        }
    }
}
