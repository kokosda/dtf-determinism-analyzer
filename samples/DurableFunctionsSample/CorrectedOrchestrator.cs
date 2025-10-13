using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsSample;

/// <summary>
/// Demonstrates the corrected versions of the problematic patterns.
/// This orchestrator follows DTF determinism rules and will not trigger analyzer warnings.
/// </summary>
public class CorrectedOrchestrator
{
    private readonly ILogger<CorrectedOrchestrator> _logger;

    public CorrectedOrchestrator(ILogger<CorrectedOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// A properly deterministic orchestrator function.
    /// This version fixes all the issues from ProblematicOrchestrator.
    /// ✅ All analyzer rules are satisfied.
    /// </summary>
    [Function(nameof(RunCorrectedOrchestrator))]
    public async Task<string> RunCorrectedOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // ✅ Use context.CurrentUtcDateTime instead of DateTime.Now/UtcNow
        DateTime startTime = context.CurrentUtcDateTime.ToLocalTime();
        DateTime utcTime = context.CurrentUtcDateTime;

        // ✅ Use context.NewGuid() instead of Guid.NewGuid()
        Guid correlationId = context.NewGuid();

        // ✅ Use durable timer instead of Thread.Sleep
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);

        // Activities can perform any operations - no restrictions
        string result = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessDataActivity),
            new { startTime, utcTime, correlationId });

        return result;
    }

    /// <summary>
    /// Demonstrates correct patterns for more complex scenarios.
    /// </summary>
    [Function(nameof(RunCorrectedComplexPatterns))]
    public async Task<string> RunCorrectedComplexPatterns([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // ✅ Use deterministic seed for Random based on orchestration context
        int seed = context.CurrentUtcDateTime.GetHashCode() ^ context.InstanceId.GetHashCode();
        var random = new Random(seed);
        int randomValue = random.Next(1, 100);

        // ✅ Use durable timer instead of Task.Delay
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(2), CancellationToken.None);

        // ✅ Move I/O operations to activities
        string fileContent = await context.CallActivityAsync<string>(
            nameof(Activities.ReadConfigFileActivity),
            "config.txt");

        // ✅ Move environment variable access to activities
        string envVar = await context.CallActivityAsync<string>(
            nameof(Activities.GetEnvironmentVariableActivity),
            "TEMP");

        string result = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessRandomDataActivity),
            new { randomValue, envVar, fileContent });

        return result;
    }

    /// <summary>
    /// Demonstrates proper error handling and retry patterns.
    /// </summary>
    [Function(nameof(RunWithRetryPolicy))]
    public async Task<string> RunWithRetryPolicy([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5)));

        try
        {
            // Activity calls with retry policy - this is deterministic
            string result = await context.CallActivityAsync<string>(
                nameof(Activities.RiskyOperationActivity),
                options: retryOptions);

            return result;
        }
        catch (TaskFailedException)
        {
            // Handle failures deterministically
            return "Operation failed after retries";
        }
    }

    /// <summary>
    /// Demonstrates the CORRECT approach for DFA0010: delegate binding operations to activities.
    /// ✅ This orchestrator properly delegates all binding access to activities,
    /// maintaining determinism while still processing external data.
    /// </summary>
    [Function(nameof(RunWithProperBindingDelegation))]
    public async Task<string> RunWithProperBindingDelegation([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // ✅ Pass blob processing to activity - no direct binding in orchestrator
        string blobContent = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessBlobActivity),
            new { containerName = "container", blobName = "data.txt" });

        // ✅ Pass queue processing to activity - no direct binding in orchestrator  
        string queueResult = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessQueueMessageActivity),
            new { queueName = "processing-queue", message = "sample-message" });

        // ✅ Pass table operations to activity - no direct binding in orchestrator
        await context.CallActivityAsync(
            nameof(Activities.SaveToTableActivity),
            new { 
                tableName = "DataTable",
                data = new { 
                    BlobContent = blobContent.Substring(0, Math.Min(100, blobContent.Length)),
                    QueueResult = queueResult,
                    Timestamp = context.CurrentUtcDateTime 
                }
            });

        return $"Successfully processed blob and queue data: {queueResult}";
    }

    /// <summary>
    /// More examples of proper binding delegation.
    /// ✅ All external service access is delegated to activities.
    /// </summary>
    [Function(nameof(RunWithCompleteBindingDelegation))]
    public async Task<string> RunWithCompleteBindingDelegation([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // ✅ HTTP processing delegated to activity
        string httpResult = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessHttpRequestActivity),
            new { url = "https://api.example.com/data", method = "GET" });

        // ✅ Service Bus processing delegated to activity
        string serviceBusResult = await context.CallActivityAsync<string>(
            nameof(Activities.ProcessServiceBusMessageActivity),
            new { topic = "notifications", subscription = "processors", message = httpResult });

        // Orchestrator only coordinates - all I/O through activities
        return $"Processed HTTP and Service Bus: {serviceBusResult}";
    }
}
