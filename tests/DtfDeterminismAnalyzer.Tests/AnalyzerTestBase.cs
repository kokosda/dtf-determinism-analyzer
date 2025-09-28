using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using VerifyAnalyzer = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Base class for analyzer tests that provides proper Azure Functions assembly references.
    /// This class configures the Microsoft.CodeAnalysis.Testing framework with the necessary
    /// assembly references to compile Azure Functions code without CS0234/CS0246 errors.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer type to test</typeparam>
    public class AnalyzerTestBase<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <summary>
        /// Gets the reference assemblies configured with Azure Functions packages.
        /// Uses .NET 8.0 target framework for compatibility with analyzer projects.
        /// </summary>
        protected static readonly ReferenceAssemblies TestReferences = 
            AzureFunctionsReferences.CreateFullReferenceAssemblies();

        /// <summary>
        /// Gets the reference assemblies for use in tests.
        /// </summary>
        public ReferenceAssemblies TestReferenceAssemblies => TestReferences;

        /// <summary>
        /// Gets the list of reference assemblies as MetadataReference objects.
        /// </summary>
        /// <returns>Collection of MetadataReference objects</returns>
        public IEnumerable<MetadataReference> GetReferenceAssemblies()
        {
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                references: TestReferences.ResolveAsync(LanguageNames.CSharp, default).Result);
            
            return compilation.References;
        }

        /// <summary>
        /// Creates MetadataReference objects for Azure Functions assemblies using typeof().Assembly.Location pattern.
        /// This method provides an alternative way to reference Azure Functions types directly from loaded assemblies.
        /// </summary>
        /// <returns>Collection of MetadataReference objects for Azure Functions assemblies</returns>
        protected IEnumerable<MetadataReference> CreateAzureFunctionsMetadataReferences()
        {
            var references = new List<MetadataReference>();

            // Add core Azure Functions assemblies if available
            try
            {
                // Microsoft.Azure.WebJobs - Core WebJobs types
                var webJobsAssembly = typeof(Microsoft.Azure.WebJobs.FunctionNameAttribute).Assembly;
                references.Add(MetadataReference.CreateFromFile(webJobsAssembly.Location));
                
                // Microsoft.Azure.WebJobs.Extensions.DurableTask - DurableTask extensions
                var durableTaskAssembly = typeof(Microsoft.Azure.WebJobs.Extensions.DurableTask.IDurableOrchestrationContext).Assembly;
                references.Add(MetadataReference.CreateFromFile(durableTaskAssembly.Location));
                
                // Microsoft.Extensions.Logging.Abstractions - ILogger interface
                var loggingAssembly = typeof(Microsoft.Extensions.Logging.ILogger).Assembly;
                references.Add(MetadataReference.CreateFromFile(loggingAssembly.Location));
            }
            catch (System.IO.FileNotFoundException)
            {
                // If assembly files are not found, fall back to package-based references
                // This can happen in some test environments or when assemblies are loaded differently
            }
            catch (System.TypeLoadException)
            {
                // If types are not available, fall back to package-based references
                // This can happen when the referenced packages are not available at runtime
            }

            // Add ASP.NET Core assemblies for HTTP triggers if available
            try
            {
                // Microsoft.AspNetCore.Mvc.Abstractions - IActionResult interface
                var mvcAssembly = typeof(Microsoft.AspNetCore.Mvc.IActionResult).Assembly;
                references.Add(MetadataReference.CreateFromFile(mvcAssembly.Location));
                
                // Microsoft.AspNetCore.Http.Abstractions - HttpRequest interface
                var httpAssembly = typeof(Microsoft.AspNetCore.Http.HttpRequest).Assembly;
                references.Add(MetadataReference.CreateFromFile(httpAssembly.Location));
            }
            catch (System.IO.FileNotFoundException)
            {
                // ASP.NET Core assemblies might not be available in all test scenarios
            }
            catch (System.TypeLoadException)
            {
                // ASP.NET Core types might not be available in all test scenarios
            }

            return references;
        }

        /// <summary>
        /// Creates a compilation with both package-based and assembly-based references.
        /// This provides maximum compatibility across different test environments.
        /// Uses proper parse options and compilation options for consistent analyzer testing.
        /// </summary>
        /// <param name="source">The C# source code to compile</param>
        /// <returns>A compilation with comprehensive Azure Functions type support</returns>
        public async Task<Compilation> CreateTestCompilationWithAssemblyReferences(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, GetParseOptions());
            
            // Get package-based references
            var packageReferences = await TestReferences.ResolveAsync(LanguageNames.CSharp, default);
            
            // Get assembly-based references
            var assemblyReferences = CreateAzureFunctionsMetadataReferences();
            
            // Combine both reference types for maximum compatibility
            var allReferences = packageReferences.Concat(assemblyReferences).Distinct();
            
            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { syntaxTree },
                allReferences,
                GetCompilationOptions());
            
            return compilation;
        }

        /// <summary>
        /// Gets the C# parse options for test compilations.
        /// Configured for C# 12.0 with latest language features to match analyzer target environment.
        /// </summary>
        /// <returns>CSharpParseOptions for C# 12.0</returns>
        public CSharpParseOptions GetParseOptions()
        {
            return new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp12,
                kind: SourceCodeKind.Regular,
                documentationMode: DocumentationMode.Parse
            );
        }

        /// <summary>
        /// Gets the C# compilation options for test compilations.
        /// Configured to match the target environment of analyzer projects with proper deterministic builds.
        /// </summary>
        /// <returns>CSharpCompilationOptions optimized for analyzer testing</returns>
        public CSharpCompilationOptions GetCompilationOptions()
        {
            return new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: false,
                nullableContextOptions: NullableContextOptions.Enable,
                deterministic: true,
                concurrentBuild: true,
                checkOverflow: false,
                platform: Platform.AnyCpu,
                reportSuppressedDiagnostics: true,
                warningLevel: 4,
                specificDiagnosticOptions: GetSpecificDiagnosticOptions()
            );
        }

        /// <summary>
        /// Gets specific diagnostic options to match the test project configuration.
        /// Configures warning levels and suppressions for optimal analyzer testing.
        /// </summary>
        /// <returns>Dictionary of diagnostic ID to report action mappings</returns>
        private static ImmutableDictionary<string, ReportDiagnostic> GetSpecificDiagnosticOptions()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>();
            
            // Suppress nullable warnings that are expected in test scenarios
            builder["CS8600"] = ReportDiagnostic.Suppress; // Converting null literal or possible null value
            builder["CS8601"] = ReportDiagnostic.Suppress; // Possible null reference assignment
            builder["CS8602"] = ReportDiagnostic.Suppress; // Dereference of a possibly null reference
            builder["CS8603"] = ReportDiagnostic.Suppress; // Possible null reference return
            builder["CS8604"] = ReportDiagnostic.Suppress; // Possible null reference argument
            
            // Ensure these critical compilation errors are always reported
            builder["CS0234"] = ReportDiagnostic.Error; // Type or namespace name does not exist
            builder["CS0246"] = ReportDiagnostic.Error; // Type or namespace name could not be found
            builder["CS0103"] = ReportDiagnostic.Error; // The name does not exist in the current context
            
            return builder.ToImmutable();
        }

        /// <summary>
        /// Gets the analyzer options for test execution.
        /// Provides configuration for analyzer behavior during testing.
        /// </summary>
        /// <returns>AnalyzerOptions configured for testing</returns>
        public AnalyzerOptions GetAnalyzerOptions()
        {
            return new AnalyzerOptions(
                additionalFiles: ImmutableArray<AdditionalText>.Empty
            );
        }

        /// <summary>
        /// Creates a properly configured solution transformation for analyzer tests.
        /// This transformation applies proper parse options and compilation options to test projects.
        /// </summary>
        /// <returns>A solution transformation function</returns>
        public Func<Solution, ProjectId, Solution> CreateSolutionTransform()
        {
            return (solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                if (project == null) return solution;
                
                var parseOptions = GetParseOptions();
                var compilationOptions = GetCompilationOptions();
                
                return solution
                    .WithProjectParseOptions(projectId, parseOptions)
                    .WithProjectCompilationOptions(projectId, compilationOptions);
            };
        }

        /// <summary>
        /// Gets the standard reference assemblies configured for Azure Functions testing.
        /// This method provides easy access to the configured reference assemblies.
        /// </summary>
        /// <returns>ReferenceAssemblies configured with Azure Functions packages</returns>
        public ReferenceAssemblies GetStandardReferenceAssemblies()
        {
            return TestReferences;
        }

        /// <summary>
        /// Creates a test compilation with the provided source code.
        /// Uses proper parse options and compilation options for consistent analyzer testing.
        /// </summary>
        /// <param name="source">The C# source code to compile</param>
        /// <returns>A compilation with Azure Functions types resolved</returns>
        public async Task<Compilation> CreateTestCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, GetParseOptions());
            var references = await TestReferences.ResolveAsync(LanguageNames.CSharp, default);
            
            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                new[] { syntaxTree },
                references,
                GetCompilationOptions());
            
            return compilation;
        }

        /// <summary>
        /// Runs the analyzer test with the provided source code.
        /// Uses properly configured compilation and analyzer options for consistent testing.
        /// </summary>
        /// <param name="source">The C# source code to analyze</param>
        /// <returns>Test result with compilation and analyzer diagnostics</returns>
        public async Task<AnalyzerTestResult> RunAnalyzerTest(string source)
        {
            var compilation = await CreateTestCompilation(source);
            var compilationDiagnostics = compilation.GetDiagnostics().ToList();
            
            // Run the analyzer
            var analyzer = new TAnalyzer();
            var analyzerDiagnostics = new List<Diagnostic>();
            
            if (compilationDiagnostics.All(d => d.Severity != DiagnosticSeverity.Error))
            {
                var compilationWithAnalyzers = compilation.WithAnalyzers(
                    ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
                    GetAnalyzerOptions());
                
                var analyzerResults = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
                analyzerDiagnostics.AddRange(analyzerResults);
            }

            return new AnalyzerTestResult
            {
                CompilationDiagnostics = compilationDiagnostics,
                AnalyzerDiagnostics = analyzerDiagnostics,
                CompilationSucceeded = compilationDiagnostics.All(d => d.Severity != DiagnosticSeverity.Error)
            };
        }
    }

    /// <summary>
    /// Result of running an analyzer test, containing both compilation and analyzer diagnostics.
    /// </summary>
    public class AnalyzerTestResult
    {
        /// <summary>
        /// Gets or sets the diagnostics from the compilation process.
        /// </summary>
        public List<Diagnostic> CompilationDiagnostics { get; set; } = new List<Diagnostic>();

        /// <summary>
        /// Gets or sets the diagnostics produced by the analyzer.
        /// </summary>
        public List<Diagnostic> AnalyzerDiagnostics { get; set; } = new List<Diagnostic>();

        /// <summary>
        /// Gets or sets whether the compilation succeeded without errors.
        /// </summary>
        public bool CompilationSucceeded { get; set; }
    }
}