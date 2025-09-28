using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0004: I/O operation detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic I/O operations and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0004IoTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0004IoAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        /// <summary>
        /// Helper method to run analyzer test and verify DFA0004 diagnostic is reported.
        /// </summary>
        private async Task VerifyDFA0004Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0004").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0004 diagnostic");
            
            var diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Outbound I/O detected in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
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
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0004").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0004 diagnostics");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify multiple DFA0004 diagnostics.
        /// </summary>
        private async Task VerifyMultipleDFA0004Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0004").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0004 diagnostics");

            foreach (var diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Outbound I/O detected in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task FileReadAllTextInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var content = File.ReadAllText(""config.txt"");
        await context.CallActivityAsync(""ProcessContent"", content);
    }
}";

            await VerifyDFA0004Diagnostic(testCode);        }

        [Test]
        public async Task FileWriteAllTextInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var data = context.GetInput<string>();
        File.WriteAllText(""output.txt"", data);
        await context.CallActivityAsync(""NotifyCompletion"", ""done"");
    }
}";

            await VerifyDFA0004Diagnostic(testCode);        }

        [Test]
        public async Task HttpClientGetAsyncInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var response = await _httpClient.GetAsync(""https://api.example.com/data"");
        var content = await response.Content.ReadAsStringAsync();
        await context.CallActivityAsync(""ProcessData"", content);
    }
}";

            await VerifyDFA0004Diagnostic(testCode);        }

        [Test]
        public async Task FileStreamConstructorInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        using var stream = new FileStream(""data.bin"", FileMode.Open);
        var buffer = new byte[1024];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        await context.CallActivityAsync(""ProcessBytes"", buffer);
    }
}";

            await VerifyDFA0004Diagnostic(testCode);        }

        [Test]
        public async Task DirectoryGetFilesInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var files = Directory.GetFiles(""/data"");
        await context.CallActivityAsync(""ProcessFiles"", files);
    }
}";

            await VerifyDFA0004Diagnostic(testCode);        }

        [Test]
        public async Task FileIOInActivityFunctionShouldNotReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string filePath)
    {
        // File I/O in activities should be allowed
        var content = File.ReadAllText(filePath);
        return content;
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task DurableHttpCallHttpAsyncInOrchestratorShouldNotReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Durable HTTP calls should be allowed - they're replay-safe
        var request = new DurableHttpRequest(HttpMethod.Get, new Uri(""https://api.example.com/data""));
        var response = await context.CallHttpAsync(request);
        await context.CallActivityAsync(""ProcessResponse"", response.Content);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleIOOperationsInOrchestratorShouldReportMultipleDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly HttpClient _httpClient = new HttpClient();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var config = File.ReadAllText(""config.json"");
        var response = await _httpClient.GetAsync(""https://api.example.com"");
        
        await context.CallActivityAsync(""ProcessData"", new { config, response });
    }
}";

            await VerifyMultipleDFA0004Diagnostics(testCode, 2);
        }

        [Test]
        public async Task PathOperationsInOrchestratorShouldNotReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Path operations don't perform I/O - should be allowed
        var fileName = Path.GetFileName(""/data/config.json"");
        var extension = Path.GetExtension(fileName);
        await context.CallActivityAsync(""ProcessPath"", new { fileName, extension });
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task IOInNestedMethodInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var data = LoadConfiguration();
        await context.CallActivityAsync(""ProcessConfig"", data);
    }

    private string LoadConfiguration()
    {
        // Should be detected even in helper methods within orchestrator class
        return File.ReadAllText(""config.json"");
    }
}";

            await VerifyDFA0004Diagnostic(testCode);        }
    }
}
