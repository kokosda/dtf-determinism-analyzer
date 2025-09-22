using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;

namespace DurableTaskSample;

/// <summary>
/// Demonstrates problematic patterns in core Durable Task Framework orchestrations.
/// This class contains orchestrator-like methods that violate determinism rules
/// to show how the analyzer detects issues in DTF contexts (not just Azure Functions).
/// 
/// Note: This sample focuses on demonstrating analyzer behavior rather than runtime execution.
/// The analyzer detects patterns based on method signatures and context parameters.
/// </summary>
public class ProblematicDtfOrchestrator
{
    // ❌ DFA0006: Static mutable state access
    private static int _staticCounter = 0;
    private static readonly object _lockObject = new object();

    /// <summary>
    /// Simulated orchestrator method with DTF context parameter.
    /// The analyzer detects this as an orchestrator based on the TaskOrchestrationContext parameter.
    /// </summary>
    public async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // ❌ DFA0001: Using DateTime.Now in orchestrator (non-deterministic!)
        var startTime = DateTime.Now;
        
        // ❌ DFA0001: Using DateTime.UtcNow in orchestrator (non-deterministic!)
        var utcTime = DateTime.UtcNow;
        
        // ❌ DFA0002: Using Guid.NewGuid() in orchestrator (non-deterministic!)
        var correlationId = Guid.NewGuid();
        
        // ❌ DFA0007: Using Thread.Sleep in orchestrator (blocking operation!)
        Thread.Sleep(1000);
        
        // ❌ DFA0008: Using non-durable async operations
        await Task.Delay(500);
        
        // ❌ DFA0003: Using non-deterministic Random
        var random = new Random();
        var randomValue = random.Next(1, 100);
        
        // ❌ DFA0004: File I/O operations in orchestrator
        var fileContent = await File.ReadAllTextAsync("config.txt");
        
        // ❌ DFA0005: Environment variable access in orchestrator
        var envVar = Environment.GetEnvironmentVariable("TEMP");
        
        // ❌ DFA0006: Static state access and modification
        var currentCount = _staticCounter;
        lock (_lockObject)
        {
            // ❌ DFA0009: Using lock (threading API)
            _staticCounter++;
        }
        
        // ❌ DFA0008: Non-durable async operation
        var httpResult = await new HttpClient().GetStringAsync("https://api.example.com/data");
        
        // This is OK - simulated activity call (real implementation would use context.CallActivityAsync)
        var result = $"Processed: {input} with violations - Start: {startTime}, ID: {correlationId}, Random: {randomValue}";
        
        return result;
    }
}

/// <summary>
/// Demonstrates the corrected version following DTF determinism rules.
/// This orchestrator uses proper patterns and will not trigger analyzer warnings.
/// </summary>
public class CorrectedDtfOrchestrator
{
    /// <summary>
    /// Properly deterministic orchestrator method.
    /// Uses TaskOrchestrationContext parameter which makes analyzer treat it as an orchestrator.
    /// </summary>
    public async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // ✅ Use context.CurrentUtcDateTime instead of DateTime.Now/UtcNow
        var startTime = context.CurrentUtcDateTime.ToLocalTime();
        var utcTime = context.CurrentUtcDateTime;
        
        // ✅ Use context.NewGuid() instead of Guid.NewGuid()
        var correlationId = context.NewGuid();
        
        // ✅ Use durable timer instead of Thread.Sleep
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
        
        // ✅ Use deterministic seed for Random based on orchestration context
        var seed = context.CurrentUtcDateTime.GetHashCode() ^ context.InstanceId.GetHashCode();
        var random = new Random(seed);
        var randomValue = random.Next(1, 100);
        
        // ✅ In real implementation, move I/O operations to activities:
        // var fileContent = await context.CallActivityAsync<string>("ReadConfigFileActivity", "config.txt");
        // var envVar = await context.CallActivityAsync<string>("GetEnvironmentVariableActivity", "TEMP");
        // var httpResult = await context.CallActivityAsync<string>("FetchDataActivity", "https://api.example.com/data");
        
        var result = $"Processed correctly: {input} - Start: {startTime}, ID: {correlationId}, Random: {randomValue}";
        return result;
    }
}

/// <summary>
/// Complex orchestrator demonstrating advanced DTF patterns.
/// Shows how analyzer handles more complex orchestration scenarios.
/// </summary>
public class ComplexDtfOrchestrator
{
    /// <summary>
    /// Complex orchestration with multiple deterministic patterns.
    /// </summary>
    public async Task<ComplexResult> RunComplexOrchestrationAsync(TaskOrchestrationContext context, ComplexInput input)
    {
        var startTime = context.CurrentUtcDateTime;
        
        var result = new ComplexResult
        {
            ProcessingId = context.NewGuid(),
            StartedAt = startTime,
            Steps = new List<string>()
        };
        
        // Step 1: Validation (would call activity in real implementation)
        result.Steps.Add("Starting validation");
        var isValid = !string.IsNullOrWhiteSpace(input.Data);
        
        if (!isValid)
        {
            result.Status = "Failed - Invalid input";
            return result;
        }
        
        // Step 2: Use durable timers for delays
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
        result.Steps.Add("Completed delay");
        
        // Step 3: Deterministic processing
        var deterministicSeed = context.CurrentUtcDateTime.Ticks + context.InstanceId.GetHashCode();
        var random = new Random((int)(deterministicSeed % int.MaxValue));
        var processingId = random.Next(1000, 9999);
        
        result.Steps.Add($"Generated processing ID: {processingId}");
        
        // Step 4: Final processing
        result.Status = "Completed";
        result.FinalResult = $"Processed {input.Data} with ID {processingId}";
        result.CompletedAt = context.CurrentUtcDateTime;
        result.Duration = result.CompletedAt - result.StartedAt;
        
        return result;
    }
}

/// <summary>
/// Regular class with methods that don't have orchestration context.
/// The analyzer should NOT flag violations in these methods.
/// </summary>
public class RegularBusinessLogic
{
    /// <summary>
    /// Regular method - not an orchestrator, so analyzer should ignore violations.
    /// </summary>
    public async Task<string> ProcessBusinessLogic(string input)
    {
        // These operations are fine in regular methods (not orchestrators):
        var now = DateTime.Now;
        var id = Guid.NewGuid();
        var random = new Random().Next(1, 100);
        Thread.Sleep(100);
        await Task.Delay(100);
        var file = await File.ReadAllTextAsync("test.txt").ConfigureAwait(false) ?? "";
        var env = Environment.GetEnvironmentVariable("PATH");
        
        return $"Regular processing: {input} - {now} - {id} - {random} - {file.Length} - {env?.Length}";
    }
}

// Data models for complex orchestrator
public class ComplexInput
{
    public string Data { get; set; } = "";
    public int ParallelCount { get; set; } = 3;
    public bool UseSubOrchestrator { get; set; } = true;
}

public class ComplexResult
{
    public Guid ProcessingId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status { get; set; } = "";
    public string FinalResult { get; set; } = "";
    public List<string> Steps { get; set; } = new();
}