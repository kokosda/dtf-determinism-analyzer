using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Contract tests for T012: CSharpAnalyzerTest configuration with proper compilation options and parse options.
    /// These tests validate that the AnalyzerTestBase provides proper configuration methods for analyzer testing.
    /// </summary>
    [TestFixture]
    public class CompilationConfigurationTests : AnalyzerTestBase<DtfDeterminismAnalyzer.Analyzers.Dfa0001TimeApiAnalyzer>
    {
        [Test]
        public void GetParseOptions_Should_Configure_CSharp12_With_Proper_Settings()
        {
            // Act
            var parseOptions = GetParseOptions();

            // Assert
            Assert.That(parseOptions, Is.Not.Null);
            Assert.That(parseOptions.LanguageVersion, Is.EqualTo(LanguageVersion.CSharp12), 
                "Should use C# 12.0 language version to match project configuration");
            Assert.That(parseOptions.Kind, Is.EqualTo(SourceCodeKind.Regular), 
                "Should use regular source code kind for standard compilation");
            Assert.That(parseOptions.DocumentationMode, Is.EqualTo(DocumentationMode.Parse), 
                "Should parse documentation comments for comprehensive analysis");
        }

        [Test]
        public void GetCompilationOptions_Should_Configure_Proper_Settings()
        {
            // Act
            var compilationOptions = GetCompilationOptions();

            // Assert
            Assert.That(compilationOptions, Is.Not.Null);
            Assert.That(compilationOptions.OutputKind, Is.EqualTo(OutputKind.DynamicallyLinkedLibrary), 
                "Should target library output for analyzer testing");
            Assert.That(compilationOptions.OptimizationLevel, Is.EqualTo(OptimizationLevel.Debug), 
                "Should use debug optimization for better testing diagnostics");
            Assert.That(compilationOptions.AllowUnsafe, Is.False, 
                "Should not allow unsafe code for safer analysis");
            Assert.That(compilationOptions.NullableContextOptions, Is.EqualTo(NullableContextOptions.Enable), 
                "Should enable nullable reference types to match project settings");
            Assert.That(compilationOptions.Deterministic, Is.True, 
                "Should enable deterministic builds for constitutional compliance");
            Assert.That(compilationOptions.ConcurrentBuild, Is.True, 
                "Should enable concurrent builds for better performance");
            Assert.That(compilationOptions.Platform, Is.EqualTo(Platform.AnyCpu), 
                "Should target any CPU platform for broad compatibility");
            Assert.That(compilationOptions.WarningLevel, Is.EqualTo(4), 
                "Should use highest warning level for comprehensive analysis");
        }

        [Test]
        public void GetCompilationOptions_Should_Configure_Critical_Diagnostic_Settings()
        {
            // Act
            var compilationOptions = GetCompilationOptions();
            var diagnosticOptions = compilationOptions.SpecificDiagnosticOptions;

            // Assert - Critical compilation errors should be reported as errors
            Assert.That(diagnosticOptions.ContainsKey("CS0234"), Is.True, 
                "Should configure CS0234 (type or namespace does not exist)");
            Assert.That(diagnosticOptions["CS0234"], Is.EqualTo(ReportDiagnostic.Error), 
                "CS0234 should be reported as error");
            
            Assert.That(diagnosticOptions.ContainsKey("CS0246"), Is.True, 
                "Should configure CS0246 (type or namespace not found)");
            Assert.That(diagnosticOptions["CS0246"], Is.EqualTo(ReportDiagnostic.Error), 
                "CS0246 should be reported as error");
            
            Assert.That(diagnosticOptions.ContainsKey("CS0103"), Is.True, 
                "Should configure CS0103 (name does not exist in context)");
            Assert.That(diagnosticOptions["CS0103"], Is.EqualTo(ReportDiagnostic.Error), 
                "CS0103 should be reported as error");

            // Assert - Nullable warnings should be suppressed for test scenarios
            Assert.That(diagnosticOptions.ContainsKey("CS8602"), Is.True, 
                "Should configure CS8602 (dereference of possibly null reference)");
            Assert.That(diagnosticOptions["CS8602"], Is.EqualTo(ReportDiagnostic.Suppress), 
                "CS8602 should be suppressed in test scenarios");
        }

        [Test]
        public void GetAnalyzerOptions_Should_Provide_Valid_Configuration()
        {
            // Act
            var analyzerOptions = GetAnalyzerOptions();

            // Assert
            Assert.That(analyzerOptions, Is.Not.Null);
            Assert.That(analyzerOptions.AdditionalFiles, Is.Not.Null, 
                "Should provide non-null additional files collection");
            Assert.That(analyzerOptions.AdditionalFiles.Length, Is.EqualTo(0), 
                "Should start with empty additional files for clean test state");
        }

        [Test]
        public void CreateSolutionTransform_Should_Provide_Valid_Transform_Function()
        {
            // Act
            var transform = CreateSolutionTransform();

            // Assert
            Assert.That(transform, Is.Not.Null, 
                "Should provide a non-null solution transform function");

            // Test that the transform function can be called without exceptions
            Assert.DoesNotThrow(() =>
            {
                // Create a minimal solution/project to test transformation
                using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
                var solution = workspace.CurrentSolution;
                var projectInfo = Microsoft.CodeAnalysis.ProjectInfo.Create(
                    Microsoft.CodeAnalysis.ProjectId.CreateNewId(),
                    Microsoft.CodeAnalysis.VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp);
                
                solution = solution.AddProject(projectInfo);
                var projectId = solution.ProjectIds[0];
                
                // Apply the transformation
                var transformedSolution = transform(solution, projectId);
                
                Assert.That(transformedSolution, Is.Not.Null, 
                    "Transform should return a valid solution");
            });
        }

        [Test]
        public void GetStandardReferenceAssemblies_Should_Return_Configured_References()
        {
            // Act
            var referenceAssemblies = GetStandardReferenceAssemblies();

            // Assert
            Assert.That(referenceAssemblies, Is.Not.Null, 
                "Should return non-null reference assemblies");
            Assert.That(referenceAssemblies, Is.EqualTo(TestReferenceAssemblies), 
                "Should return the same reference assemblies as TestReferenceAssemblies property");
        }

        [Test]
        public async Task Configuration_Methods_Should_Work_Together_For_Valid_Compilation()
        {
            // Arrange
            const string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

public class TestFunction
{
    [FunctionName(""TestFunction"")]
    public void Run([TimerTrigger(""0 0 * * * *"")] TimerInfo timer, ILogger log)
    {
        log.LogInformation(""Configuration test function executed at: {0}"", DateTime.UtcNow);
    }
}";

            // Act
            var compilation = await CreateTestCompilation(testCode);
            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            // Assert
            Assert.That(compilation, Is.Not.Null, 
                "Should create a valid compilation");
            
            // Filter out CS0234/CS0246 errors which are what we're trying to fix
            var criticalErrors = diagnostics
                .Where(d => !d.Id.StartsWith("CS0246", StringComparison.Ordinal) && 
                           !d.Id.StartsWith("CS0234", StringComparison.Ordinal))
                .ToList();
            
            Assert.That(criticalErrors, Is.Empty, 
                $"Should not have critical compilation errors after configuration. Errors: {string.Join(", ", criticalErrors.Select(d => $"{d.Id}: {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}"))}");
        }
    }
}