using Microsoft.CodeAnalysis;

namespace DtfDeterminismAnalyzer
{
    /// <summary>
    /// Diagnostic descriptors for DTF (Durable Task Framework) determinism analyzer rules.
    /// Each descriptor defines metadata for Roslyn analyzer diagnostics including severity, 
    /// category, message format, and help links to Microsoft Learn documentation.
    /// </summary>
    public static class DiagnosticDescriptors
    {
        /// <summary>
        /// Base help URI for DTF code constraints documentation.
        /// </summary>
        public const string HelpBaseUri = "https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-code-constraints";

        /// <summary>
        /// Diagnostic category for all DTF determinism rules.
        /// </summary>
        public const string Category = "DTF.Determinism";

        /// <summary>
        /// DFA0001: Do not use DateTime.Now/UtcNow/Stopwatch in orchestrators.
        /// Non-deterministic time APIs can cause replay inconsistencies.
        /// </summary>
        public static readonly DiagnosticDescriptor TimeApiRule = new(
            id: "DFA0001",
            title: "Do not use DateTime.Now/UtcNow/Stopwatch in orchestrators",
            messageFormat: "Non-deterministic time API used in orchestrator",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "DateTime.Now, DateTime.UtcNow, and Stopwatch APIs return different values during replay, " +
                        "causing non-deterministic behavior in orchestrator functions. Use IDurableOrchestrationContext.CurrentUtcDateTime instead.",
            helpLinkUri: $"{HelpBaseUri}#dates-and-times");

        /// <summary>
        /// DFA0002: Do not use Guid.NewGuid() in orchestrators.
        /// Non-deterministic GUID generation can cause replay inconsistencies.
        /// </summary>
        public static readonly DiagnosticDescriptor GuidRule = new(
            id: "DFA0002",
            title: "Do not use Guid.NewGuid() in orchestrators",
            messageFormat: "Non-deterministic GUID generated in orchestrator",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Guid.NewGuid() generates different values during replay, causing non-deterministic behavior " +
                        "in orchestrator functions. Use IDurableOrchestrationContext.NewGuid() for replay-safe GUID generation.",
            helpLinkUri: $"{HelpBaseUri}#guids");

        /// <summary>
        /// DFA0003: Do not use Random without fixed seed in orchestrators.
        /// Non-deterministic random number generation can cause replay inconsistencies.
        /// </summary>
        public static readonly DiagnosticDescriptor RandomRule = new(
            id: "DFA0003",
            title: "Do not use Random without fixed seed",
            messageFormat: "Non-deterministic random used in orchestrator",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Random number generators without deterministic seeding produce different values during replay, " +
                        "causing non-deterministic behavior. Move random number generation to activity functions or use deterministic seeding.",
            helpLinkUri: $"{HelpBaseUri}#random-numbers");

        /// <summary>
        /// DFA0004: Do not perform I/O or network calls in orchestrators.
        /// Direct I/O operations are non-deterministic and break replay safety.
        /// </summary>
        public static readonly DiagnosticDescriptor IoRule = new(
            id: "DFA0004",
            title: "Do not perform I/O or network calls in orchestrators",
            messageFormat: "Outbound I/O detected in orchestrator",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "File I/O, HTTP calls, and other network operations are non-deterministic and cannot be replayed safely. " +
                        "Use activity functions for I/O operations or IDurableOrchestrationContext.CallHttpAsync for HTTP calls.",
            helpLinkUri: $"{HelpBaseUri}#io-operations");

        /// <summary>
        /// DFA0005: Do not read environment variables in orchestrators.
        /// Environment variables can change between executions, causing non-deterministic behavior.
        /// </summary>
        public static readonly DiagnosticDescriptor EnvironmentRule = new(
            id: "DFA0005",
            title: "Do not read environment variables in orchestrators",
            messageFormat: "Environment variable read is non-deterministic",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Environment variables may change between orchestrator executions and replay, causing non-deterministic behavior. " +
                        "Pass environment values via orchestrator input or retrieve them in activity functions.",
            helpLinkUri: $"{HelpBaseUri}#environment-variables");

        /// <summary>
        /// DFA0006: Do not use static mutable state in orchestrators.
        /// Static state may change between executions and replay, causing inconsistencies.
        /// </summary>
        public static readonly DiagnosticDescriptor StaticStateRule = new(
            id: "DFA0006",
            title: "Do not use static mutable state in orchestrators",
            messageFormat: "Static state may change across replays",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reading or writing static mutable state in orchestrators can cause non-deterministic behavior during replay " +
                        "as the state may differ between executions. Use constants, pass state via input, or manage state in activity functions.",
            helpLinkUri: $"{HelpBaseUri}#static-state");

