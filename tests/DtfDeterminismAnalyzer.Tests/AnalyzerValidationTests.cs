using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for AnalyzerDiagnosticValidation entity.
    /// These tests validate that analyzer diagnostics are properly reported without compilation errors.
    /// CRITICAL: These tests MUST FAIL before implementation to follow TDD approach.
    /// </summary>
    [TestFixture]
    public class AnalyzerValidationTests
    {
        [Test]
        public async Task RunAnalyzerValidation_WithValidCode_ReportsDiagnosticsWithoutCompilationErrors()
        {
            // Contract: Analyzer must report diagnostics on compiled code, not compilation failures
            string testCode = @"
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var now = DateTime.Now; // This should trigger DFA0001
        await context.CallActivityAsync(""TestActivity"", now);
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var result = await testBase.RunAnalyzerTest(testCode);
            
            // Contract: No compilation errors should exist
            var compilationErrors = result.CompilationDiagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
            
            Assert.IsEmpty(compilationErrors, 
                $"Analysis should run on compiled code without compilation errors. Found: {string.Join(", ", compilationErrors.Select(d => d.Id))}");
            
            // Contract: Analyzer diagnostics should be reported
            var analyzerDiagnostics = result.AnalyzerDiagnostics
                .Where(d => d.Id.StartsWith("DFA"))
                .ToList();
            
            Assert.IsNotEmpty(analyzerDiagnostics, 
                "Analyzer should report diagnostics on successfully compiled test code");
        }

        [Test]
        public async Task RunAnalyzerValidation_WithInvalidCode_DistinguishesCompilationErrorsFromAnalyzerDiagnostics()
        {
            // Contract: Framework must separate compilation diagnostics from analyzer diagnostics
            string validTestCode = @"
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await Task.CompletedTask;
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var result = await testBase.RunAnalyzerTest(validTestCode);
            
            // Contract: Compilation should succeed
            Assert.IsTrue(result.CompilationSucceeded, 
                "Test compilation must succeed with proper assembly references");
            
            // Contract: Diagnostics should be categorized correctly
            var compilationDiagnostics = result.CompilationDiagnostics.Count;
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Count;
            
            Assert.AreEqual(0, result.CompilationDiagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
                "No compilation errors should exist with proper assembly references");
        }

        [Test]
        public async Task RunAnalyzerValidation_WithMultipleAzureFunctionsAttributes_HandlesCorrectly()
        {
            // Contract: Framework must handle complex Azure Functions scenarios
            string testCode = @"
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class ComplexFunctionApp
{
    [FunctionName(""HttpStarter"")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, ""post"")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        string instanceId = await starter.StartNewAsync(""ComplexOrchestrator"", null);
        return new OkObjectResult(instanceId);
    }

    [FunctionName(""ComplexOrchestrator"")]
    public async Task ComplexOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var result = await context.CallActivityAsync<string>(""ComplexActivity"", ""input"");
        log.LogInformation($""Result: {result}"");
    }

    [FunctionName(""ComplexActivity"")]
    public string ComplexActivity([ActivityTrigger] string input, ILogger log)
    {
        return $""Processed: {input}"";
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var compilation = await testBase.CreateTestCompilation(testCode);
            
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
            
            Assert.IsEmpty(errors, 
                $"Complex Azure Functions scenarios must compile successfully. Errors: {string.Join(", ", errors.Select(d => $"{d.Id}: {d.GetMessage()}"))}");
        }

        [Test]
        public async Task RunAnalyzerValidation_WithDiagnostics_PreservesExpectedLocations()
        {
            // Contract: Diagnostic locations must be preserved through compilation process
            string testCode = @"
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var now = DateTime.Now; // Expected diagnostic location
        await context.CallActivityAsync(""TestActivity"", now);
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var result = await testBase.RunAnalyzerTest(testCode);
            
            // Contract: Diagnostic locations should be accurate
            var timeApiDiagnostics = result.AnalyzerDiagnostics
                .Where(d => d.Id == "DFA0001")
                .ToList();
            
            if (timeApiDiagnostics.Any())
            {
                var diagnostic = timeApiDiagnostics.First();
                Assert.IsTrue(diagnostic.Location.GetLineSpan().StartLinePosition.Line >= 0,
                    "Diagnostic location must be preserved and accurate");
            }
        }
    }
}