using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsSample;

/// <summary>
/// Demonstrates problematic patterns that the DTF Determinism Analyzer will flag.
/// This orchestrator intentionally contains code that violates determinism rules
/// to show how the analyzer detects and helps fix these issues.
/// </summary>
public class ProblematicOrchestrator
{
    private readonly ILogger<ProblematicOrchestrator> _logger;

    // ❌ DFA0006: Static mutable state access
    private static int _staticCounter;
    private static readonly object _lockObject = new object();

    public ProblematicOrchestrator(ILogger<ProblematicOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// An orchestrator function with multiple determinism violations.
    /// The DTF Determinism Analyzer will flag these issues:
    /// - DFA0001: DateTime.Now usage
    /// - DFA0002: Guid.NewGuid() usage  
    /// - DFA0007: Thread.Sleep usage
    /// </summary>
    [Function(nameof(RunProblematicOrchestrator))]
    public async Task<string> RunProblematicOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // ❌ DFA0001: Using DateTime.Now in orchestrator (non-deterministic!)
        DateTime startTime = DateTime.Now;

        // ❌ DFA0001: Using DateTime.UtcNow in orchestrator (non-deterministic!)
        DateTime utcTime = DateTime.UtcNow;

        // ❌ DFA0002: Using Guid.NewGuid() in orchestrator (non-deterministic!)
        var correlationId = Guid.NewGuid();

        // ❌ DFA0007: Using Thread.Sleep in orchestrator (blocking operation!)
        Thread.Sleep(1000);

        // This is OK - calling activities for actual work
        string result = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessDataActivity),
            new { startTime, utcTime, correlationId });

        return result;
    }

    /// <summary>
    /// Another problematic orchestrator showing more violations.
    /// </summary>
    [Function(nameof(RunMoreProblematicPatterns))]
    public async Task<string> RunMoreProblematicPatterns([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // ❌ DFA0003: Random without deterministic seed
        var random = new Random();
        int randomValue = random.Next(1, 100);

        // ❌ DFA0008: Task.Delay (non-durable async operation)
        await Task.Delay(2000);

        // ❌ DFA0004: Direct I/O operation in orchestrator  
        string fileContent = File.ReadAllText("config.txt");
        
        // ❌ DFA0004: File I/O operations in orchestrator (async version)
        string fileContentAsync = await File.ReadAllTextAsync("config.txt");

        // ❌ DFA0005: Environment variable access
        string? envVar = Environment.GetEnvironmentVariable("TEMP");

        // ❌ DFA0006: Static state access
        int currentCounter = _staticCounter;
        _staticCounter++;

        // ❌ DFA0009: Threading API usage 
        lock (_lockObject)
        {
            Thread.Sleep(100); // Also triggers DFA0007
        }

        string result = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessRandomDataActivity),
            new { randomValue, envVar, fileContent, currentCounter });

        return result;
    }
}
