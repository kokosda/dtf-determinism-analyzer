using Microsoft.Extensions.Logging;

namespace DurableTaskSample;

/// <summary>
/// Activity functions for the DTF sample application.
/// Activities can perform any operations including I/O, HTTP calls, etc.
/// without triggering analyzer warnings since they don't have determinism constraints.
/// </summary>
public static class Activities
{
    /// <summary>
    /// Processes data from orchestrator - can use any APIs without restrictions.
    /// </summary>
    [Function(nameof(ProcessDataActivity))]
    public static Task<string> ProcessDataActivity([ActivityTrigger] object input, ILogger logger)
    {
        // Activities can safely use DateTime.Now, Random, I/O, etc.
        DateTime processedAt = DateTime.Now;
        var random = new Random();
        int randomSuffix = random.Next(1000, 9999);

        logger.LogInformation("Processing data at {Time} with random suffix {Suffix}",
            processedAt, randomSuffix);

        return Task.FromResult($"Processed: {input} - {processedAt:yyyy-MM-dd HH:mm:ss} - {randomSuffix}");
    }

    /// <summary>
    /// Reads configuration files safely in activity.
    /// </summary>
    [Function(nameof(ReadConfigFileActivity))]
    public static async Task<string> ReadConfigFileActivity([ActivityTrigger] string fileName, ILogger logger)
    {
        try
        {
            // Safe to do file I/O in activities
            if (File.Exists(fileName))
            {
                string content = await File.ReadAllTextAsync(fileName);
                logger.LogInformation("Read {Length} characters from {FileName}", content.Length, fileName);
                return content;
            }
            else
            {
                // Create a sample config if it doesn't exist
                string sampleConfig = $"# Sample config created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                await File.WriteAllTextAsync(fileName, sampleConfig);
                logger.LogInformation("Created sample config file {FileName}", fileName);
                return sampleConfig;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read config file {FileName}", fileName);
            return $"Error reading {fileName}: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets environment variables safely in activity.
    /// </summary>
    [Function(nameof(GetEnvironmentVariableActivity))]
    public static Task<string> GetEnvironmentVariableActivity([ActivityTrigger] string variableName, ILogger logger)
    {
        // Safe to access environment variables in activities
        string value = Environment.GetEnvironmentVariable(variableName) ?? $"Not found: {variableName}";
        logger.LogInformation("Environment variable {Name} = {Value}", variableName, value);
        return Task.FromResult(value);
    }

    /// <summary>
    /// Performs HTTP calls safely in activity.
    /// </summary>
    [Function(nameof(FetchDataActivity))]
    public static async Task<string> FetchDataActivity([ActivityTrigger] string url, ILogger logger)
    {
        try
        {
            // Safe to make HTTP calls in activities
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // For demo purposes, simulate a response instead of making real HTTP call
            await Task.Delay(100); // Simulate network delay
            string mockResponse = $"Mock data from {url} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            logger.LogInformation("Fetched data from {Url}", url);
            return mockResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch data from {Url}", url);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Manages state safely in activity with proper concurrency control.
    /// </summary>
    private static int _counter;
    private static readonly object _counterLock = new object();

    [Function(nameof(IncrementCounterActivity))]
    public static Task<int> IncrementCounterActivity([ActivityTrigger] string input, ILogger logger)
    {
        // Safe to use locks and static state in activities
        lock (_counterLock)
        {
            _counter++;
            logger.LogInformation("Counter incremented to {Counter} for input {Input}", _counter, input);
            return Task.FromResult(_counter);
        }
    }

    /// <summary>
    /// Validates input data.
    /// </summary>
    [Function(nameof(ValidateInputActivity))]
    public static Task<bool> ValidateInputActivity([ActivityTrigger] ComplexInput input, ILogger logger)
    {
        bool isValid = !string.IsNullOrWhiteSpace(input.Data) &&
                     input.ParallelCount > 0 &&
                     input.ParallelCount <= 10;

        logger.LogInformation("Input validation result: {IsValid} for data length {Length}",
            isValid, input.Data?.Length ?? 0);

        return Task.FromResult(isValid);
    }

    /// <summary>
    /// Processes a chunk of data in parallel.
    /// </summary>
    [Function(nameof(ProcessChunkActivity))]
    public static async Task<string> ProcessChunkActivity([ActivityTrigger] object input, ILogger logger)
    {
        // Simulate processing time
        var random = new Random();
        int processingTime = random.Next(500, 2000);
        await Task.Delay(processingTime);

        string result = $"Chunk processed: {input} (took {processingTime}ms)";
        logger.LogInformation("Chunk processing completed: {Result}", result);
        return result;
    }

    /// <summary>
    /// Stage 1 processing for sub-orchestrator.
    /// </summary>
    [Function(nameof(Stage1ProcessingActivity))]
    public static async Task<string> Stage1ProcessingActivity([ActivityTrigger] string input, ILogger logger)
    {
        await Task.Delay(500); // Simulate work
        string result = $"Stage1({input})";
        logger.LogInformation("Stage 1 processing: {Input} -> {Result}", input, result);
        return result;
    }

    /// <summary>
    /// Stage 2 processing for sub-orchestrator.
    /// </summary>
    [Function(nameof(Stage2ProcessingActivity))]
    public static async Task<string> Stage2ProcessingActivity([ActivityTrigger] string input, ILogger logger)
    {
        await Task.Delay(500); // Simulate work
        string result = $"Stage2({input})";
        logger.LogInformation("Stage 2 processing: {Input} -> {Result}", input, result);
        return result;
    }

    /// <summary>
    /// Final processing combining all results.
    /// </summary>
    [Function(nameof(FinalProcessingActivity))]
    public static Task<string> FinalProcessingActivity([ActivityTrigger] string[] chunkResults, ILogger logger)
    {
        string combined = string.Join(" | ", chunkResults);
        string result = $"Final: [{combined}] - Completed at {DateTime.Now:HH:mm:ss}";

        logger.LogInformation("Final processing completed with {Count} chunks", chunkResults.Length);
        return Task.FromResult(result);
    }
}

/// <summary>
/// Helper attribute for DTF activities (since we're not using Azure Functions here).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FunctionAttribute : Attribute
{
    public string Name { get; }

    public FunctionAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Helper attribute for DTF activity triggers.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class ActivityTriggerAttribute : Attribute
{
}
