using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0003RandomAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0003: Random usage detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic Random usage and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0003RandomTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task RandomConstructorNoSeedInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = {|#0:new Random()|};
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomSharedFieldInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Random _random = {|#0:new Random()|};

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = _random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomNextMethodInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = {|#0:new Random()|};
        var randomValue = random.Next();
        await context.CallActivityAsync(""ProcessRandom"", randomValue);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomNextDoubleMethodInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = {|#0:new Random()|};
        var randomValue = random.NextDouble();
        await context.CallActivityAsync(""ProcessRandom"", randomValue);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomNextBytesMethodInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random = {|#0:new Random()|};
        var bytes = new byte[10];
        random.NextBytes(bytes);
        await context.CallActivityAsync(""ProcessBytes"", bytes);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomWithFixedSeedInOrchestratorShouldNotReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Fixed seed makes Random deterministic - should be allowed
        var random = new Random(12345);
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task RandomWithContextBasedSeedInOrchestratorShouldNotReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Deterministic seed based on context - should be allowed
        var seed = context.CurrentUtcDateTime.GetHashCode();
        var random = new Random(seed);
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task RandomInActivityFunctionShouldNotReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<int> RunActivity([ActivityTrigger] string input)
    {
        // Random usage in activities should be allowed
        var random = new Random();
        return random.Next(1, 100);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task RandomInRegularClassShouldNotReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class RandomService
{
    private readonly Random _random = new Random();

    public int GetRandomValue()
    {
        // Random usage outside orchestrators should be allowed
        return _random.Next(1, 100);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleRandomInOrchestratorShouldReportMultipleDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var random1 = {|#0:new Random()|};
        var random2 = {|#1:new Random()|};
        
        var value1 = random1.Next();
        var value2 = random2.NextDouble();
        
        await context.CallActivityAsync(""ProcessValues"", new { value1, value2 });
    }
}";

            var expected = new[]
            {
                VerifyCS.Diagnostic("DFA0003").WithLocation(0).WithMessage("Non-deterministic random used in orchestrator."),
                VerifyCS.Diagnostic("DFA0003").WithLocation(1).WithMessage("Non-deterministic random used in orchestrator.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomInNestedMethodInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = GenerateRandomValue();
        await context.CallActivityAsync(""SomeActivity"", value);
    }

    private int GenerateRandomValue()
    {
        var random = {|#0:new Random()|}; // Should be detected even in helper methods within orchestrator class
        return random.Next(1, 100);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomSharedSeededInOrchestratorShouldNotReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    private static readonly Random _random = new Random(42); // Fixed seed should be allowed

    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var value = _random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task RandomInConditionalBranchInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var input = context.GetInput<bool>();
        var value = input ? {|#0:new Random()|}.Next() : 42;
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task RandomWithVariableSeedInOrchestratorShouldReportDFA0003()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Non-deterministic seed based on current time - should be flagged
        var seed = DateTime.Now.Millisecond;
        var random = {|#0:new Random(seed)|};
        var value = random.Next(1, 100);
        await context.CallActivityAsync(""SomeActivity"", value);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0003")
                .WithLocation(0)
                .WithMessage("Non-deterministic random used in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}