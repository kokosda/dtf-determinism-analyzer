using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Integration tests for Azure Functions type resolution in the test framework.
    /// These tests validate that all Azure Functions types are properly resolved during test execution.
    /// CRITICAL: These tests MUST FAIL before implementation to follow TDD approach.
    /// </summary>
    [TestFixture]
    public class AzureFunctionsTypeResolutionTests
    {
        [Test]
        public async Task TypeResolution_Should_Resolve_All_WebJobs_Core_Types()
        {
            // Integration contract: All Microsoft.Azure.WebJobs types must be resolvable
            string testCode = @"
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

public class WebJobsTypesTest
{
    [FunctionName(""TestFunction"")]
    public async Task TestMethod()
    {
        await Task.CompletedTask;
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var compilation = await testBase.CreateTestCompilation(testCode);
            
            // Verify FunctionName attribute is resolved
            var functionNameSymbol = compilation.GetTypeByMetadataName("Microsoft.Azure.WebJobs.FunctionNameAttribute");
            Assert.IsNotNull(functionNameSymbol, 
                "FunctionNameAttribute must be resolved from Microsoft.Azure.WebJobs assembly");
            
            var webJobsErrors = compilation.GetDiagnostics()
                .Where(d => d.Id == "CS0246" && d.GetMessage().Contains("FunctionName"))
                .ToList();
            
            Assert.IsEmpty(webJobsErrors, 
                "Microsoft.Azure.WebJobs types must be resolved without CS0246 errors");
        }

        [Test]
        public async Task TypeResolution_Should_Resolve_All_DurableTask_Types()
        {
            // Integration contract: All DurableTask extension types must be resolvable
            string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public class DurableTaskTypesTest
{
    [FunctionName(""Orchestrator"")]
    public async Task OrchestratorMethod([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await context.CallActivityAsync(""Activity"", ""data"");
    }

    [FunctionName(""Activity"")]
    public string ActivityMethod([ActivityTrigger] string input)
    {
        return input;
    }

    [FunctionName(""Client"")]
    public async Task ClientMethod([DurableClient] IDurableOrchestrationClient client)
    {
        await client.StartNewAsync(""Orchestrator"", null);
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var compilation = await testBase.CreateTestCompilation(testCode);
            
            // Verify key DurableTask types are resolved
            var orchestrationContextSymbol = compilation.GetTypeByMetadataName("Microsoft.Azure.WebJobs.Extensions.DurableTask.IDurableOrchestrationContext");
            Assert.IsNotNull(orchestrationContextSymbol, 
                "IDurableOrchestrationContext must be resolved from DurableTask extensions");
            
            var durableTaskErrors = compilation.GetDiagnostics()
                .Where(d => d.Id == "CS0246" && 
                           (d.GetMessage().Contains("OrchestrationTrigger") ||
                            d.GetMessage().Contains("ActivityTrigger") ||
                            d.GetMessage().Contains("DurableClient") ||
                            d.GetMessage().Contains("IDurableOrchestrationContext")))
                .ToList();
            
            Assert.IsEmpty(durableTaskErrors, 
                $"DurableTask extension types must be resolved without errors. Found: {string.Join(", ", durableTaskErrors.Select(d => d.GetMessage()))}");
        }

        [Test]
        public async Task TypeResolution_Should_Resolve_All_AspNetCore_Types()
        {
            // Integration contract: All ASP.NET Core types used in Azure Functions must be resolvable
            string testCode = @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

public class AspNetCoreTypesTest
{
    [FunctionName(""HttpFunction"")]
    public async Task<IActionResult> HttpMethod(
        [HttpTrigger(AuthorizationLevel.Function, ""get"", ""post"")] HttpRequest req)
    {
        return new OkObjectResult(""Success"");
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var compilation = await testBase.CreateTestCompilation(testCode);
            
            // Verify ASP.NET Core types are resolved
            var actionResultSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.IActionResult");
            Assert.IsNotNull(actionResultSymbol, 
                "IActionResult must be resolved from Microsoft.AspNetCore.Mvc assembly");
            
            var httpRequestSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpRequest");
            Assert.IsNotNull(httpRequestSymbol, 
                "HttpRequest must be resolved from Microsoft.AspNetCore.Http assembly");
            
            var aspNetCoreErrors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error && 
                           (d.GetMessage().Contains("IActionResult") ||
                            d.GetMessage().Contains("HttpRequest") ||
                            d.GetMessage().Contains("HttpTrigger") ||
                            d.GetMessage().Contains("OkObjectResult")))
                .ToList();
            
            Assert.IsEmpty(aspNetCoreErrors, 
                $"ASP.NET Core types must be resolved without errors. Found: {string.Join(", ", aspNetCoreErrors.Select(d => d.GetMessage()))}");
        }

        [Test]
        public async Task TypeResolution_Should_Resolve_Extensions_Logging_Types()
        {
            // Integration contract: Microsoft.Extensions.Logging types must be resolvable
            string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class LoggingTypesTest
{
    [FunctionName(""LoggingFunction"")]
    public async Task LoggingMethod(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        logger.LogInformation(""Test log message"");
        logger.LogWarning(""Test warning"");
        logger.LogError(""Test error"");
        await Task.CompletedTask;
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var compilation = await testBase.CreateTestCompilation(testCode);
            
            // Verify ILogger is resolved
            var loggerSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
            Assert.IsNotNull(loggerSymbol, 
                "ILogger must be resolved from Microsoft.Extensions.Logging assembly");
            
            var loggingErrors = compilation.GetDiagnostics()
                .Where(d => d.Id == "CS0246" && d.GetMessage().Contains("ILogger"))
                .ToList();
            
            Assert.IsEmpty(loggingErrors, 
                "Microsoft.Extensions.Logging types must be resolved without CS0246 errors");
        }

        [Test]
        public void TypeResolution_Should_Have_Consistent_Assembly_Loading()
        {
            // Integration contract: Assembly loading must be consistent across test runs
            var testBase1 = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var testBase2 = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0002GuidAnalyzer>();
            
            var references1 = testBase1.GetReferenceAssemblies().ToList();
            var references2 = testBase2.GetReferenceAssemblies().ToList();
            
            Assert.AreEqual(references1.Count, references2.Count,
                "Assembly reference count must be consistent across different analyzer test instances");
            
            var webJobsRef1 = references1.FirstOrDefault(r => r.Display?.Contains("Microsoft.Azure.WebJobs") == true);
            var webJobsRef2 = references2.FirstOrDefault(r => r.Display?.Contains("Microsoft.Azure.WebJobs") == true);
            
            Assert.IsNotNull(webJobsRef1, "First test base must include WebJobs assembly");
            Assert.IsNotNull(webJobsRef2, "Second test base must include WebJobs assembly");
            Assert.AreEqual(webJobsRef1.Display, webJobsRef2.Display, 
                "WebJobs assembly reference must be identical across test base instances");
        }

        [Test]
        public async Task TypeResolution_Should_Support_Complex_Inheritance_Scenarios()
        {
            // Integration contract: Complex type inheritance scenarios must be supported
            string testCode = @"
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

public interface ICustomOrchestrationContext : IDurableOrchestrationContext
{
    Task<string> CustomMethod(string input);
}

public class CustomOrchestrationContext : DurableOrchestrationContextBase, ICustomOrchestrationContext
{
    public async Task<string> CustomMethod(string input)
    {
        return await CallActivityAsync<string>(""ProcessCustom"", input);
    }
}";

            var testBase = new AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>();
            var compilation = await testBase.CreateTestCompilation(testCode);
            
            var inheritanceErrors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error && 
                           (d.GetMessage().Contains("IDurableOrchestrationContext") ||
                            d.GetMessage().Contains("DurableOrchestrationContextBase")))
                .ToList();
            
            Assert.IsEmpty(inheritanceErrors, 
                $"Complex inheritance scenarios with DurableTask types must be supported. Errors: {string.Join(", ", inheritanceErrors.Select(d => d.GetMessage()))}");
        }
    }
}