using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using DtfDeterminismAnalyzer.Analyzers;
using DtfDeterminismAnalyzer.CodeFixes;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Unit tests for Dfa0001TimeApiCodeFix.
    /// Tests automatic code fixes for DateTime and Stopwatch API usage in orchestrator functions.
    /// </summary>
    [TestFixture]
    public class Dfa0001TimeApiCodeFixTests
    {
        /// <summary>
        /// Base test configuration for code fix tests
        /// </summary>
        private static CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> CreateTest()
        {
            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = 
                new CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier>
                {
                    ReferenceAssemblies = AzureFunctionsReferences.CreateFullReferenceAssemblies(),
                };
            return test;
        }

        #region DateTime.Now Code Fix Tests

        [Test]
        public async Task DateTimeNow_InOrchestrator_ShouldOfferCodeFix()
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
        var now = {|#0:DateTime.Now|};
        return now.ToString();
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
        var now = context.CurrentUtcDateTime;
        return now.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0001")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic time API used in orchestrator"));

            await test.RunAsync();
        }

        [Test]
        public async Task DateTimeUtcNow_InOrchestrator_ShouldOfferCodeFix()
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
        var utcNow = {|#0:DateTime.UtcNow|};
        return utcNow.ToString();
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
        var utcNow = context.CurrentUtcDateTime;
        return utcNow.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0001")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic time API used in orchestrator"));

            await test.RunAsync();
        }

        [Test]
        public async Task DateTimeToday_InOrchestrator_ShouldOfferCodeFix()
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
        var today = {|#0:DateTime.Today|};
        return today.ToString();
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
        var today = context.CurrentUtcDateTime.Date;
        return today.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0001")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic time API used in orchestrator"));

            await test.RunAsync();
        }

        #endregion

        #region Stopwatch Code Fix Tests

        [Test]
        public async Task StopwatchStartNew_InOrchestrator_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var stopwatch = {|#0:Stopwatch.StartNew()|};
        return stopwatch.ToString();
    }
}";

            const string fixedCode = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var stopwatch = context.CurrentUtcDateTime;
        return stopwatch.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0001")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic time API used in orchestrator"));

            await test.RunAsync();
        }

        [Test]
        public async Task StopwatchGetTimestamp_InOrchestrator_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var timestamp = {|#0:Stopwatch.GetTimestamp()|};
        return timestamp.ToString();
    }
}";

            const string fixedCode = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var timestamp = context.CurrentUtcDateTime.Ticks;
        return timestamp.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0001")
                    .WithLocation(0)
                    .WithMessage("Non-deterministic time API used in orchestrator"));

            await test.RunAsync();
        }

        #endregion

        #region Activity Function Tests (Should Not Trigger)

        [Test]
        public async Task DateTimeNow_InActivityFunction_ShouldNotOfferCodeFix()
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
        var now = DateTime.Now; // Should not trigger DFA0001 in activity
        return now.ToString();
    }
}";

            CSharpCodeFixTest<Dfa0001TimeApiAnalyzer, Dfa0001TimeApiCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            // No FixedCode because no diagnostic should be reported
            // No ExpectedDiagnostics because activity functions should not trigger DFA0001

            await test.RunAsync();
        }

        #endregion
    }
}