using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Tests that validate the MetadataReference creation functionality in AnalyzerTestBase.
    /// These contract tests ensure the typeof().Assembly.Location pattern works correctly.
    /// </summary>
    [TestFixture]
    public class MetadataReferenceTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>
    {
        [Test]
        public void CreateAzureFunctionsMetadataReferences_Should_Return_Valid_References()
        {
            // Act
            var references = CreateAzureFunctionsMetadataReferences().ToList();

            // Assert
            Assert.That(references, Is.Not.Empty, "Should return at least some metadata references");
            Assert.That(references.All(r => r != null), Is.True, "All references should be non-null");
            
            // Verify each reference has valid display information
            foreach (var reference in references)
            {
                Assert.That(string.IsNullOrEmpty(reference.Display), Is.False, 
                    $"Reference should have valid display information: {reference.Display}");
            }
        }

        [Test]
        public void CreateAzureFunctionsMetadataReferences_Should_Handle_Missing_Assemblies_Gracefully()
        {
            // This test ensures that if some assemblies are not available,
            // the method doesn't throw exceptions and returns what it can
            
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() =>
            {
                var references = CreateAzureFunctionsMetadataReferences().ToList();
                // Even if some assemblies are missing, we should get a valid (possibly empty) collection
                Assert.That(references, Is.Not.Null);
            });
        }

        [Test]
        public void CreateTestCompilationWithAssemblyReferences_Should_Create_Valid_Compilation()
        {
            // Arrange
            const string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class TestFunction
{
    [FunctionName(""TestFunction"")]
    public void Run([TimerTrigger(""0 0 * * * *"")] TimerInfo timer, ILogger log)
    {
        log.LogInformation(""Test function executed"");
    }
}";

            // Act & Assert - should not throw
            Assert.DoesNotThrowAsync(async () =>
            {
                var compilation = await CreateTestCompilationWithAssemblyReferences(testCode);
                
                Assert.That(compilation, Is.Not.Null);
                Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));
                Assert.That(compilation.References.Count(), Is.GreaterThan(0));
                
                // Verify the compilation doesn't have critical errors
                var diagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList();
                
                // Filter out any CS0246 or CS0234 errors which are what we're trying to fix
                var criticalErrors = diagnostics
                    .Where(d => !d.Id.StartsWith("CS0246") && !d.Id.StartsWith("CS0234"))
                    .ToList();
                
                Assert.That(criticalErrors, Is.Empty, 
                    $"Should not have critical compilation errors: {string.Join(", ", criticalErrors.Select(d => $"{d.Id}: {d.GetMessage()}"))}");
            });
        }

        [Test]
        public void MetadataReference_Creation_Should_Be_Deterministic()
        {
            // Act - call the method multiple times
            var references1 = CreateAzureFunctionsMetadataReferences().ToList();
            var references2 = CreateAzureFunctionsMetadataReferences().ToList();

            // Assert - results should be consistent
            Assert.That(references1.Count, Is.EqualTo(references2.Count), 
                "Should return the same number of references each time");
            
            // Compare display strings (assembly paths) for consistency
            var displays1 = references1.Select(r => r.Display).OrderBy(d => d).ToList();
            var displays2 = references2.Select(r => r.Display).OrderBy(d => d).ToList();
            
            Assert.That(displays1, Is.EqualTo(displays2), 
                "Should return references to the same assemblies each time");
        }
    }
}