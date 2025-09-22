using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<DtfDeterminismAnalyzer.Analyzers.Dfa0002GuidAnalyzer>;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for DFA0002: GUID generation detection in Durable Task Framework orchestrators.
    /// These tests validate that the analyzer detects non-deterministic GUID generation and reports appropriate diagnostics.
    /// </summary>
    [TestFixture]
    public class Dfa0002GuidTests
    {
        private const string OrchestrationTriggerUsing = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
";

        [Test]
        public async Task GuidNewGuidInOrchestratorShouldReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var newGuid = {|#0:Guid.NewGuid()|};
        await context.CallActivityAsync(""SomeActivity"", newGuid);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0002")
                .WithLocation(0)
                .WithMessage("Non-deterministic GUID generated in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task GuidNewGuidAssignedToVariableInOrchestratorShouldReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Guid correlationId = {|#0:Guid.NewGuid()|};
        var input = new { Id = correlationId, Data = ""test"" };
        await context.CallActivityAsync(""ProcessData"", input);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0002")
                .WithLocation(0)
                .WithMessage("Non-deterministic GUID generated in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task GuidNewGuidInMethodCallInOrchestratorShouldReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await context.CallActivityAsync(""SomeActivity"", {|#0:Guid.NewGuid()|});
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0002")
                .WithLocation(0)
                .WithMessage("Non-deterministic GUID generated in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task GuidNewGuidInPropertyInitializerInOrchestratorShouldReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var request = new { RequestId = {|#0:Guid.NewGuid()|}, Data = ""test"" };
        await context.CallActivityAsync(""ProcessRequest"", request);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0002")
                .WithLocation(0)
                .WithMessage("Non-deterministic GUID generated in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task GuidNewGuidInActivityFunctionShouldNotReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public async Task<Guid> RunActivity([ActivityTrigger] string input)
    {
        var newGuid = Guid.NewGuid(); // This should be allowed in activities
        return newGuid;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task GuidNewGuidInRegularClassShouldNotReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class RegularService
{
    public Guid GenerateId()
    {
        return Guid.NewGuid(); // This should be allowed outside orchestrators
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task ContextNewGuidInOrchestratorShouldNotReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var deterministicGuid = context.NewGuid(); // This should be allowed - it's deterministic
        await context.CallActivityAsync(""SomeActivity"", deterministicGuid);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task MultipleGuidNewGuidInOrchestratorShouldReportMultipleDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var guid1 = {|#0:Guid.NewGuid()|};
        var guid2 = {|#1:Guid.NewGuid()|};
        var request = new { Id1 = guid1, Id2 = guid2, Id3 = {|#2:Guid.NewGuid()|} };
        
        await context.CallActivityAsync(""ProcessMultipleIds"", request);
    }
}";

            var expected = new[]
            {
                VerifyCS.Diagnostic("DFA0002").WithLocation(0).WithMessage("Non-deterministic GUID generated in orchestrator."),
                VerifyCS.Diagnostic("DFA0002").WithLocation(1).WithMessage("Non-deterministic GUID generated in orchestrator."),
                VerifyCS.Diagnostic("DFA0002").WithLocation(2).WithMessage("Non-deterministic GUID generated in orchestrator.")
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task GuidNewGuidInNestedMethodInOrchestratorShouldReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = GenerateCorrelationId();
        await context.CallActivityAsync(""SomeActivity"", id);
    }

    private Guid GenerateCorrelationId()
    {
        return {|#0:Guid.NewGuid()|}; // Should be detected even in helper methods within orchestrator class
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0002")
                .WithLocation(0)
                .WithMessage("Non-deterministic GUID generated in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Test]
        public async Task GuidConstructorWithByteArrayInOrchestratorShouldNotReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Creating GUID from deterministic byte array should be allowed
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var deterministicGuid = new Guid(bytes);
        await context.CallActivityAsync(""SomeActivity"", deterministicGuid);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task GuidParseInOrchestratorShouldNotReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Parsing deterministic GUID string should be allowed
        var parsedGuid = Guid.Parse(""12345678-1234-1234-1234-123456789012"");
        await context.CallActivityAsync(""SomeActivity"", parsedGuid);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task GuidEmptyInOrchestratorShouldNotReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Using Guid.Empty should be allowed - it's deterministic
        var emptyGuid = Guid.Empty;
        await context.CallActivityAsync(""SomeActivity"", emptyGuid);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task GuidNewGuidInConditionalExpressionInOrchestratorShouldReportDFA0002()
        {
            var testCode = OrchestrationTriggerUsing + @"
public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var input = context.GetInput<string>();
        var id = string.IsNullOrEmpty(input) ? {|#0:Guid.NewGuid()|} : Guid.Parse(input);
        await context.CallActivityAsync(""SomeActivity"", id);
    }
}";

            var expected = VerifyCS.Diagnostic("DFA0002")
                .WithLocation(0)
                .WithMessage("Non-deterministic GUID generated in orchestrator.");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}