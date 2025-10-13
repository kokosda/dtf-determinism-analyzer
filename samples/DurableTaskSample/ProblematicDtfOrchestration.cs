using Microsoft.DurableTask;

namespace DurableTaskSample;

/// <summary>
/// Demonstrates problematic patterns in core Durable Task Framework orchestrations.
/// This class contains orchestrator-like methods that violate determinism rules
/// to show how the analyzer detects issues in DTF contexts (not just Azure Functions).
/// 
/// Note: This sample focuses on demonstrating analyzer behavior rather than runtime execution.
/// The analyzer detects patterns based on method signatures and context parameters.
/// </summary>
public class ProblematicDtfOrchestration
{
    // ❌ DFA0006: Static mutable state access
    private static int _staticCounter;
    private static readonly object _lockObject = new object();

    /// <summary>
    /// Simulated orchestrator method with DTF context parameter.
    /// The analyzer detects this as an orchestrator based on the TaskOrchestrationContext parameter.
    /// </summary>
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // ❌ DFA0001: Using DateTime.Now in orchestrator (non-deterministic!)
        DateTime startTime = DateTime.Now;

        // ❌ DFA0001: Using DateTime.UtcNow in orchestrator (non-deterministic!)
        DateTime utcTime = DateTime.UtcNow;

        // ❌ DFA0002: Using Guid.NewGuid() in orchestrator (non-deterministic!)
        var correlationId = Guid.NewGuid();

        // ❌ DFA0003: Using non-deterministic Random
        var random = new Random();
        int randomValue = random.Next(1, 100);

        // ❌ DFA0004: Direct I/O operation in orchestrator  
        string fileContent = File.ReadAllText("config.txt");

        // ❌ DFA0004: File I/O operations in orchestrator (async version)
        string fileContentAsync = await File.ReadAllTextAsync("config.txt");

        // ❌ DFA0005: Environment variable access in orchestrator
        string? envVar = Environment.GetEnvironmentVariable("TEMP");

        // ❌ DFA0006: Static state access and modification
        int currentCount = _staticCounter;

        // ❌ DFA0007: Using Thread.Sleep in orchestrator (blocking operation!)
        Thread.Sleep(1000);

        // ❌ DFA0008: Non-durable async operations
        await Task.Delay(500);

        // ❌ DFA0008: Non-durable async operation
        string httpResult = await new HttpClient().GetStringAsync("https://api.example.com/data");

        // ❌ DFA0009: Using lock (threading API)
        lock (_lockObject)
        {
            _staticCounter++;
        }

        // This is OK - simulated activity call (real implementation would use context.CallActivityAsync)
        string result = $"Processed: {input} with violations - Start: {startTime}, ID: {correlationId}, Random: {randomValue}";

        return result;
    }

    /// <summary>
    /// Demonstrates DFA0010 violations in DTF context: direct binding usage.
    /// Even in core DTF (outside Azure Functions), binding attributes are problematic in orchestrators.
    /// </summary>
    public static async Task<string> OrchestrationWithBindingViolations(
        TaskOrchestrationContext context,
        // ❌ DFA0010: Direct binding attributes in orchestrator parameters
        [BlobTrigger("container/{name}")] Stream blobData,
        [QueueTrigger("queue")] string queueMessage)
    {
        // ❌ DFA0010: Direct blob access in orchestrator
        using var reader = new StreamReader(blobData);
        string content = await reader.ReadToEndAsync();

        // ❌ DFA0010: Direct queue message processing in orchestrator
        string processed = $"DTF Processing: {queueMessage} with blob length {content.Length}";

        return processed;
    }

    /// <summary>
    /// More DFA0010 violations with various binding types in DTF context.
    /// </summary>
    public static string OrchestrationWithMoreBindingViolations(
        TaskOrchestrationContext context,
        // ❌ DFA0010: Table binding in orchestrator
        [Table("MyTable")] IAsyncCollector<dynamic> table,
        // ❌ DFA0010: ServiceBus binding in orchestrator  
        [ServiceBusTrigger("topic", "sub")] string message)
    {
        // ❌ DFA0010: Direct binding operations in orchestrator
        table.AddAsync(new { Data = message, Timestamp = DateTime.UtcNow });
        
        return $"DTF processed ServiceBus message: {message}";
    }
}
