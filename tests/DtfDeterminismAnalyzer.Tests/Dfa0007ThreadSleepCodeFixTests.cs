using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using DtfDeterminismAnalyzer.Analyzers;
using DtfDeterminismAnalyzer.CodeFixes;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Unit tests for Dfa0007ThreadSleepCodeFix.
    /// Tests automatic code fixes for Thread.Sleep usage in orchestrator functions.
    /// </summary>
    [TestFixture]
    public class Dfa0007ThreadSleepCodeFixTests
    {
        /// <summary>
        /// Base test configuration for code fix tests
        /// </summary>
        private static CSharpCodeFixTest<Dfa0007ThreadBlockingAnalyzer, Dfa0007ThreadSleepCodeFix, DefaultVerifier> CreateTest()
        {
            CSharpCodeFixTest<Dfa0007ThreadBlockingAnalyzer, Dfa0007ThreadSleepCodeFix, DefaultVerifier> test = 
                new CSharpCodeFixTest<Dfa0007ThreadBlockingAnalyzer, Dfa0007ThreadSleepCodeFix, DefaultVerifier>
                {
                    ReferenceAssemblies = AzureFunctionsReferences.CreateFullReferenceAssemblies(),
                };
            return test;
        }

        #region Thread.Sleep(int) Code Fix Tests

        [Test]
        public async Task ThreadSleepInt_InOrchestrator_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:Thread.Sleep(1000)|};
        return ""Done"";
    }
}";

            const string fixedCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await context.CreateTimer(context.CurrentUtcDateTime.AddMilliseconds(1000), CancellationToken.None);
        return ""Done"";
    }
}";

            CSharpCodeFixTest<Dfa0007ThreadBlockingAnalyzer, Dfa0007ThreadSleepCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0007")
                    .WithLocation(0)
                    .WithMessage("Thread-blocking call detected"));

            await test.RunAsync();
        }

        [Test]
        public async Task ThreadSleepTimeSpan_InOrchestrator_ShouldOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        {|#0:Thread.Sleep(TimeSpan.FromSeconds(5))|};
        return ""Done"";
    }
}";

            const string fixedCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        await context.CreateTimer(context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(5)), CancellationToken.None);
        return ""Done"";
    }
}";

            CSharpCodeFixTest<Dfa0007ThreadBlockingAnalyzer, Dfa0007ThreadSleepCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            test.FixedCode = fixedCode;
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError("DFA0007")
                    .WithLocation(0)
                    .WithMessage("Thread-blocking call detected"));

            await test.RunAsync();
        }

        #endregion

        #region Activity Function Tests (Should Not Trigger)

        [Test]
        public async Task ThreadSleep_InActivityFunction_ShouldNotOfferCodeFix()
        {
            const string testCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestActivity
{
    [FunctionName(""TestActivity"")]
    public string RunActivity([ActivityTrigger] string input)
    {
        Thread.Sleep(1000); // Should not trigger DFA0007 in activity
        return ""Done"";
    }
}";

            CSharpCodeFixTest<Dfa0007ThreadBlockingAnalyzer, Dfa0007ThreadSleepCodeFix, DefaultVerifier> test = CreateTest();
            test.TestCode = testCode;
            // No FixedCode because no diagnostic should be reported
            // No ExpectedDiagnostics because activity functions should not trigger DFA0007

            await test.RunAsync();
        }

        #endregion
    }
}