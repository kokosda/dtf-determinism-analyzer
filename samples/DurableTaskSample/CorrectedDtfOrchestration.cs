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
}
