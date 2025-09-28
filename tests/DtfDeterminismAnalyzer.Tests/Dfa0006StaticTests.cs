using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;


namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0006: Static state detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic static state access and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0006StaticTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0006StaticAnalyzer>
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        private async Task VerifyDFA0006Diagnostic(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0006").ToList();
            Assert.AreEqual(1, analyzerDiagnostics.Count, "Should report exactly one DFA0006 diagnostic");
            var diagnostic = analyzerDiagnostics[0];
            Assert.AreEqual("Static field access in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                "Diagnostic message should match expected message");
        }

        private async Task VerifyNoDiagnostics(string testCode)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0006").ToList();
            Assert.AreEqual(0, analyzerDiagnostics.Count, "Should report no DFA0006 diagnostics");
        }

        private async Task VerifyMultipleDFA0006Diagnostics(string testCode, int expectedCount)
        {
            var result = await RunAnalyzerTest(testCode);
            Assert.IsTrue(result.CompilationSucceeded, 
                $"Compilation should succeed. Errors: {string.Join(", ", result.CompilationDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
            var analyzerDiagnostics = result.AnalyzerDiagnostics.Where(d => d.Id == "DFA0006").ToList();
            Assert.AreEqual(expectedCount, analyzerDiagnostics.Count, $"Should report exactly {expectedCount} DFA0006 diagnostics");
            foreach (var diagnostic in analyzerDiagnostics)
            {
                Assert.AreEqual("Static field access in orchestrator", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), 
                    "Diagnostic message should match expected message");
            }
        }

        [Test]
        public async Task StaticFieldWriteInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static string _sharedState = ""initial"";

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        _sharedState = ""modified"";
        await context.CallActivityAsync(""ProcessState"", _sharedState);
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }

        [Test]
        public async Task StaticFieldReadMutableInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static string _sharedState = ""initial"";

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var currentState = _sharedState;
        await context.CallActivityAsync(""ProcessState"", currentState);
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }

        [Test]
        public async Task StaticPropertyWriteInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static string SharedValue { get; set; } = ""initial"";

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        SharedValue = ""modified"";
        await context.CallActivityAsync(""ProcessValue"", SharedValue);
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }

        [Test]
        public async Task StaticCollectionModificationInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
using System.Collections.Generic;

public class TestOrchestrator
{
    private static readonly List<string> _sharedList = new List<string>();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var item = context.GetInput<string>();
        _sharedList.Add(item);
        await context.CallActivityAsync(""ProcessList"", _sharedList);
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }

        [Test]
        public async Task StaticReadonlyConstInOrchestratorShouldNotReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly string ReadOnlyValue = ""constant"";
    private const string ConstantValue = ""constant"";

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Reading readonly/const values should be allowed - they're immutable
        var value1 = ReadOnlyValue;
        var value2 = ConstantValue;
        await context.CallActivityAsync(""ProcessValues"", new { value1, value2 });
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task StaticImmutableCollectionInOrchestratorShouldNotReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
using System.Collections.Generic;
using System.Linq;

public class TestOrchestrator
{
    private static readonly IReadOnlyList<string> ReadOnlyList = new[] { ""item1"", ""item2"" };

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Reading immutable collections should be allowed
        var items = ReadOnlyList.ToList();
        await context.CallActivityAsync(""ProcessItems"", items);
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task StaticFieldInActivityFunctionShouldNotReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    private static string _sharedState = ""initial"";

    [FunctionName(""TestActivity"")]
    public async Task<string> RunActivity([ActivityTrigger] string input)
    {
        // Static state access in activities should be allowed
        _sharedState = input;
        return _sharedState;
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task StaticFieldInRegularClassShouldNotReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class StatefulService
{
    private static int _counter = 0;

    public int GetNextValue()
    {
        // Static state access outside orchestrators should be allowed
        return ++_counter;
    }
}";

            await VerifyNoDiagnostics(testCode);
        }

        [Test]
        public async Task MultipleStaticAccessInOrchestratorShouldReportMultipleDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static string _state1 = ""initial1"";
    private static string _state2 = ""initial2"";

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        _state1 = ""modified1"";
        var currentState2 = _state2;
        
        await context.CallActivityAsync(""ProcessStates"", new { _state1, currentState2 });
    }
}";

            await VerifyMultipleDFA0006Diagnostics(testCode, 2);
        }

        [Test]
        public async Task StaticAccessInNestedMethodInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static string _sharedState = ""initial"";

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        ModifySharedState(""modified"");
        await context.CallActivityAsync(""ProcessState"", _sharedState);
    }

    private void ModifySharedState(string newValue)
    {
        // Should be detected even in helper methods within orchestrator class
        _sharedState = newValue;
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }

        [Test]
        public async Task StaticDictionaryModificationInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
using System.Collections.Generic;

public class TestOrchestrator
{
    private static readonly Dictionary<string, string> _sharedDictionary = new Dictionary<string, string>();

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var key = context.GetInput<string>();
        _sharedDictionary[key] = ""value"";
        await context.CallActivityAsync(""ProcessDictionary"", _sharedDictionary);
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }

        [Test]
        public async Task StaticLazyAccessInOrchestratorShouldReportDFA0006()
        {
            string testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Lazy<string> _lazyValue = new Lazy<string>(() => DateTime.Now.ToString());

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Lazy initialization can be non-deterministic
        var value = _lazyValue.Value;
        await context.CallActivityAsync(""ProcessValue"", value);
    }
}";

            await VerifyDFA0006Diagnostic(testCode);        }
    }
}
