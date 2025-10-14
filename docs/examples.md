# Complete Code Examples

This document provides comprehensive, working examples that demonstrate both problematic patterns and their correct implementations.

## Azure Durable Functions Examples

### Basic Orchestrator - Before and After

**❌ Problematic Code:**
```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var timestamp = DateTime.UtcNow; // DFA0001: Non-deterministic time
    var id = Guid.NewGuid(); // DFA0002: Non-deterministic GUID
    Thread.Sleep(5000); // DFA0007: Blocking operation
    
    var random = new Random(); // DFA0003: Non-deterministic seed
    var value = random.Next();
    
    var config = Environment.GetEnvironmentVariable("CONFIG"); // DFA0005: Environment access
    var file = File.ReadAllText("data.txt"); // DFA0004: I/O operation
    
    await Task.Delay(1000); // DFA0008: Non-durable async
    
    return $"{timestamp}-{id}-{value}-{config}-{file}";
}
```

**✅ Corrected Code:**
```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var timestamp = context.CurrentUtcDateTime; // Uses replay-safe time
    var id = context.NewGuid(); // Uses deterministic GUID generation
    
    // Use durable timer instead of Thread.Sleep
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(5), CancellationToken.None);
    
    // Use deterministic seed based on orchestration context
    var seed = context.CurrentUtcDateTime.GetHashCode();
    var random = new Random(seed);
    var value = random.Next();
    
    // Move environment and I/O operations to activities
    var config = await context.CallActivityAsync<string>("GetConfigActivity", "CONFIG");
    var file = await context.CallActivityAsync<string>("ReadFileActivity", "data.txt");
    
    // Use durable timer instead of Task.Delay
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
    
    return $"{timestamp}-{id}-{value}-{config}-{file}";
}

[FunctionName("GetConfigActivity")]
public static string GetConfigActivity([ActivityTrigger] string key)
{
    return Environment.GetEnvironmentVariable(key); // Activities can access environment
}

[FunctionName("ReadFileActivity")]
public static async Task<string> ReadFileActivity([ActivityTrigger] string filePath)
{
    return await File.ReadAllTextAsync(filePath); // Activities can perform I/O
}
```

### Complex Workflow Example

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[FunctionName("ProcessOrderOrchestrator")]
public static async Task<OrderResult> ProcessOrderOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var order = context.GetInput<Order>();
    
    // Step 1: Validate order (parallel validation)
    var validationTasks = new List<Task<ValidationResult>>
    {
        context.CallActivityAsync<ValidationResult>("ValidateCustomer", order.CustomerId),
        context.CallActivityAsync<ValidationResult>("ValidateInventory", order.Items),
        context.CallActivityAsync<ValidationResult>("ValidatePayment", order.PaymentInfo)
    };
    
    var validationResults = await Task.WhenAll(validationTasks);
    
    if (validationResults.Any(r => !r.IsValid))
    {
        return new OrderResult { Success = false, Error = "Validation failed" };
    }
    
    // Step 2: Reserve inventory
    var reservationId = await context.CallActivityAsync<string>("ReserveInventory", order.Items);
    
    // Step 3: Process payment
    var paymentResult = await context.CallActivityAsync<PaymentResult>("ProcessPayment", order.PaymentInfo);
    
    if (!paymentResult.Success)
    {
        // Rollback reservation
        await context.CallActivityAsync("ReleaseInventory", reservationId);
        return new OrderResult { Success = false, Error = "Payment failed" };
    }
    
    // Step 4: Wait for fulfillment confirmation or timeout
    using (var cts = new CancellationTokenSource())
    {
        var fulfillmentTask = context.WaitForExternalEvent<string>("FulfillmentCompleted");
        var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddHours(24), cts.Token);
        
        var winner = await Task.WhenAny(fulfillmentTask, timeoutTask);
        
        if (winner == fulfillmentTask)
        {
            cts.Cancel(); // Cancel the timer
            var trackingNumber = await fulfillmentTask;
            
            // Send confirmation
            await context.CallActivityAsync("SendConfirmationEmail", new 
            { 
                order.CustomerEmail, 
                TrackingNumber = trackingNumber 
            });
            
            return new OrderResult { Success = true, TrackingNumber = trackingNumber };
        }
        else
        {
            // Timeout - cancel order
            await context.CallActivityAsync("CancelOrder", order.Id);
            await context.CallActivityAsync("RefundPayment", paymentResult.TransactionId);
            await context.CallActivityAsync("ReleaseInventory", reservationId);
            
            return new OrderResult { Success = false, Error = "Fulfillment timeout" };
        }
    }
}

