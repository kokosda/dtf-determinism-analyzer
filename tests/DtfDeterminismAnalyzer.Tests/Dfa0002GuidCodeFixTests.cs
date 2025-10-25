using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using DtfDeterminismAnalyzer.Analyzers;
using DtfDeterminismAnalyzer.CodeFixes;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Unit tests for Dfa0002GuidCodeFix.
    /// Tests automatic code fixes for Guid.NewGuid() usage in orchestrator functions.
    /// </summary>
    [TestFixture]
    public class Dfa0002GuidCodeFixTests
    {
        /// <summary>
        /// Base test configuration for code fix tests
        /// </summary>
        private static CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> CreateTest()
        {
            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = 
                new CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier>
                {
                    ReferenceAssemblies = AzureFunctionsReferences.CreateFullReferenceAssemblies(),
                };
            return test;
        }

        #region Basic Guid.NewGuid() Code Fix Tests

        [Test]
        public async Task GuidNewGuid_InOrchestrator_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = {|#0:Guid.NewGuid()|};
        return id.ToString();
    }
}";

            const string fixedCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = context.NewGuid();
        return id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));

            await test.RunAsync();
        }

        [Test]
        public async Task GuidNewGuid_AssignedToVariable_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Guid correlationId = {|#0:Guid.NewGuid()|};
        return correlationId.ToString();
    }
}";

            const string fixedCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        Guid correlationId = context.NewGuid();
        return correlationId.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));

            await test.RunAsync();
        }

        #endregion

        #region Property and Complex Expression Tests

        [Test]
        public async Task GuidNewGuid_InPropertyInitializer_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var data = new { Id = {|#0:Guid.NewGuid()|}, Name = ""Test"" };
        return data.Id.ToString();
    }
}";

            const string fixedCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var data = new { Id = context.NewGuid(), Name = ""Test"" };
        return data.Id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));

            await test.RunAsync();
        }

        [Test]
        public async Task GuidNewGuid_InConditionalExpression_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var condition = true;
        var id = condition ? {|#0:Guid.NewGuid()|} : Guid.Empty;
        return id.ToString();
    }
}";

            const string fixedCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var condition = true;
        var id = condition ? context.NewGuid() : Guid.Empty;
        return id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));

            await test.RunAsync();
        }

        #endregion

        #region Multiple Guid Usage Tests

        [Test]
        public async Task MultipleGuidNewGuid_InOrchestrator_ShouldOfferMultipleFixes()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id1 = {|#0:Guid.NewGuid()|};
        var id2 = {|#1:Guid.NewGuid()|};
        var id3 = {|#2:Guid.NewGuid()|};
        return $""{id1},{id2},{id3}"";
    }
}";

            const string fixedCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id1 = context.NewGuid();
        var id2 = context.NewGuid();
        var id3 = context.NewGuid();
        return $""{id1},{id2},{id3}"";
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(1)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0002")
                    .WithLocation(2)
                    .WithMessage("Non-deterministic GUID generated in orchestrator"));

            await test.RunAsync();
        }

        #endregion

        #region Activity Function Tests (Should Not Trigger)

        [Test]
        public async Task GuidNewGuid_InActivityFunction_ShouldNotOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public string RunActivity([ActivityTrigger] string input)
    {
        var id = Guid.NewGuid(); // Should not trigger DFA0002 in activity
        return id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            // No FixedCode because no diagnostic should be reported
            // No ExpectedDiagnostics because activity functions should not trigger DFA0002

            await test.RunAsync();
        }

        #endregion

        #region Valid Guid Usage Tests (Should Not Trigger)

        [Test]
        public async Task GuidParse_InOrchestrator_ShouldNotOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = Guid.Parse(""00000000-0000-0000-0000-000000000000""); // Should not trigger DFA0002
        return id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            // No FixedCode because no diagnostic should be reported
            // No ExpectedDiagnostics because Guid.Parse is deterministic

            await test.RunAsync();
        }

        [Test]
        public async Task GuidEmpty_InOrchestrator_ShouldNotOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = Guid.Empty; // Should not trigger DFA0002
        return id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            // No FixedCode because no diagnostic should be reported
            // No ExpectedDiagnostics because Guid.Empty is deterministic

            await test.RunAsync();
        }

        [Test]
        public async Task ContextNewGuid_InOrchestrator_ShouldNotOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var id = context.NewGuid(); // Should not trigger DFA0002 - this is correct usage
        return id.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0002GuidAnalyzer, Dfa0002GuidCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            // No FixedCode because no diagnostic should be reported
            // No ExpectedDiagnostics because context.NewGuid() is the correct replacement

            await test.RunAsync();
        }

        #endregion
    }
}
