using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace DtfDeterminismAnalyzer.Tests
{
    /// <summary>
    /// Helper class that provides Azure Functions assembly references for the test framework.
    /// This class centralizes the configuration of package identities and versions used
    /// across all analyzer tests to ensure consistency.
    /// </summary>
    public static class AzureFunctionsReferences
    {
        /// <summary>
        /// Gets the core Azure Functions and WebJobs packages required for test compilation.
        /// These packages provide the fundamental types like FunctionName, OrchestrationTrigger, etc.
        /// </summary>
        public static ImmutableArray<PackageIdentity> CorePackages { get; } = ImmutableArray.Create(
            // Core Azure Functions and WebJobs packages
            new PackageIdentity("Microsoft.Azure.WebJobs", "3.0.37"),
            new PackageIdentity("Microsoft.Azure.WebJobs.Extensions.DurableTask", "2.13.0"),
            new PackageIdentity("DurableTask.Core", "2.0.0.6")
        );

        /// <summary>
        /// Gets the HTTP trigger support packages required for HTTP-based Azure Functions.
        /// These packages provide types like HttpTrigger, IActionResult, HttpRequest, etc.
        /// </summary>
        public static ImmutableArray<PackageIdentity> HttpPackages { get; } = ImmutableArray.Create(
            // HTTP trigger support
            new PackageIdentity("Microsoft.Azure.WebJobs.Extensions.Http", "3.2.0"),
            new PackageIdentity("Microsoft.AspNetCore.Mvc.Abstractions", "2.2.0"),
            new PackageIdentity("Microsoft.AspNetCore.Http.Abstractions", "2.2.0")
        );

        /// <summary>
        /// Gets the logging support packages required for ILogger functionality.
        /// These packages provide the Microsoft.Extensions.Logging types.
        /// </summary>
        public static ImmutableArray<PackageIdentity> LoggingPackages { get; } = ImmutableArray.Create(
            // Logging support  
            new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", "8.0.0")
        );

        /// <summary>
        /// Gets the utility packages for JSON serialization and other common functionality.
        /// These packages provide additional support for complex Azure Functions scenarios.
        /// </summary>
        public static ImmutableArray<PackageIdentity> UtilityPackages { get; } = ImmutableArray.Create(
            // JSON support for complex scenarios
            new PackageIdentity("Newtonsoft.Json", "13.0.3")
        );

        /// <summary>
        /// Gets all Azure Functions packages combined into a single collection.
        /// This is the primary collection used by AnalyzerTestBase for comprehensive support.
        /// </summary>
        public static ImmutableArray<PackageIdentity> AllPackages { get; } = 
            CorePackages
                .AddRange(HttpPackages)
                .AddRange(LoggingPackages)
                .AddRange(UtilityPackages);

        /// <summary>
        /// Gets a minimal set of packages for basic Azure Functions support.
        /// Use this for tests that only need core orchestration functionality without HTTP or complex features.
        /// </summary>
        public static ImmutableArray<PackageIdentity> MinimalPackages { get; } = 
            CorePackages.AddRange(LoggingPackages);

        /// <summary>
        /// Gets packages for HTTP-triggered Azure Functions.
        /// Use this for tests that need both core functionality and HTTP trigger support.
        /// </summary>
        public static ImmutableArray<PackageIdentity> HttpFunctionPackages { get; } =
            CorePackages
                .AddRange(HttpPackages)
                .AddRange(LoggingPackages);

        /// <summary>
        /// Creates a ReferenceAssemblies configuration with the specified Azure Functions packages.
        /// </summary>
        /// <param name="packages">The package identities to include in the reference assemblies</param>
        /// <returns>A ReferenceAssemblies instance configured with .NET 8.0 and the specified packages</returns>
        public static ReferenceAssemblies CreateReferenceAssemblies(ImmutableArray<PackageIdentity> packages)
        {
            return ReferenceAssemblies.Net.Net80.AddPackages(packages);
        }

        /// <summary>
        /// Creates a ReferenceAssemblies configuration with all Azure Functions packages.
        /// This is equivalent to calling CreateReferenceAssemblies(AllPackages).
        /// </summary>
        /// <returns>A ReferenceAssemblies instance configured with all Azure Functions support</returns>
        public static ReferenceAssemblies CreateFullReferenceAssemblies()
        {
            return CreateReferenceAssemblies(AllPackages);
        }

        /// <summary>
        /// Creates a ReferenceAssemblies configuration with minimal Azure Functions packages.
        /// This is equivalent to calling CreateReferenceAssemblies(MinimalPackages).
        /// </summary>
        /// <returns>A ReferenceAssemblies instance configured with minimal Azure Functions support</returns>
        public static ReferenceAssemblies CreateMinimalReferenceAssemblies()
        {
            return CreateReferenceAssemblies(MinimalPackages);
        }
    }
}