// Supporting classes
public class Order
{
    public string Id { get; set; }
    public string CustomerId { get; set; }
    public string CustomerEmail { get; set; }
    public List<OrderItem> Items { get; set; }
    public PaymentInfo PaymentInfo { get; set; }
}

public class OrderResult
{
    public bool Success { get; set; }
    public string TrackingNumber { get; set; }
    public string Error { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; }
}
```

## Durable Task Framework Examples

### Basic DTF Orchestration

**❌ Problematic Code:**
```csharp
using DurableTask.Core;
using System;
using System.IO;
using System.Threading.Tasks;

public class BadOrchestration : TaskOrchestration<string, string>
{
    public override async Task<string> RunTask(OrchestrationContext context, string input)
    {
        var timestamp = DateTime.UtcNow; // DFA0001: Non-deterministic
        var id = Guid.NewGuid(); // DFA0002: Non-deterministic
        
        var config = Environment.GetEnvironmentVariable("CONFIG"); // DFA0005
        var data = File.ReadAllText("input.txt"); // DFA0004
        
        return $"{timestamp}-{id}-{config}-{data}";
    }
}
```

**✅ Corrected Code:**
```csharp
using DurableTask.Core;
using System;
using System.Threading.Tasks;

public class GoodOrchestration : TaskOrchestration<string, string>
{
    public override async Task<string> RunTask(OrchestrationContext context, string input)
    {
        var timestamp = context.CurrentUtcDateTime; // Deterministic
        var id = context.NewGuid(); // Deterministic
        
        // Move external operations to activities
        var config = await context.ScheduleTask<string>(typeof(GetConfigActivity), "CONFIG");
        var data = await context.ScheduleTask<string>(typeof(ReadFileActivity), "input.txt");
        
        return $"{timestamp}-{id}-{config}-{data}";
    }
}

public class GetConfigActivity : TaskActivity<string, string>
{
    protected override string Execute(TaskContext context, string input)
    {
        return Environment.GetEnvironmentVariable(input); // Safe in activities
    }
}

public class ReadFileActivity : TaskActivity<string, string>
{
    protected override string Execute(TaskContext context, string input)
    {
        return File.ReadAllText(input); // Safe in activities
    }
}
```

### Complex DTF Workflow

```csharp
using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DataProcessingOrchestration : TaskOrchestration<ProcessingResult, ProcessingRequest>
{
    public override async Task<ProcessingResult> RunTask(OrchestrationContext context, ProcessingRequest input)
    {
        var startTime = context.CurrentUtcDateTime;
        var batchId = context.NewGuid().ToString();
        
        try
        {
            // Step 1: Validate input data
            var validationResult = await context.ScheduleTask<ValidationResult>(
                typeof(ValidateDataActivity), input.DataPath);
            
            if (!validationResult.IsValid)
            {
                return new ProcessingResult 
                { 
                    Success = false, 
                    Error = validationResult.ErrorMessage 
                };
            }
            
            // Step 2: Split data into chunks for parallel processing
            var chunks = await context.ScheduleTask<List<DataChunk>>(
                typeof(SplitDataActivity), new { input.DataPath, ChunkSize = 1000 });
            
            // Step 3: Process chunks in parallel
            var processingTasks = chunks.Select(chunk =>
                context.ScheduleTask<ChunkResult>(typeof(ProcessChunkActivity), chunk)
            ).ToList();
            
            var chunkResults = await Task.WhenAll(processingTasks);
            
            // Step 4: Aggregate results
            var aggregateResult = await context.ScheduleTask<AggregateResult>(
                typeof(AggregateResultsActivity), chunkResults);
            
            // Step 5: Generate report
            var reportPath = await context.ScheduleTask<string>(
                typeof(GenerateReportActivity), 
                new { BatchId = batchId, AggregateResult = aggregateResult });
            
            var endTime = context.CurrentUtcDateTime;
            var processingTime = endTime - startTime;
            
            return new ProcessingResult
            {
                Success = true,
                BatchId = batchId,
                ReportPath = reportPath,
                ProcessingTime = processingTime,
                RecordsProcessed = aggregateResult.TotalRecords
            };
        }
        catch (Exception ex)
        {
            // Log error and return failure result
            await context.ScheduleTask(typeof(LogErrorActivity), 
                new { BatchId = batchId, Error = ex.Message });
            
            return new ProcessingResult
            {
                Success = false,
                BatchId = batchId,
                Error = ex.Message
            };
        }
    }
}

