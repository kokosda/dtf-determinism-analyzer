using Microsoft.DurableTask;

namespace DurableTaskSample;

/// <summary>
/// Complex orchestrator demonstrating advanced DTF patterns.
/// Shows how analyzer handles more complex orchestration scenarios.
/// </summary>
public class ComplexDtfOrchestration
{
    /// <summary>
    /// Complex orchestration with multiple deterministic patterns.
    /// </summary>
    public static async Task<ComplexResult> RunComplexOrchestrationAsync(TaskOrchestrationContext context, ComplexInput input)
    {
        DateTime startTime = context.CurrentUtcDateTime;

        var result = new ComplexResult
        {
            ProcessingId = context.NewGuid(),
            StartedAt = startTime,
            Steps = new List<string>()
        };

        // Step 1: Validation (would call activity in real implementation)
        result.Steps.Add("Starting validation");
        bool isValid = !string.IsNullOrWhiteSpace(input.Data);

        if (!isValid)
        {
            result.Status = "Failed - Invalid input";
            return result;
        }

        // Step 2: Use durable timers for delays
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
        result.Steps.Add("Completed delay");

        // Step 3: Deterministic processing
        long deterministicSeed = context.CurrentUtcDateTime.Ticks + context.InstanceId.GetHashCode();
        var random = new Random((int)(deterministicSeed % int.MaxValue));
        int processingId = random.Next(1000, 9999);

        result.Steps.Add($"Generated processing ID: {processingId}");

        // Step 4: Final processing
        result.Status = "Completed";
        result.FinalResult = $"Processed {input.Data} with ID {processingId}";
        result.CompletedAt = context.CurrentUtcDateTime;
        result.Duration = result.CompletedAt - result.StartedAt;

        return result;
    }
}
