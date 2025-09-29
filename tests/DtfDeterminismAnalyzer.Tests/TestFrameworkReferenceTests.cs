using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for TestFrameworkAssemblyReferences entity.
    /// These tests validate that the test framework properly includes Azure Functions assembly references.
    /// CRITICAL: These tests MUST FAIL before implementation to follow TDD approach.
    /// </summary>
    [TestFixture]
    public class TestFrameworkReferenceTests
    {
        [Test]
        public void TestFramework_WithDefaultSetup_IncludesAzureFunctionsWebJobsAssembly()
        {
            // Contract: TestFrameworkConfiguration must include Microsoft.Azure.WebJobs assembly
            var testBase = new AnalyzerTestBase<Analyzers.Dfa0001TimeApiAnalyzer>();
            IEnumerable<MetadataReference> references = testBase.GetReferenceAssemblies();

            MetadataReference? webJobsAssembly = references.FirstOrDefault(r => 
                r.Display?.Contains("Microsoft.Azure.WebJobs") == true);
            
            Assert.IsNotNull(webJobsAssembly, 
                "Microsoft.Azure.WebJobs assembly must be included in test framework references");
        }

        [Test]
        public void TestFramework_WithDefaultSetup_IncludesDurableTaskExtensionsAssembly()
        {
            // Contract: TestFrameworkConfiguration must include DurableTask extensions
            var testBase = new AnalyzerTestBase<Analyzers.Dfa0001TimeApiAnalyzer>();
            IEnumerable<MetadataReference> references = testBase.GetReferenceAssemblies();

            MetadataReference? durableTaskAssembly = references.FirstOrDefault(r => 
                r.Display?.Contains("Microsoft.Azure.WebJobs.Extensions.DurableTask") == true);
            
            Assert.IsNotNull(durableTaskAssembly, 
                "Microsoft.Azure.WebJobs.Extensions.DurableTask assembly must be included in test framework references");
        }

        [Test]
        public void TestFramework_WithDefaultSetup_IncludesAspNetCoreMvcAssembly()
        {
            // Contract: TestFrameworkConfiguration must include ASP.NET Core MVC types
            var testBase = new AnalyzerTestBase<Analyzers.Dfa0001TimeApiAnalyzer>();
            IEnumerable<MetadataReference> references = testBase.GetReferenceAssemblies();

            MetadataReference? mvcAssembly = references.FirstOrDefault(r => 
                r.Display?.Contains("Microsoft.AspNetCore.Mvc") == true);
            
            Assert.IsNotNull(mvcAssembly, 
                "Microsoft.AspNetCore.Mvc assembly must be included in test framework references for HTTP trigger types");
        }

        [Test]
        public void TestFramework_WithDefaultSetup_IncludesExtensionsLoggingAssembly()
        {
            // Contract: TestFrameworkConfiguration must include Microsoft.Extensions.Logging
            var testBase = new AnalyzerTestBase<Analyzers.Dfa0001TimeApiAnalyzer>();
            IEnumerable<MetadataReference> references = testBase.GetReferenceAssemblies();

            MetadataReference? loggingAssembly = references.FirstOrDefault(r => 
                r.Display?.Contains("Microsoft.Extensions.Logging") == true);
            
            Assert.IsNotNull(loggingAssembly, 
                "Microsoft.Extensions.Logging assembly must be included in test framework references for ILogger interface");
        }

        [Test]
        public void TestFramework_WithDefaultSetup_UsesNet80ReferenceAssemblies()
        {
            // Contract: TestFrameworkConfiguration must use .NET 8.0 reference assemblies
            var testBase = new AnalyzerTestBase<Analyzers.Dfa0001TimeApiAnalyzer>();
            ReferenceAssemblies referenceAssemblies = testBase.TestReferenceAssemblies;
            
            Assert.IsNotNull(referenceAssemblies, "ReferenceAssemblies must be configured");
            Assert.AreEqual("net8.0", referenceAssemblies.TargetFramework, 
                "Test framework must target .NET 8.0 for compatibility with analyzer project");
        }
    }
}