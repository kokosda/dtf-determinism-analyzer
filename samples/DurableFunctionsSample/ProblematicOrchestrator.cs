using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
        // ❌ DFA0001: Using DateTime.Now in orchestrator (non-deterministic!)
        DateTime startTime = DateTime.Now;

        // ❌ DFA0001: Using DateTime.UtcNow in orchestrator (non-deterministic!)
        DateTime utcTime = DateTime.UtcNow;

        // ❌ DFA0002: Using Guid.NewGuid() in orchestrator (non-deterministic!)
        var correlationId = Guid.NewGuid();

        // ❌ DFA0003: Random without deterministic seed
        var random = new Random();
        int randomValue = random.Next(1, 100);

        // ❌ DFA0004: Direct I/O operation in orchestrator  
        string fileContent = File.ReadAllText("config.txt");

        // ❌ DFA0004: File I/O operations in orchestrator (async version)
        string fileContentAsync = await File.ReadAllTextAsync("config.txt");

        // ❌ DFA0005: Environment variable access
        string? envVar = Environment.GetEnvironmentVariable("TEMP");

        // ❌ DFA0006: Static state access
        int currentCounter = _staticCounter;
        _staticCounter++;

        // ❌ DFA0007: Using Thread.Sleep in orchestrator (blocking operation!)
        Thread.Sleep(1000);

        // ❌ DFA0008: Task.Delay (non-durable async operation)
        await Task.Delay(2000);

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

    /// <summary>
    /// Demonstrates DFA0010 violations: direct binding usage in orchestrator.
    /// ❌ This orchestrator incorrectly uses Azure Functions bindings directly,
    /// which violates determinism rules.
    /// </summary>
    [Function(nameof(RunWithDirectBindingsViolation))]
    public async Task<string> RunWithDirectBindingsViolation(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        // ❌ DFA0010: Direct BlobTrigger binding in orchestrator parameter
        [BlobTrigger("container/{name}")] Stream blob,
        // ❌ DFA0010: Direct QueueTrigger binding in orchestrator parameter  
        [QueueTrigger("processing-queue")] string queueMessage,
        // ❌ DFA0010: Direct Table binding in orchestrator parameter
        [Table("DataTable")] IAsyncCollector<dynamic> tableOutput)
    {
        // ❌ DFA0010: Direct access to blob binding in orchestrator
        using var reader = new StreamReader(blob);
        string blobContent = await reader.ReadToEndAsync();

        // ❌ DFA0010: Direct access to queue message in orchestrator
        string processedMessage = $"Processing: {queueMessage}";

        // ❌ DFA0010: Direct table output binding usage in orchestrator
        await tableOutput.AddAsync(new { 
            Message = processedMessage,
            BlobContent = blobContent.Substring(0, Math.Min(100, blobContent.Length)),
            Timestamp = context.CurrentUtcDateTime 
        });

        return $"Processed queue message: {queueMessage}";
    }

    /// <summary>
    /// More DFA0010 violations with different binding types.
    /// </summary>
    [Function(nameof(RunWithMoreBindingViolations))]
    public async Task<string> RunWithMoreBindingViolations(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        // ❌ DFA0010: Direct HttpTrigger in orchestrator (should never happen in practice)
        [HttpTrigger(AuthorizationLevel.Function)] HttpRequestData req,
        // ❌ DFA0010: Direct ServiceBus binding in orchestrator
        [ServiceBusTrigger("topic", "subscription")] string serviceBusMessage)
    {
        // ❌ DFA0010: Direct HTTP request handling in orchestrator
        string requestBody = await req.ReadAsStringAsync() ?? "";

        // ❌ DFA0010: Direct Service Bus message processing in orchestrator
        string result = $"HTTP: {requestBody}, ServiceBus: {serviceBusMessage}";

        return result;
    }
}
