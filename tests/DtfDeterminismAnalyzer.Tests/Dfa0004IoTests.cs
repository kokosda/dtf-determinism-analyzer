using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0004IoAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0004: I/O operation detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic I/O operations and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0004IoTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task FileReadAllTextInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var content = {|#0:File.ReadAllText(""config.txt"")|};
        await context.CallActivityAsync(""ProcessContent"", content);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0004")
                .WithLocation(0)
                .WithMessage("Outbound I/O detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:File.WriteAllText(""output.txt"", data)|};
        await context.CallActivityAsync(""NotifyCompletion"", ""done"");
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0004")
                .WithLocation(0)
                .WithMessage("Outbound I/O detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        var response = await {|#0:_httpClient.GetAsync(""https://api.example.com/data"")|};
        var content = await response.Content.ReadAsStringAsync();
        await context.CallActivityAsync(""ProcessData"", content);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0004")
                .WithLocation(0)
                .WithMessage("Outbound I/O detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task FileStreamConstructorInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        using var stream = {|#0:new FileStream(""data.bin"", FileMode.Open)|};
        var buffer = new byte[1024];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        await context.CallActivityAsync(""ProcessBytes"", buffer);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0004")
                .WithLocation(0)
                .WithMessage("Outbound I/O detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task DirectoryGetFilesInOrchestratorShouldReportDFA0004()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var files = {|#0:Directory.GetFiles(""/data"")|};
        await context.CallActivityAsync(""ProcessFiles"", files);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0004")
                .WithLocation(0)
                .WithMessage("Outbound I/O detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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
        var config = {|#0:File.ReadAllText(""config.json"")|};
        var response = await {|#1:_httpClient.GetAsync(""https://api.example.com"")|};
        
        await context.CallActivityAsync(""ProcessData"", new { config, response });
    }
}";

            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic("DFA0004").WithLocation(0).WithMessage("Outbound I/O detected in orchestrator."),
                VerifyCS.Diagnostic("DFA0004").WithLocation(1).WithMessage("Outbound I/O detected in orchestrator.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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
        return {|#0:File.ReadAllText(""config.json"")|};
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0004")
                .WithLocation(0)
                .WithMessage("Outbound I/O detected in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
