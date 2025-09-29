using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using DtfDeterminismAnalyzer.Analyzers;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for TestCodeCompilation entity.
    /// These tests validate that test code compiles successfully with Azure Functions types resolved.
    /// CRITICAL: These tests MUST FAIL before implementation to follow TDD approach.
    /// </summary>
    [TestFixture]
    public class TestCompilationTests
    {
        [Test]
        public async Task CreateTestCompilation_WithFunctionNameAttribute_ResolvesSuccessfully()
        {
            // Contract: Test compilation must resolve FunctionName attribute from Microsoft.Azure.WebJobs
            string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public class TestClass
{
    [FunctionName(""TestFunction"")]
    public async Task TestMethod([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await Task.CompletedTask;
    }
}";

            var testBase = new AnalyzerTestBase<Dfa0001TimeApiAnalyzer>();
            Compilation compilation = await testBase.CreateTestCompilation(testCode);
            
            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
            
            Assert.IsEmpty(diagnostics, 
                $"Test code must compile without errors. Found errors: {string.Join(", ", diagnostics.Select(d => d.Id))}");
        }

        [Test]
        public async Task CreateTestCompilation_WithOrchestrationTriggerAttribute_ResolvesSuccessfully()
        {
            // Contract: Test compilation must resolve OrchestrationTrigger from DurableTask extensions
            string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public class TestClass
{
    [FunctionName(""TestOrchestrator"")]
    public async Task TestOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await context.CallActivityAsync(""TestActivity"", ""data"");
    }
}";

            var testBase = new AnalyzerTestBase<Dfa0001TimeApiAnalyzer>();
            Compilation compilation = await testBase.CreateTestCompilation(testCode);
            
            var orchestrationTriggerErrors = compilation.GetDiagnostics()
                .Where(d => d.Id == "CS0246" && d.GetMessage(CultureInfo.InvariantCulture).Contains("OrchestrationTrigger"))
                .ToList();
            
            Assert.IsEmpty(orchestrationTriggerErrors, 
                "OrchestrationTrigger attribute must be resolved during test compilation");
        }

        [Test]
        public async Task CreateTestCompilation_WithHttpTriggerTypes_ResolvesSuccessfully()
        {
            // Contract: Test compilation must resolve HTTP trigger types from ASP.NET Core
            string testCode = @"
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

public class TestClass
{
    [FunctionName(""HttpFunction"")]
    public async Task<IActionResult> HttpFunction(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
    {
        return new OkResult();
    }
}";

            var testBase = new AnalyzerTestBase<Dfa0001TimeApiAnalyzer>();
            Compilation compilation = await testBase.CreateTestCompilation(testCode);
            
            var httpErrors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error && 
                           (d.GetMessage(CultureInfo.InvariantCulture).Contains("IActionResult") || 
                            d.GetMessage(CultureInfo.InvariantCulture).Contains("HttpTrigger") ||
                            d.GetMessage(CultureInfo.InvariantCulture).Contains("HttpRequest")))
                .ToList();
            
            Assert.IsEmpty(httpErrors, 
                $"HTTP trigger types must be resolved during test compilation. Found errors: {string.Join(", ", httpErrors.Select(d => d.GetMessage(CultureInfo.InvariantCulture)))}");
        }

        [Test]
        public async Task CreateTestCompilation_WithILoggerInterface_ResolvesSuccessfully()
        {
            // Contract: Test compilation must resolve ILogger from Microsoft.Extensions.Logging
            string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class TestClass
{
    [FunctionName(""LoggingFunction"")]
    public async Task LoggingFunction(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        logger.LogInformation(""Test message"");
        await Task.CompletedTask;
    }
}";

            var testBase = new AnalyzerTestBase<Dfa0001TimeApiAnalyzer>();
            Compilation compilation = await testBase.CreateTestCompilation(testCode);
            
            var loggerErrors = compilation.GetDiagnostics()
                .Where(d => d.Id == "CS0246" && d.GetMessage(CultureInfo.InvariantCulture).Contains("ILogger"))
                .ToList();
            
            Assert.IsEmpty(loggerErrors, 
                "ILogger interface must be resolved during test compilation");
        }

        [Test]
        public void GetParseOptions_WithDefaultConfiguration_UsesCSharp12LanguageVersion()
        {
            // Contract: Test compilation must use C# 12.0 for compatibility
            var testBase = new AnalyzerTestBase<Dfa0001TimeApiAnalyzer>();
            CSharpParseOptions parseOptions = testBase.GetParseOptions();
            
            Assert.AreEqual(LanguageVersion.CSharp12, parseOptions.LanguageVersion,
                "Test compilation must use C# 12.0 language version");
        }
    }
}