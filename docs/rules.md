# Complete Rules Documentation

This document provides comprehensive documentation for all DTF Determinism Analyzer rules with detailed examples and explanations.

## Rule Summary Table

| Rule ID | Category | Severity | Title | Auto-Fix |
|---------|----------|----------|--------|----------|
| DFA0001 | Time APIs | Warning | Don't use DateTime.Now, DateTime.UtcNow, or Stopwatch in orchestrators | ✅ |
| DFA0002 | Random Generation | Warning | Don't use Guid.NewGuid() in orchestrators | ✅ |
| DFA0003 | Random Generation | Warning | Don't use Random class without a deterministic seed in orchestrators | ❌ |
| DFA0004 | I/O Operations | Warning | Don't use I/O operations directly in orchestrators | ❌ |
| DFA0005 | Environment | Warning | Don't access environment variables in orchestrators | ❌ |
| DFA0006 | Static State | Warning | Don't access static mutable state in orchestrators | ❌ |
| DFA0007 | Threading | Warning | Don't use Thread.Sleep or other blocking operations in orchestrators | ✅ |
| DFA0008 | Async Operations | Warning | Don't use non-durable async operations in orchestrators | ❌ |
| DFA0009 | Threading APIs | Warning | Don't use threading APIs in orchestrators | ❌ |
| DFA0010 | Bindings | Warning | Don't use non-durable input bindings in orchestrators | ❌ |

---

## DFA0001: DateTime and Stopwatch APIs

**Don't use DateTime.Now, DateTime.UtcNow, or Stopwatch in orchestrators**

### Problem
DateTime and Stopwatch APIs return different values on each execution, breaking replay determinism.

### ❌ Problematic Code

**Azure Durable Functions:**
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var now = DateTime.Now;        // DFA0001: Non-deterministic!
    var utcNow = DateTime.UtcNow;  // DFA0001: Non-deterministic!
    var stopwatch = Stopwatch.StartNew(); // DFA0001: Non-deterministic!
    
    return $"Time: {now}, UTC: {utcNow}, Elapsed: {stopwatch.ElapsedMilliseconds}";
}
```

**Durable Task Framework:**
```csharp
public class BadOrchestration : TaskOrchestration<string, string>
{
    public override async Task<string> RunTask(OrchestrationContext context, string input)
    {
        var now = DateTime.Now;        // DFA0001: Non-deterministic!
        var utcNow = DateTime.UtcNow;  // DFA0001: Non-deterministic!
        var stopwatch = Stopwatch.StartNew(); // DFA0001: Non-deterministic!
        
        return $"Time: {now}, UTC: {utcNow}, Elapsed: {stopwatch.ElapsedMilliseconds}";
    }
}
```

### ✅ Corrected Code

**Azure Durable Functions:**
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var now = context.CurrentUtcDateTime.ToLocalTime(); // Deterministic
    var utcNow = context.CurrentUtcDateTime;             // Deterministic
    
    // Use durable timer instead of Stopwatch
    var startTime = context.CurrentUtcDateTime;
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
    var elapsed = context.CurrentUtcDateTime - startTime;
    
    return $"Time: {now}, UTC: {utcNow}, Elapsed: {elapsed.TotalMilliseconds}";
}
```

**Durable Task Framework:**
```csharp
public class GoodOrchestration : TaskOrchestration<string, string>
{
    public override async Task<string> RunTask(OrchestrationContext context, string input)
    {
        var now = context.CurrentUtcDateTime.ToLocalTime(); // Deterministic
        var utcNow = context.CurrentUtcDateTime;             // Deterministic
        
        // Use durable timer instead of Stopwatch
        var startTime = context.CurrentUtcDateTime;
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1));
        var elapsed = context.CurrentUtcDateTime - startTime;
        
        return $"Time: {now}, UTC: {utcNow}, Elapsed: {elapsed.TotalMilliseconds}";
    }
}
```

---

## DFA0002: Guid.NewGuid() calls

**Don't use Guid.NewGuid() in orchestrators**

### Problem
`Guid.NewGuid()` generates different GUIDs on each replay, breaking determinism.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var id = Guid.NewGuid(); // DFA0002: Non-deterministic!
    return id.ToString();
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var id = context.NewGuid(); // Deterministic GUID generation
    return id.ToString();
}
```

---

## DFA0003: Random without seed

**Don't use Random class without a deterministic seed in orchestrators**

### Problem
`new Random()` uses a time-based seed, producing different values on replay.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var random = new Random(); // DFA0003: Non-deterministic seed!
    var value = random.Next(1, 100);
    return value.ToString();
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Use deterministic seed based on orchestration context
    var seed = context.CurrentUtcDateTime.GetHashCode();
    var random = new Random(seed);
    var value = random.Next(1, 100);
    return value.ToString();
}
```

---

## DFA0004: I/O operations

**Don't perform I/O operations directly in orchestrators**

### Problem
I/O operations can fail, have different results, or take different amounts of time during replay.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // DFA0004: Direct I/O operations!
    var fileContent = File.ReadAllText("data.txt");
    var httpResult = await new HttpClient().GetStringAsync("https://api.example.com");
    var dbResult = await database.QueryAsync("SELECT * FROM Users");
    
    return $"{fileContent}-{httpResult}-{dbResult}";
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Move I/O operations to activities
    var fileContent = await context.CallActivityAsync<string>("ReadFileActivity", "data.txt");
    var httpResult = await context.CallActivityAsync<string>("HttpCallActivity", "https://api.example.com");
    var dbResult = await context.CallActivityAsync<string>("DatabaseQueryActivity", "SELECT * FROM Users");
    
    return $"{fileContent}-{httpResult}-{dbResult}";
}