// Supporting classes and activities
public class ProcessingRequest
{
    public string DataPath { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public class ProcessingResult
{
    public bool Success { get; set; }
    public string BatchId { get; set; }
    public string ReportPath { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public int RecordsProcessed { get; set; }
    public string Error { get; set; }
}

public class DataChunk
{
    public int ChunkId { get; set; }
    public string DataPath { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}
```

## Pattern Comparisons

### Timer Operations

**❌ Wrong Way:**
```csharp
// Don't use these in orchestrators
Thread.Sleep(5000);
await Task.Delay(5000);
System.Timers.Timer timer = new System.Timers.Timer(5000);
```

**✅ Correct Way:**
```csharp
// Azure Functions
await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(5), CancellationToken.None);

// DTF
await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(5));
```

### Random Number Generation

**❌ Wrong Way:**
```csharp
var random = new Random(); // Time-based seed
var random2 = new Random(Environment.TickCount); // Non-deterministic seed
```

**✅ Correct Way:**
```csharp
// Use deterministic seed
var seed = context.CurrentUtcDateTime.GetHashCode();
var random = new Random(seed);

// Or use orchestration-specific GUID for seed
var guidSeed = context.NewGuid().GetHashCode();
var random2 = new Random(guidSeed);
```

### Parallel Operations

**❌ Wrong Way:**
```csharp
// Don't use Task.Run in orchestrators
await Task.Run(() => DoWork1());
await Task.Run(() => DoWork2());

// Don't use Parallel class
Parallel.ForEach(items, item => ProcessItem(item));
```

**✅ Correct Way:**
```csharp
// Azure Functions - parallel activities
var task1 = context.CallActivityAsync("DoWork1Activity", null);
var task2 = context.CallActivityAsync("DoWork2Activity", null);
await Task.WhenAll(task1, task2);

// DTF - parallel activities
var task1 = context.ScheduleTask(typeof(DoWork1Activity), null);
var task2 = context.ScheduleTask(typeof(DoWork2Activity), null);
await Task.WhenAll(task1, task2);
```

### External Events and Timeouts

```csharp
// Azure Functions example
[FunctionName("WaitForApprovalOrchestrator")]
public static async Task<string> WaitForApprovalOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var approvalRequest = context.GetInput<ApprovalRequest>();
    
    // Send notification
    await context.CallActivityAsync("SendApprovalNotification", approvalRequest);
    
    // Wait for approval or timeout
    using (var cts = new CancellationTokenSource())
    {
        var approvalTask = context.WaitForExternalEvent<bool>("ApprovalReceived");
        var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddDays(3), cts.Token);
        
        var winner = await Task.WhenAny(approvalTask, timeoutTask);
        
        if (winner == approvalTask)
        {
            cts.Cancel();
            var approved = await approvalTask;
            
            if (approved)
            {
                await context.CallActivityAsync("ProcessApprovedRequest", approvalRequest);
                return "Approved and processed";
            }
            else
            {
                await context.CallActivityAsync("HandleRejectedRequest", approvalRequest);
                return "Rejected";
            }
        }
        else
        {
            await context.CallActivityAsync("HandleTimeoutRequest", approvalRequest);
            return "Timed out";
        }
    }
}
```

## Testing Examples

### Unit Testing Orchestrators

```csharp
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using Xunit;

public class OrchestratorTests
{
    [Fact]
    public async Task ProcessOrderOrchestrator_ValidOrder_ReturnsSuccess()
    {
        // Arrange
        var mockContext = new Mock<IDurableOrchestrationContext>();
        var order = new Order { CustomerId = "123", Items = new List<OrderItem>() };
        
        mockContext.Setup(x => x.GetInput<Order>()).Returns(order);
        mockContext.Setup(x => x.CallActivityAsync<ValidationResult>("ValidateCustomer", "123"))
                  .ReturnsAsync(new ValidationResult { IsValid = true });
        
        // Act
        var result = await ProcessOrderOrchestrator(mockContext.Object);
        
        // Assert
        Assert.True(result.Success);
    }
}
```

These examples demonstrate the key principles:

1. **Move non-deterministic operations to activities**
2. **Use context-provided APIs for time, GUIDs, etc.**
3. **Use durable timers instead of blocking operations**
4. **Structure workflows as a series of durable operations**
5. **Handle errors and timeouts gracefully**

Each pattern shows how to transform common problematic code into deterministic, replay-safe orchestrations.