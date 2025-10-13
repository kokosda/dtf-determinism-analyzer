using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsSample;

/// <summary>
/// Activity functions that perform the actual work.
/// Activities have no determinism restrictions - they can perform I/O, access environment variables,
/// use current time, etc. This is where all the non-deterministic operations should go.
/// </summary>
public class Activities
{
    private readonly ILogger<Activities> _logger;
    private readonly HttpClient _httpClient;

    public Activities(ILogger<Activities> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    // Basic data processing activities

    [Function(nameof(ProcessDataActivity))]
    public string ProcessDataActivity([ActivityTrigger] dynamic input)
    {
        // Activities can safely use current time, GUIDs, I/O, etc.
        DateTime processedAt = DateTime.Now;
        var processingId = Guid.NewGuid();

        _logger.LogInformation("Processing data at {ProcessedAt} with ID {ProcessingId}",
            processedAt, processingId);

        return JsonSerializer.Serialize(new
        {
            input,
            processedAt,
            processingId,
            result = "Data processed successfully"
        });
    }

    [Function(nameof(ProcessRandomDataActivity))]
    public string ProcessRandomDataActivity([ActivityTrigger] dynamic input)
    {
        // Activities can perform any operations including I/O
        DateTime timestamp = DateTime.UtcNow;
        string fileData = "Simulated file content"; // In reality: File.ReadAllText(...)

        return JsonSerializer.Serialize(new
        {
            input,
            timestamp,
            fileData,
            result = "Random data processed"
        });
    }

    // I/O activities (what orchestrators should delegate to)

    [Function(nameof(ReadConfigFileActivity))]
    public Task<string> ReadConfigFileActivity([ActivityTrigger] string fileName)
    {
        _logger.LogInformation("Reading config file: {FileName}", fileName);

        // In a real implementation, this would read from actual file system
        // return await File.ReadAllTextAsync(fileName);

        // Simulated config content
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            fileName,
            content = "{ \"setting1\": \"value1\", \"setting2\": \"value2\" }",
            readAt = DateTime.UtcNow
        }));
    }

    [Function(nameof(GetEnvironmentVariableActivity))]
    public string GetEnvironmentVariableActivity([ActivityTrigger] string variableName)
    {
        _logger.LogInformation("Getting environment variable: {VariableName}", variableName);

        // Activities can safely access environment variables
        string value = Environment.GetEnvironmentVariable(variableName) ?? "Not found";

        return JsonSerializer.Serialize(new
        {
            variableName,
            value,
            retrievedAt = DateTime.UtcNow
        });
    }

    [Function(nameof(RiskyOperationActivity))]
    public async Task<string> RiskyOperationActivity([ActivityTrigger] string input)
    {
        _logger.LogInformation("Performing risky operation with input: {Input}", input);

        // Simulate an operation that might fail
        var random = new Random();
        if (random.NextDouble() < 0.3) // 30% chance of failure
        {
            throw new InvalidOperationException("Simulated failure in risky operation");
        }

        // Simulate some async work
        await Task.Delay(1000);

        return $"Risky operation completed successfully for: {input}";
    }

    // Complex orchestrator support activities

    [Function(nameof(ValidateUserActivity))]
    public async Task<bool> ValidateUserActivity([ActivityTrigger] string userId)
    {
        _logger.LogInformation("Validating user: {UserId}", userId);

        // Simulate user validation (could involve database call, API call, etc.)
        await Task.Delay(500);

        // Simple validation: reject empty or obviously invalid IDs
        return !string.IsNullOrWhiteSpace(userId) && userId != "invalid";
    }

    [Function(nameof(ProcessStandardDataActivity))]
    public async Task<string> ProcessStandardDataActivity([ActivityTrigger] string userId)
    {
        _logger.LogInformation("Processing standard data for user: {UserId}", userId);

        // Simulate standard processing
        await Task.Delay(1000);

        return $"Standard processing completed for {userId} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }

    [Function(nameof(ProcessPremiumDataActivity))]
    public async Task<string> ProcessPremiumDataActivity([ActivityTrigger] string userId)
    {
        _logger.LogInformation("Processing premium data for user: {UserId}", userId);

        // Simulate more intensive premium processing
        await Task.Delay(2000);

        // Premium processing might involve external API calls
        string apiResult = await SimulateApiCall($"https://api.example.com/premium/{userId}");

        return $"Premium processing completed for {userId}. API result: {apiResult}";
    }

    [Function(nameof(AggregateDataActivity))]
    public string AggregateDataActivity([ActivityTrigger] dynamic input)
    {
        string userId = (string)input.userId;
        string data = (string)input.data;

        _logger.LogInformation("Aggregating data for user {UserId}: {Data}", userId, data);

        // Simulate data aggregation
        var aggregated = new
        {
            userId,
            originalData = data,
            aggregatedAt = DateTime.UtcNow,
            hash = data.GetHashCode(),
            length = data.Length
        };

        return JsonSerializer.Serialize(aggregated);
    }

    [Function(nameof(FinalizeProcessingActivity))]
    public async Task<string> FinalizeProcessingActivity([ActivityTrigger] string userId)
    {
        _logger.LogInformation("Finalizing processing for user: {UserId}", userId);

        // Simulate finalization steps (database updates, notifications, etc.)
        await Task.Delay(800);

        // Simulate possible failure for retry demonstration
        var random = new Random();
        if (random.NextDouble() < 0.2) // 20% chance of failure
        {
            throw new InvalidOperationException("Finalization failed - will retry");
        }

        return $"Processing finalized for {userId} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }

    [Function(nameof(ProcessItemActivity))]
    public async Task<string> ProcessItemActivity([ActivityTrigger] string item)
    {
        _logger.LogInformation("Processing item: {Item}", item);

        // Simulate variable processing time
        var random = new Random();
        int delay = random.Next(500, 2000);
        await Task.Delay(delay);

        return $"Processed: {item} (took {delay}ms)";
    }

    // Helper methods

    private async Task<string> SimulateApiCall(string url)
    {
        try
        {
            // In a real scenario, you'd make an actual HTTP call
            // var response = await _httpClient.GetStringAsync(url);

            // Simulate API response
            await Task.Delay(300);
            return JsonSerializer.Serialize(new
            {
                url,
                response = "API response data",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API call failed to {Url}", url);
            return "API call failed";
        }
    }
}
