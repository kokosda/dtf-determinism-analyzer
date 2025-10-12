using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsSample;

/// <summary>
/// HTTP trigger functions that start orchestrations and demonstrate usage patterns.
/// </summary>
public class HttpTriggers
{
    private readonly ILogger<HttpTriggers> _logger;

    public HttpTriggers(ILogger<HttpTriggers> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts the problematic orchestrator (will show analyzer warnings).
    /// </summary>
    [Function("StartProblematicOrchestrator")]
    public async Task<HttpResponseData> StartProblematicOrchestrator(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Starting problematic orchestrator");

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ProblematicOrchestrator.RunProblematicOrchestrator));

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/status/{instanceId}",
            message = "Problematic orchestrator started (check build output for analyzer warnings)"
        });

        return response;
    }

    /// <summary>
    /// Starts the corrected orchestrator (analyzer-compliant).
    /// </summary>
    [Function("StartCorrectedOrchestrator")]
    public async Task<HttpResponseData> StartCorrectedOrchestrator(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Starting corrected orchestrator");

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(CorrectedOrchestrator.RunCorrectedOrchestrator));

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/status/{instanceId}",
            message = "Corrected orchestrator started (no analyzer warnings)"
        });

        return response;
    }

    /// <summary>
    /// Complex example that demonstrates multiple patterns.
    /// </summary>
    [Function("StartComplexExample")]
    public async Task<HttpResponseData> StartComplexExample(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Starting complex example orchestrator");

        var input = new ComplexOrchestrator.ComplexInput
        {
            UserId = "user123",
            ProcessingType = "standard",
            RetryCount = 3
        };

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ComplexOrchestrator.RunComplexOrchestrator),
            input);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            input,
            statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/status/{instanceId}",
            message = "Complex orchestrator started"
        });

        return response;
    }
}
