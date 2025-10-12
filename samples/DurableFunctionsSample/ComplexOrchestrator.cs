using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsSample;

/// <summary>
/// A more complex orchestrator that demonstrates real-world patterns
/// while following determinism rules.
/// </summary>
public class ComplexOrchestrator
{
    private readonly ILogger<ComplexOrchestrator> _logger;

    public ComplexOrchestrator(ILogger<ComplexOrchestrator> logger)
    {
        _logger = logger;
    }

    public class ComplexInput
    {
        public string UserId { get; set; } = "";
        public string ProcessingType { get; set; } = "";
        public int RetryCount { get; set; }
    }

    public class ProcessingResult
    {
        public string UserId { get; set; } = "";
        public DateTime ProcessedAt { get; set; }
        public string Status { get; set; } = "";
        public List<string> Steps { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
    }

    /// <summary>
    /// Complex orchestrator demonstrating multiple patterns:
    /// - Sub-orchestrators
    /// - Parallel execution
    /// - Conditional logic
    /// - Timer usage
    /// - Error handling
    /// </summary>
    [Function(nameof(RunComplexOrchestrator))]
    public async Task<ProcessingResult> RunComplexOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ComplexInput input = context.GetInput<ComplexInput>()!;
        DateTime startTime = context.CurrentUtcDateTime;
        var steps = new List<string>();

        var result = new ProcessingResult
        {
            UserId = input.UserId,
            ProcessedAt = startTime,
            Status = "Processing",
            Steps = steps
        };

        try
        {
            // Step 1: Validate input
            steps.Add("Validating input");
            bool isValid = await context.CallActivityAsync<bool>(
                nameof(Activities.ValidateUserActivity),
                input.UserId);

            if (!isValid)
            {
                result.Status = "Invalid user";
                return result;
            }

            // Step 2: Run parallel activities based on processing type
            steps.Add("Starting parallel processing");

            var tasks = new List<Task<string>>();

            if (input.ProcessingType == "standard" || input.ProcessingType == "premium")
            {
                tasks.Add(context.CallActivityAsync<string>(
                    nameof(Activities.ProcessStandardDataActivity),
                    input.UserId));
            }

            if (input.ProcessingType == "premium")
            {
                tasks.Add(context.CallActivityAsync<string>(
                    nameof(Activities.ProcessPremiumDataActivity),
                    input.UserId));
            }

            // Wait for all parallel tasks
            string[] results = await Task.WhenAll(tasks);
            steps.Add($"Completed {results.Length} parallel operations");

            // Step 3: Conditional delay based on processing type
            if (input.ProcessingType == "premium")
            {
                // Premium processing includes additional validation time
                steps.Add("Premium validation delay");
                await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(5), CancellationToken.None);
            }

            // Step 4: Call sub-orchestrator for complex processing
            if (results.Any())
            {
                steps.Add("Starting sub-orchestrator");
                string subResult = await context.CallSubOrchestratorAsync<string>(
                    nameof(RunDataAggregationSubOrchestrator),
                    new { userId = input.UserId, data = results });
                steps.Add($"Sub-orchestrator completed: {subResult}");
            }

            // Step 5: Final processing with retry logic
            var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
                maxNumberOfAttempts: input.RetryCount,
                firstRetryInterval: TimeSpan.FromSeconds(2)));

            string finalResult = await context.CallActivityAsync<string>(
                nameof(Activities.FinalizeProcessingActivity),
                input.UserId,
                options: retryOptions);

            steps.Add("Finalization completed");
            result.Status = "Completed";
        }
        catch (TaskFailedException ex)
        {
            steps.Add($"Error: {ex.Message}");
            result.Status = "Failed";
        }

        result.TotalDuration = context.CurrentUtcDateTime - startTime;
        result.Steps = steps;

        return result;
    }

    /// <summary>
    /// Sub-orchestrator for data aggregation.
    /// Demonstrates how to break complex logic into smaller, manageable orchestrators.
    /// </summary>
    [Function(nameof(RunDataAggregationSubOrchestrator))]
    public async Task<string> RunDataAggregationSubOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        dynamic input = context.GetInput<dynamic>()!;

        // Process each data item
        var aggregatedData = new List<string>();

        foreach (string dataItem in (IEnumerable<string>)input.data)
        {
            string processed = await context.CallActivityAsync<string>(
                nameof(Activities.AggregateDataActivity),
                new { input.userId, data = dataItem });

            aggregatedData.Add(processed);

            // Small delay between processing items to avoid overwhelming downstream systems
            await context.CreateTimer(context.CurrentUtcDateTime.AddMilliseconds(100), CancellationToken.None);
        }

        // Return aggregated result
        return JsonSerializer.Serialize(aggregatedData);
    }

    /// <summary>
    /// Demonstrates fan-out/fan-in pattern with proper determinism.
    /// </summary>
    [Function(nameof(RunFanOutFanInExample))]
    public async Task<List<string>> RunFanOutFanInExample(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        string[] input = context.GetInput<string[]>() ?? Array.Empty<string>();

        // Fan-out: Start multiple activities in parallel
        Task<string>[] tasks = input.Select(item =>
            context.CallActivityAsync<string>(
                nameof(Activities.ProcessItemActivity),
                item)).ToArray();

        // Fan-in: Wait for all to complete
        string[] results = await Task.WhenAll(tasks);

        return results.ToList();
    }
}
