
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DurableTask;

namespace Performance.DTF.Batch1
{
    public class DtfOrchestrator017
    {
    public async Task<string> Method0Async(TaskOrchestrationContext context)
    {
        // DTF performance test method with violations
        var temp0 = Task.Delay(1000);
        var temp1 = Thread.Sleep(1000);
        var temp2 = HttpClient.GetAsync("https://example.com");
        var temp3 = File.ReadAllText("test.txt");
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity0", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }
    public async Task<string> Method1Async(TaskOrchestrationContext context)
    {
        // DTF performance test method with violations
        var temp0 = DateTime.UtcNow;
        var temp1 = HttpClient.GetAsync("https://example.com");
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity1", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }
    public async Task<string> Method2Async(TaskOrchestrationContext context)
    {
        // DTF performance test method with violations
        var temp0 = Guid.NewGuid();
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity2", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }
    public async Task<string> Method3Async(TaskOrchestrationContext context)
    {
        // DTF performance test method with violations
        var temp0 = new Random().Next();
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity3", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }
    public async Task<string> Method4Async(TaskOrchestrationContext context)
    {
        // DTF performance test method with violations
        var temp0 = Thread.Sleep(1000);
        var temp1 = Task.Run(() => {});
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity4", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }
    public async Task<string> Method5Async(TaskOrchestrationContext context)
    {
        // DTF performance test method with violations
        var temp0 = new Random().Next();
        var temp1 = Environment.GetEnvironmentVariable("TEST");
        var temp2 = Guid.NewGuid();
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity5", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }
    }
}
