using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0005: Environment variable detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic environment variable access and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0005EnvironmentTests : AnalyzerTestBase<Analyzers.Dfa0005EnvironmentAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        /// <summary>
        /// Helper method to run analyzer test and verify DFA0005 diagnostic is reported.
        /// </summary>
        private async Task VerifyDFA0005Diagnostic(string testCode)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0005").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0005 diagnostic");

            Microsoft.CodeAnalysis.Diagnostic diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Environment variable read is non-deterministic", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
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
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0005").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0005 diagnostics");
        }

        /// <summary>
        /// Helper method to run analyzer test and verify multiple DFA0005 diagnostics.
        /// </summary>
        private async Task VerifyMultipleDFA0005Diagnostics(string testCode, int expectedCount)
        {
            AnalyzerTestResult result = await RunAnalyzerTest(testCode);
            
            // Verify compilation succeeded
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            
            // Verify analyzer diagnostics
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0005").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0005 diagnostics");

            foreach (Microsoft.CodeAnalysis.Diagnostic? diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Environment variable read is non-deterministic", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentGetEnvironmentVariableInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var connectionString = Environment.GetEnvironmentVariable(""DATABASE_CONNECTION"");
        await context.CallActivityAsync(""ConnectToDatabase"", connectionString);
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentGetEnvironmentVariablesInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var allVars = Environment.GetEnvironmentVariables();
        await context.CallActivityAsync(""ProcessEnvironment"", allVars);
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentSetEnvironmentVariableInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = context.GetInput<string>();
        Environment.SetEnvironmentVariable(""TEMP_VALUE"", value);
        await context.CallActivityAsync(""ProcessAfterSet"", ""done"");
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentUserNameInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var userName = Environment.UserName;
        await context.CallActivityAsync(""LogUser"", userName);
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentMachineNameInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var machine = Environment.MachineName;
        await context.CallActivityAsync(""LogMachine"", machine);
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentGetEnvironmentVariableInActivityFunction_DoesNotReportDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string variableName)
    {
        // Environment variable access in activities should be allowed
        var value = Environment.GetEnvironmentVariable(variableName);
        return value ?? ""default"";
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentGetEnvironmentVariableInRegularClass_DoesNotReportDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class ConfigurationService
{
    public string GetConnectionString()
    {
        // Environment variable access outside orchestrators should be allowed
        return Environment.GetEnvironmentVariable(""CONNECTION_STRING"") ?? ""default"";
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task RunAnalyzer_WithMultipleEnvironmentAccessInOrchestrator_ReportsMultipleDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var dbConnection = Environment.GetEnvironmentVariable(""DB_CONNECTION"");
        var apiKey = Environment.GetEnvironmentVariable(""API_KEY"");
        var userName = Environment.UserName;
        
        await context.CallActivityAsync(""ProcessConfig"", new { dbConnection, apiKey, userName });
    }
}";

            await VerifyMultipleDFA0005Diagnostics(testCode, 3);
        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentAccessInNestedMethodInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var config = GetConfiguration();
        await context.CallActivityAsync(""ProcessConfig"", config);
    }

    private string GetConfiguration()
    {
        // Should be detected even in helper methods within orchestrator class
        return Environment.GetEnvironmentVariable(""APP_CONFIG"") ?? ""default"";
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentExpandEnvironmentVariablesInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var expanded = Environment.ExpandEnvironmentVariables(""%TEMP%\\myapp"");
        await context.CallActivityAsync(""ProcessPath"", expanded);
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }

        [Test]
        public async Task RunAnalyzer_WithEnvironmentCurrentDirectoryInOrchestrator_ReportsDFA0005()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var currentDir = Environment.CurrentDirectory;
        await context.CallActivityAsync(""ProcessDirectory"", currentDir);
    }
}";

            await VerifyDFA0005Diagnostic(testCode);        }
    }
}