        /// <summary>
        /// DFA0007: Do not block threads in orchestrators.
        /// Thread-blocking operations prevent proper orchestrator scheduling and replay.
        /// </summary>
        public static readonly DiagnosticDescriptor ThreadBlockingRule = new(
            id: "DFA0007",
            title: "Do not block threads in orchestrators",
            messageFormat: "Thread-blocking call detected",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Thread-blocking operations like Thread.Sleep, Task.Wait, and Task.Result prevent proper orchestrator " +
                        "scheduling and can cause deadlocks. Use IDurableOrchestrationContext.CreateTimer for delays and await tasks properly.",
            helpLinkUri: $"{HelpBaseUri}#thread-blocking");

        /// <summary>
        /// DFA0008: Do not start non-durable async operations in orchestrators.
        /// Non-durable async operations cannot be replayed safely.
        /// </summary>
        public static readonly DiagnosticDescriptor NonDurableAsyncRule = new(
            id: "DFA0008",
            title: "Do not start non-durable async operations",
            messageFormat: "Non-durable async operation detected",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Non-durable async operations like Task.Run, Task.Delay, and custom async methods cannot be replayed safely " +
                        "and may cause non-deterministic behavior. Use durable APIs or move async work to activity functions.",
            helpLinkUri: $"{HelpBaseUri}#async-operations");

        /// <summary>
        /// DFA0009: Avoid .NET threading APIs like ConfigureAwait(false) in orchestrators.
        /// Threading APIs can interfere with the orchestrator's execution context and replay model.
        /// </summary>
        public static readonly DiagnosticDescriptor ThreadingApisRule = new(
            id: "DFA0009",
            title: "Avoid .NET threading APIs like ConfigureAwait(false)",
            messageFormat: "Threading API usage detected",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Threading APIs like ConfigureAwait(false), custom TaskSchedulers, and synchronization primitives " +
                        "can interfere with the orchestrator's execution context and replay model. Let orchestrator code run on its natural context.",
            helpLinkUri: $"{HelpBaseUri}#threading-apis");

        /// <summary>
        /// DFA0010: Do not use bindings inside orchestrators.
        /// Direct Azure Functions bindings are not supported in orchestrator functions.
        /// </summary>
        public static readonly DiagnosticDescriptor BindingsRule = new(
            id: "DFA0010",
            title: "Do not use bindings inside orchestrators",
            messageFormat: "Direct binding usage detected in orchestrator",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Direct Azure Functions bindings like [Blob], [Queue], [Table] are not supported in orchestrator functions " +
                        "and can cause runtime errors. Use bindings in client functions or activity functions instead.",
            helpLinkUri: $"{HelpBaseUri}#bindings");

        /// <summary>
        /// Gets all diagnostic descriptors defined by this analyzer.
        /// Used by analyzer infrastructure to report supported diagnostics.
        /// </summary>
        /// <returns>Array of all diagnostic descriptors.</returns>
        public static DiagnosticDescriptor[] GetAllDescriptors()
        {
            return
            [
                TimeApiRule,
                GuidRule,
                RandomRule,
                IoRule,
                EnvironmentRule,
                StaticStateRule,
                ThreadBlockingRule,
                NonDurableAsyncRule,
                ThreadingApisRule,
                BindingsRule
            ];
        }

        /// <summary>
        /// Gets a diagnostic descriptor by its rule ID.
        /// </summary>
        /// <param name="ruleId">The diagnostic rule ID (e.g., "DFA0001").</param>
        /// <returns>The matching diagnostic descriptor, or null if not found.</returns>
        public static DiagnosticDescriptor? GetDescriptorById(string ruleId)
        {
            return ruleId switch
            {
                "DFA0001" => TimeApiRule,
                "DFA0002" => GuidRule,
                "DFA0003" => RandomRule,
                "DFA0004" => IoRule,
                "DFA0005" => EnvironmentRule,
                "DFA0006" => StaticStateRule,
                "DFA0007" => ThreadBlockingRule,
                "DFA0008" => NonDurableAsyncRule,
                "DFA0009" => ThreadingApisRule,
                "DFA0010" => BindingsRule,
                _ => null
            };
        }
    }
}