[FunctionName("ReadFileActivity")]
public static string ReadFileActivity([ActivityTrigger] string filePath)
{
    return File.ReadAllText(filePath); // I/O is safe in activities
}
```

---

## DFA0005: Environment variables

**Don't access environment variables in orchestrators**

### Problem
Environment variables can change between executions, breaking replay determinism.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING"); // DFA0005
    var apiKey = Environment.GetEnvironmentVariable("API_KEY"); // DFA0005
    
    return $"Config: {connectionString}, Key: {apiKey}";
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var connectionString = await context.CallActivityAsync<string>("GetConfigActivity", "CONNECTION_STRING");
    var apiKey = await context.CallActivityAsync<string>("GetConfigActivity", "API_KEY");
    
    return $"Config: {connectionString}, Key: {apiKey}";
}

[FunctionName("GetConfigActivity")]
public static string GetConfigActivity([ActivityTrigger] string key)
{
    return Environment.GetEnvironmentVariable(key); // Safe in activities
}
```

---

## DFA0006: Static mutable state

**Don't access static mutable state in orchestrators**

### Problem
Static mutable state can be modified by other threads or processes, causing non-deterministic behavior.

### ❌ Problematic Code
```csharp
public static class GlobalState
{
    public static int Counter = 0;
    public static Dictionary<string, object> Cache = new();
}

[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    GlobalState.Counter++; // DFA0006: Mutable static state!
    GlobalState.Cache["key"] = "value"; // DFA0006: Mutable static state!
    
    return GlobalState.Counter.ToString();
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Use activities for state management
    var counter = await context.CallActivityAsync<int>("IncrementCounterActivity", null);
    await context.CallActivityAsync("SetCacheActivity", new { key = "key", value = "value" });
    
    return counter.ToString();
}

[FunctionName("IncrementCounterActivity")]
public static int IncrementCounterActivity([ActivityTrigger] object input)
{
    return ++GlobalState.Counter; // Safe to modify state in activities
}
```

---

## DFA0007: Thread blocking operations

**Don't use Thread.Sleep or other blocking operations in orchestrators**

### Problem
Blocking operations prevent the orchestrator thread from processing other orchestrations and can cause deadlocks.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    Thread.Sleep(5000); // DFA0007: Blocking operation!
    Task.Delay(1000).Wait(); // DFA0007: Blocking operation!
    
    return "Done waiting";
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Use durable timers instead
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(5), CancellationToken.None);
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
    
    return "Done waiting";
}
```

---

## DFA0008: Non-durable async operations

**Don't use non-durable async operations in orchestrators**

### Problem
Non-durable async operations are not tracked by the orchestration framework and can cause replay issues.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    await Task.Delay(1000); // DFA0008: Non-durable async!
    await Task.Run(() => DoWork()); // DFA0008: Non-durable async!
    
    return "Work completed";
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Use durable operations instead
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
    await context.CallActivityAsync("DoWorkActivity", null);
    
    return "Work completed";
}

[FunctionName("DoWorkActivity")]
public static void DoWorkActivity([ActivityTrigger] object input)
{
    DoWork(); // Work is safe in activities
}
```

---

## DFA0009: Threading APIs

**Don't use threading APIs in orchestrators**

### Problem
Threading APIs create non-deterministic execution paths that cannot be replayed consistently.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // DFA0009: Threading APIs!
    var thread = new Thread(() => DoWork());
    thread.Start();
    
    await Task.Run(() => DoMoreWork()); // Also DFA0009
    
    return "Work started";
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Use activities for parallel work
    var task1 = context.CallActivityAsync("DoWorkActivity", null);
    var task2 = context.CallActivityAsync("DoMoreWorkActivity", null);
    
    await Task.WhenAll(task1, task2);
    
    return "Work completed";
}
```

---

## DFA0010: Non-durable bindings

**Don't use non-durable input bindings in orchestrators**

### Problem
Non-durable bindings (like Blob, Queue, Table) can have different states during replay, breaking determinism.

### ❌ Problematic Code
```csharp
[FunctionName("BadOrchestrator")]
public static async Task<string> BadOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context,
    [Blob("container/blob")] CloudBlockBlob blob) // DFA0010: Non-durable binding!
{
    var content = await blob.DownloadTextAsync();
    return content;
}
```

### ✅ Corrected Code
```csharp
[FunctionName("GoodOrchestrator")]
public static async Task<string> GoodOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // Move binding access to activities
    var content = await context.CallActivityAsync<string>("ReadBlobActivity", "container/blob");
    return content;
}

[FunctionName("ReadBlobActivity")]
public static async Task<string> ReadBlobActivity(
    [ActivityTrigger] string blobPath,
    [Blob("container/{blobPath}")] CloudBlockBlob blob) // Bindings are safe in activities
{
    return await blob.DownloadTextAsync();
}
```

## Why These Rules Matter

All these rules exist because of the **replay behavior** of durable orchestrations:

1. **Orchestrations are replayed** from the beginning when they resume after waiting
2. **Replays must produce identical results** to maintain consistency
3. **Non-deterministic operations** break this replay guarantee
4. **Activities are not replayed** - they're cached, making them safe for non-deterministic operations

Understanding this fundamental concept helps you write better durable orchestrations and understand why the analyzer flags certain patterns as problematic.