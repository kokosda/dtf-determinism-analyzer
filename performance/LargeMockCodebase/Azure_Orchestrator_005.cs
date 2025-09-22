
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Performance.Azure.Batch0
{
    public class TestOrchestrator005
    {
    [FunctionName("TestOrchestrator005_Method0")]
    public async Task<string> Method0Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = Thread.Sleep(1000);
        var temp1 = Guid.NewGuid();
        var temp2 = Environment.GetEnvironmentVariable("TEST");
        var temp3 = DateTime.Now;
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity0", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method1")]
    public async Task<string> Method1Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = Guid.NewGuid();
        var temp1 = Task.Delay(1000);
        var temp2 = DateTime.UtcNow;
        var temp3 = Task.Run(() => {});
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity1", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method2")]
    public async Task<string> Method2Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = Environment.GetEnvironmentVariable("TEST");
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity2", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method3")]
    public async Task<string> Method3Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = File.ReadAllText("test.txt");
        var temp1 = Guid.NewGuid();
        var temp2 = Environment.GetEnvironmentVariable("TEST");
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity3", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method4")]
    public async Task<string> Method4Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = new Random().Next();
        var temp1 = DateTime.UtcNow;
        var temp2 = Task.Delay(1000);
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity4", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method5")]
    public async Task<string> Method5Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = new Random().Next();
        var temp1 = DateTime.Now;
        var temp2 = Guid.NewGuid();
        var temp3 = File.ReadAllText("test.txt");
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity5", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method6")]
    public async Task<string> Method6Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = Task.Run(() => {});
        var temp1 = DateTime.UtcNow;
        var temp2 = Guid.NewGuid();
        var temp3 = Thread.Sleep(1000);
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity6", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    [FunctionName("TestOrchestrator005_Method7")]
    public async Task<string> Method7Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Performance test method with violations
        var temp0 = Guid.NewGuid();
        var temp1 = DateTime.Now;
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity7", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }
    }
}
