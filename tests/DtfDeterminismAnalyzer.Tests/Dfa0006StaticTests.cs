using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0006StaticAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0006: Static state detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic static state access and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0006StaticTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

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
        {|#0:_sharedState = ""modified""|};
        await context.CallActivityAsync(""ProcessState"", _sharedState);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        var currentState = {|#0:_sharedState|};
        await context.CallActivityAsync(""ProcessState"", currentState);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:SharedValue = ""modified""|};
        await context.CallActivityAsync(""ProcessValue"", SharedValue);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:_sharedList.Add(item)|};
        await context.CallActivityAsync(""ProcessList"", _sharedList);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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

            await VerifyCS.VerifyAnalyzerAsync(testCode);
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
        {|#0:_state1 = ""modified1""|};
        var currentState2 = {|#1:_state2|};
        
        await context.CallActivityAsync(""ProcessStates"", new { _state1, currentState2 });
    }
}";

            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic("DFA0006").WithLocation(0).WithMessage("Static state may change across replays."),
                VerifyCS.Diagnostic("DFA0006").WithLocation(1).WithMessage("Static state may change across replays.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
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
        {|#0:_sharedState = newValue|};
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        {|#0:_sharedDictionary[key] = ""value""|};
        await context.CallActivityAsync(""ProcessDictionary"", _sharedDictionary);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

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
        var value = {|#0:_lazyValue.Value|};
        await context.CallActivityAsync(""ProcessValue"", value);
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("DFA0006")
                .WithLocation(0)
                .WithMessage("Static state may change across replays.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
