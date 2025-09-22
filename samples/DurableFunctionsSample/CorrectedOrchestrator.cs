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
        var startTime = context.CurrentUtcDateTime.ToLocalTime();
        var utcTime = context.CurrentUtcDateTime;
        
        // ✅ Use context.NewGuid() instead of Guid.NewGuid()
        var correlationId = context.NewGuid();
        
        // ✅ Use durable timer instead of Thread.Sleep
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
        
        // Activities can perform any operations - no restrictions
        var result = await context.CallActivityAsync<string>(
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
        var seed = context.CurrentUtcDateTime.GetHashCode() ^ context.InstanceId.GetHashCode();
        var random = new Random(seed);
        var randomValue = random.Next(1, 100);
        
        // ✅ Use durable timer instead of Task.Delay
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(2), CancellationToken.None);
        
        // ✅ Move I/O operations to activities
        var fileContent = await context.CallActivityAsync<string>(
            nameof(Activities.ReadConfigFileActivity), 
            "config.txt");
        
        // ✅ Move environment variable access to activities
        var envVar = await context.CallActivityAsync<string>(
            nameof(Activities.GetEnvironmentVariableActivity), 
            "TEMP");
        
        var result = await context.CallActivityAsync<string>(
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
            var result = await context.CallActivityAsync<string>(
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
}