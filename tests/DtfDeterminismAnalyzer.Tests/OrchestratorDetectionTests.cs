using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for orchestrator detection integration.
    /// These tests validate that the analyzer correctly identifies orchestrator functions and applies rules appropriately.
    /// </summary>
    [TestFixture]
    public class OrchestratorDetectionTests : AnalyzerTestBase<Analyzers.Dfa0001TimeApiAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        // Helper methods for test verification
        private async Task VerifyDFA0001Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(1), "Expected exactly one diagnostic");
            Assert.That(result.AnalyzerDiagnostics[0].Id, Is.EqualTo("DFA0001"), "Expected DFA0001 diagnostic");
            Assert.That(result.AnalyzerDiagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Non-deterministic time API used."), "Expected correct diagnostic message");
        }

        private async Task VerifyNoDiagnostics(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(0), "Expected no diagnostics");
        }

        private async Task VerifyMultipleDFA0001Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.That(result.AnalyzerDiagnostics.Count, Is.EqualTo(expectedCount), $"Expected exactly {expectedCount} diagnostics");
            foreach (var diagnostic in result.AnalyzerDiagnostics)
            {
                Assert.That(diagnostic.Id, Is.EqualTo("DFA0001"), "Expected DFA0001 diagnostic");
                Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("Non-deterministic time API used."), "Expected correct diagnostic message");
            }
        }

        [Test]
        public async Task OrchestrationTriggerAttributeShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // This should be detected as an orchestrator function
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            // This test validates orchestrator detection - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task ActivityTriggerAttributeShouldNotDetectAsOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string input)
    {
        // This should NOT be detected as an orchestrator function
        // Non-deterministic operations should be allowed here
        await Task.Delay(1000);
        return ""processed: "" + input;
    }
}";

            // No diagnostics expected for activity functions
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task HttpTriggerFunctionShouldNotDetectAsOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class TestHttpFunction
{
    [FunctionName(""TestHttpFunction"")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, ""get"", ""post"")] HttpRequest req)
    {
        // This should NOT be detected as an orchestrator function
        // Non-deterministic operations should be allowed here
        await Task.Delay(1000);
        return new OkResult();
    }
}";

            // No diagnostics expected for HTTP trigger functions
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task OrchestrationTriggerWithDurableOrchestrationContextBaseShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] DurableOrchestrationContextBase context)
    {
        // This should be detected as an orchestrator function using base class
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            // This test validates orchestrator detection with base class - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleParametersWithOrchestrationTriggerShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
using Microsoft.Extensions.Logging;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        // This should be detected as an orchestrator function despite multiple parameters
        log.LogInformation(""Starting orchestration"");
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            // This test validates orchestrator detection with multiple parameters - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task StaticMethodWithOrchestrationTriggerShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public static class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public static async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // This should be detected as an orchestrator function even if static
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            // This test validates orchestrator detection for static methods - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task OrchestrationTriggerWithReturnValueShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // This should be detected as an orchestrator function with return value
        var result = await context.CallActivityAsync<string>(""TestActivity"", ""data"");
        return result;
    }
}";

            // This test validates orchestrator detection with return value - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task FunctionNameWithoutOrchestrationTriggerShouldNotDetectAsOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class TestFunction
{
    [FunctionName(""NotAnOrchestrator"")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequest req)
    {
        // This should NOT be detected as an orchestrator despite having FunctionName attribute
        await Task.Delay(1000);
        return new OkResult();
    }
}";

            // No diagnostics expected for non-orchestrator functions
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task OrchestrationTriggerParameterWithoutFunctionNameShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    // Even without FunctionName, OrchestrationTrigger parameter should trigger detection
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // This should be detected as an orchestrator function
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            // This test validates orchestrator detection without FunctionName attribute - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task NestedClassWithOrchestrationTriggerShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class OuterClass
{
    public class TestOrchestrator
    {
        [FunctionName(""NestedOrchestrator"")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // This should be detected as an orchestrator function even in nested class
            await context.CallActivityAsync(""TestActivity"", ""data"");
        }
    }
}";

            // This test validates orchestrator detection in nested classes - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleOrchestratorsInSameClassShouldDetectAll()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrators
{
    [FunctionName(""Orchestrator1"")]
    public async Task RunOrchestrator1([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // First orchestrator
        await context.CallActivityAsync(""TestActivity1"", ""data"");
    }

    [FunctionName(""Orchestrator2"")]
    public async Task RunOrchestrator2([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Second orchestrator
        await context.CallActivityAsync(""TestActivity2"", ""data"");
    }

    [FunctionName(""NotAnOrchestrator"")]
    public async Task<string> RunActivity([ActivityTrigger] string input)
    {
        // This is an activity, not an orchestrator
        return ""processed: "" + input;
    }
}";

            // This test validates detection of multiple orchestrators in same class - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task OrchestrationTriggerWithCustomContextInterfaceShouldDetectOrchestrator()
        {
            string testCode = OrchestrationTriggerUsing + @"
public interface ICustomOrchestrationContext : IDurableOrchestrationContext
{
    // Custom orchestration context interface
}

public class TestOrchestrator
{
    [FunctionName(""CustomContextOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] ICustomOrchestrationContext context)
    {
        // This should be detected as an orchestrator function with custom context interface
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            // This test validates orchestrator detection with custom context interface - no diagnostics expected
            await VerifyNoDiagnostics(testCode);
        }
    }
}
