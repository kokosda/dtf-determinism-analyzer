using Microsoft.DurableTask;

namespace DurableTaskSample;

/// <summary>
/// Demonstrates the corrected version following DTF determinism rules.
/// This orchestrator uses proper patterns and will not trigger analyzer warnings.
/// </summary>
public class CorrectedDtfOrchestration
{
    /// <summary>
    /// Properly deterministic orchestrator method.
    /// Uses TaskOrchestrationContext parameter which makes analyzer treat it as an orchestrator.
    /// </summary>
    public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // ✅ Use context.CurrentUtcDateTime instead of DateTime.Now/UtcNow
        DateTime startTime = context.CurrentUtcDateTime.ToLocalTime();
        DateTime utcTime = context.CurrentUtcDateTime;

        // ✅ Use context.NewGuid() instead of Guid.NewGuid()
        Guid correlationId = context.NewGuid();

        // ✅ Use durable timer instead of Thread.Sleep
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);

        // ✅ Use deterministic seed for Random based on orchestration context
        int seed = context.CurrentUtcDateTime.GetHashCode() ^ context.InstanceId.GetHashCode();
        var random = new Random(seed);
        int randomValue = random.Next(1, 100);

        // ✅ In real implementation, move I/O operations to activities:
        // var fileContent = await context.CallActivityAsync<string>("ReadConfigFileActivity", "config.txt");
        // var envVar = await context.CallActivityAsync<string>("GetEnvironmentVariableActivity", "TEMP");
        // var httpResult = await context.CallActivityAsync<string>("FetchDataActivity", "https://api.example.com/data");

        string result = $"Processed correctly: {input} - Start: {startTime}, ID: {correlationId}, Random: {randomValue}";
        return result;
    }

    /// <summary>
    /// Demonstrates the CORRECT approach for DFA0010 in DTF: delegate binding operations to activities.
    /// ✅ This orchestrator properly delegates all binding access to activities,
    /// maintaining determinism even when processing external binding data.
    /// </summary>
    public static async Task<string> OrchestrationWithProperBindingDelegation(TaskOrchestrationContext context)
    {
        // ✅ Delegate blob processing to activities - no direct bindings in orchestrator
        string blobContent = await context.CallActivityAsync<string>(
            "ProcessBlobActivity",
            new { containerName = "container", blobName = "data.txt" });

        // ✅ Delegate queue processing to activities - no direct bindings in orchestrator  
        string queueResult = await context.CallActivityAsync<string>(
            "ProcessQueueMessageActivity",
            new { queueName = "processing-queue", message = "sample-message" });

        // ✅ Delegate table operations to activities - no direct bindings in orchestrator
        await context.CallActivityAsync(
            "SaveToTableActivity",
            new { 
                tableName = "MyTable",
                data = new { 
                    BlobContent = blobContent.Substring(0, Math.Min(100, blobContent.Length)),
                    QueueResult = queueResult,
                    Timestamp = context.CurrentUtcDateTime 
                }
            });

        return $"DTF orchestrator processed blob and queue data correctly: {queueResult}";
    }

    /// <summary>
    /// More examples of proper binding delegation in DTF context.
    /// ✅ All external service access is delegated to activities.
    /// </summary>
    public static async Task<string> OrchestrationWithCompleteBindingDelegation(TaskOrchestrationContext context)
    {
        // ✅ Service Bus processing delegated to activity
        string serviceBusResult = await context.CallActivityAsync<string>(
            "ProcessServiceBusMessageActivity",
            new { topic = "notifications", subscription = "processors", message = "dtf-message" });

        // ✅ Table operations delegated to activity
        await context.CallActivityAsync(
            "SaveToTableActivity",
            new { tableName = "ProcessingResults", data = new { Result = serviceBusResult, ProcessedAt = context.CurrentUtcDateTime } });

        // Orchestrator only coordinates - all I/O through activities
        return $"DTF orchestrator completed Service Bus processing: {serviceBusResult}";
    }
}
