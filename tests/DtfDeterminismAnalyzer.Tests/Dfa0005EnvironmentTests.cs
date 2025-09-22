using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0005EnvironmentAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0005: Environment variable detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic environment variable access and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0005EnvironmentTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task EnvironmentGetEnvironmentVariableInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var connectionString = {|#0:Environment.GetEnvironmentVariable(""DATABASE_CONNECTION"")|};
        await context.CallActivityAsync(""ConnectToDatabase"", connectionString);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentGetEnvironmentVariablesInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var allVars = {|#0:Environment.GetEnvironmentVariables()|};
        await context.CallActivityAsync(""ProcessEnvironment"", allVars);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentSetEnvironmentVariableInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = context.GetInput<string>();
        {|#0:Environment.SetEnvironmentVariable(""TEMP_VALUE"", value)|};
        await context.CallActivityAsync(""ProcessAfterSet"", ""done"");
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentUserNameInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var userName = {|#0:Environment.UserName|};
        await context.CallActivityAsync(""LogUser"", userName);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentMachineNameInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var machine = {|#0:Environment.MachineName|};
        await context.CallActivityAsync(""LogMachine"", machine);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentGetEnvironmentVariableInActivityFunctionShouldNotReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task EnvironmentGetEnvironmentVariableInRegularClassShouldNotReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class ConfigurationService
{
    public string GetConnectionString()
    {
        // Environment variable access outside orchestrators should be allowed
        return Environment.GetEnvironmentVariable(""CONNECTION_STRING"") ?? ""default"";
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleEnvironmentAccessInOrchestratorShouldReportMultipleDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var dbConnection = {|#0:Environment.GetEnvironmentVariable(""DB_CONNECTION"")|};
        var apiKey = {|#1:Environment.GetEnvironmentVariable(""API_KEY"")|};
        var userName = {|#2:Environment.UserName|};
        
        await context.CallActivityAsync(""ProcessConfig"", new { dbConnection, apiKey, userName });
    }
}";

            var expected = new[]
            {
                VerifyCS.Diagnostic("DFA0005").WithLocation(0).WithMessage("Environment variable read is non-deterministic."),
                VerifyCS.Diagnostic("DFA0005").WithLocation(1).WithMessage("Environment variable read is non-deterministic."),
                VerifyCS.Diagnostic("DFA0005").WithLocation(2).WithMessage("Environment variable read is non-deterministic.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentAccessInNestedMethodInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
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
        return {|#0:Environment.GetEnvironmentVariable(""APP_CONFIG"")|} ?? ""default"";
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentExpandEnvironmentVariablesInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var expanded = {|#0:Environment.ExpandEnvironmentVariables(""%TEMP%\\myapp"")|};
        await context.CallActivityAsync(""ProcessPath"", expanded);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task EnvironmentCurrentDirectoryInOrchestratorShouldReportDFA0005()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var currentDir = {|#0:Environment.CurrentDirectory|};
        await context.CallActivityAsync(""ProcessDirectory"", currentDir);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0005")
                .WithLocation(0)
                .WithMessage("Environment variable read is non-deterministic.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}