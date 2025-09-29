# DTF Determinism Analyzer

A production-ready Roslyn analyzer and code fixes package that validates Durable Task Framework (DTF) orchestration code for determinism constraints. Ensures your orchestrator functions follow replay-safe patterns required by Azure Durable Functions and Durable Task Framework.

[![NuGet Version](https://img.shields.io/nuget/v/DtfDeterminismAnalyzer)](https://www.nuget.org/packages/DtfDeterminismAnalyzer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DtfDeterminismAnalyzer)](https://www.nuget.org/packages/DtfDeterminismAnalyzer)
[![Build Status](https://img.shields.io/github/actions/workflow/status/kokosda/dtf-determinism-analyzer/ci.yml)](https://github.com/kokosda/dtf-determinism-analyzer/actions)
[![License](https://img.shields.io/github/license/kokosda/dtf-determinism-analyzer)](LICENSE)

## Quick Start

### 1. Install the Package

Install via NuGet Package Manager:

```xml
<PackageReference Include="DtfDeterminismAnalyzer" Version="1.0.0" PrivateAssets="all" />
```

Or via .NET CLI:

```bash
dotnet add package DtfDeterminismAnalyzer --version 1.0.0
```

### 2. Write Your Orchestrator

```csharp
[FunctionName("MyOrchestrator")]
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    // The analyzer will automatically detect determinism issues
    var result = await context.CallActivityAsync<string>("MyActivity", "input");
    return result;
}
```

### 3. Fix Issues Automatically

When you write problematic code, the analyzer will:
- 🔍 **Detect** non-deterministic patterns
- ⚠️ **Report** diagnostics with clear messages
- 🔧 **Suggest** automatic fixes via code actions

### 4. Configure Rules (Optional)

Customize rule severities in `.editorconfig`:

```ini
[*.cs]
# DTF Determinism Rules
dotnet_diagnostic.DFA0001.severity = error
dotnet_diagnostic.DFA0002.severity = warning
dotnet_diagnostic.DFA0003.severity = suggestion
```

## Overview

This package helps ensure your DTF orchestrator functions follow determinism rules required for proper replay behavior. It provides:

- **Static analysis** - Detects non-deterministic API usage in orchestrator functions
- **Code fixes** - Automatic fixes to replace problematic code with durable alternatives
- **IDE integration** - Works in Visual Studio, VS Code, and Rider

## Rules Reference

### DFA0001: DateTime and Stopwatch APIs
**Don't use DateTime.Now, DateTime.UtcNow, or Stopwatch in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var now = DateTime.Now;  // Non-deterministic!
    var utcNow = DateTime.UtcNow;  // Non-deterministic!
    var stopwatch = Stopwatch.StartNew();  // Non-deterministic!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var now = context.CurrentUtcDateTime.ToLocalTime();  // Deterministic
    var utcNow = context.CurrentUtcDateTime;  // Deterministic
    var timer = await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(10), CancellationToken.None);  // Deterministic
}
```

### DFA0002: Guid.NewGuid() calls
**Don't use Guid.NewGuid() in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var id = Guid.NewGuid();  // Non-deterministic!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var id = context.NewGuid();  // Deterministic
}
```

### DFA0003: Random without seed
**Don't use Random without a deterministic seed in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var random = new Random();  // Non-deterministic seed!
    var value = random.Next();
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var seed = context.CurrentUtcDateTime.GetHashCode();
    var random = new Random(seed);  // Deterministic seed
    var value = random.Next();
}
```

### DFA0004: I/O operations
**Don't perform I/O operations directly in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var content = File.ReadAllText("file.txt");  // I/O operation!
    var httpResult = await httpClient.GetAsync("https://api.example.com");  // I/O operation!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var content = await context.CallActivityAsync<string>("ReadFileActivity", "file.txt");  // Use activity
    var httpResult = await context.CallActivityAsync<string>("HttpCallActivity", "https://api.example.com");  // Use activity
}
```

### DFA0005: Environment variables
**Don't access environment variables in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var path = Environment.GetEnvironmentVariable("PATH");  // Non-deterministic!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var path = await context.CallActivityAsync<string>("GetEnvironmentVariableActivity", "PATH");  // Use activity
}
```

### DFA0006: Static mutable state
**Don't access static mutable state in orchestrators**

❌ **Problem:**
```csharp
public static int Counter = 0;

public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    Counter++;  // Static mutable state!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    await context.CallActivityAsync("IncrementCounterActivity", null);  // Use activity for state changes
}
```

### DFA0007: Thread blocking operations
**Don't use Thread.Sleep or other blocking operations in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    Thread.Sleep(1000);  // Blocking operation!
    Task.Delay(1000).Wait();  // Blocking operation!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);  // Durable timer
}
```

### DFA0008: Non-durable async operations
**Don't use non-durable async operations in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    await Task.Delay(1000);  // Non-durable async!
    await Task.Run(() => DoWork());  // Non-durable async!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);  // Durable timer
    await context.CallActivityAsync("DoWorkActivity", null);  // Durable activity call
}
```

### DFA0009: Threading APIs
**Don't use threading APIs in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var thread = new Thread(() => DoWork());  // Threading API!
    thread.Start();
    
    await Task.Run(() => DoWork());  // Threading API!
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    await context.CallActivityAsync("DoWorkActivity", null);  // Use activity for work
}
```

### DFA0010: Non-durable bindings
**Don't use non-durable bindings in orchestrators**

❌ **Problem:**
```csharp
public static async Task<string> RunOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context,
    [Blob("container/blob")] CloudBlockBlob blob)  // Non-durable binding!
{
    var content = await blob.DownloadTextAsync();
}
```

✅ **Solution:**
```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var content = await context.CallActivityAsync<string>("ReadBlobActivity", "container/blob");  // Use activity
}
```

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

## Configuration

### Rule Severity Customization

Configure rule severities in `.editorconfig`:

```ini
[*.cs]
# DTF Determinism Rules - Set severity levels
dotnet_diagnostic.DFA0001.severity = error      # DateTime/Stopwatch APIs
dotnet_diagnostic.DFA0002.severity = warning    # Guid.NewGuid()
dotnet_diagnostic.DFA0003.severity = suggestion # Random without seed
dotnet_diagnostic.DFA0004.severity = warning    # I/O operations
dotnet_diagnostic.DFA0005.severity = warning    # Environment variables
dotnet_diagnostic.DFA0006.severity = error      # Static mutable state
dotnet_diagnostic.DFA0007.severity = warning    # Thread blocking
dotnet_diagnostic.DFA0008.severity = suggestion # Non-durable async
dotnet_diagnostic.DFA0009.severity = warning    # Threading APIs
dotnet_diagnostic.DFA0010.severity = error      # Non-durable bindings
```

### Global Suppression

To disable all DTF rules globally, add to `GlobalSuppressions.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("DTF", "DFA0001", Justification = "Reviewed")]
// Add more suppressions as needed
```

## Code Fixes

The package includes automatic code fixes for common violations:

- **DFA0001**: `DateTime.Now` → `context.CurrentUtcDateTime`, `Stopwatch` → `context.CreateTimer()`
- **DFA0002**: `Guid.NewGuid()` → `context.NewGuid()`
- **DFA0007**: `Thread.Sleep()` → `await context.CreateTimer()`

### Using Code Fixes

1. **Visual Studio**: Click the lightbulb 💡 icon or press `Ctrl+.`
2. **VS Code**: Click the lightbulb 💡 icon or press `Ctrl+.` (Windows) / `Cmd+.` (macOS)
3. **Rider**: Press `Alt+Enter`

## Troubleshooting

### Common Issues

**Q: The analyzer doesn't detect issues in my orchestrator**
A: Make sure your method has one of these attributes:
- `[FunctionName]` with `[OrchestrationTrigger]` parameter
- `[Function]` with `[OrchestrationTrigger]` parameter

**Q: I'm getting false positives outside of orchestrators**
A: The analyzer only triggers in orchestrator functions. If you're seeing issues in regular methods, check that your orchestrator detection is working correctly.

**Q: Code fixes aren't showing up**
A: Ensure the `DtfDeterminismAnalyzer` package is installed with `PrivateAssets="all"` and restart your IDE.

**Q: How do I disable a specific rule?**
A: Add to your `.editorconfig`:
```ini
[*.cs]
dotnet_diagnostic.DFA0001.severity = none  # Disable DateTime rule
```

**Q: Can I suppress rules for a specific method?**
A: Yes, use `#pragma` directives or `[SuppressMessage]` attributes:
```csharp
#pragma warning disable DFA0001
var time = DateTime.Now; // This won't trigger the analyzer
#pragma warning restore DFA0001
```

### Getting Help

- 📖 **Documentation**: [View detailed docs](https://github.com/your-repo/dtf-determinism-analyzer)
- 🐛 **Report Issues**: [Create an issue](https://github.com/your-repo/dtf-determinism-analyzer/issues)
- 💬 **Discussions**: [Join the conversation](https://github.com/your-repo/dtf-determinism-analyzer/discussions)

## Complete Examples

### Basic Orchestrator

Here's a complete example showing the analyzer in action:

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Threading;
using System.Threading.Tasks;

public static class MyOrchestrator
{
    [FunctionName("MyOrchestrator")]
    public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // ❌ This will trigger DFA0001
        // var timestamp = DateTime.Now;
        
        // ✅ Use this instead - auto-fixable!
        var timestamp = context.CurrentUtcDateTime;
        
        // ❌ This will trigger DFA0002
        // var id = Guid.NewGuid();
        
        // ✅ Use this instead - auto-fixable!
        var id = context.NewGuid();
        
        // ❌ This will trigger DFA0007
        // Thread.Sleep(1000);
        
        // ✅ Use this instead - auto-fixable!
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None);
        
        // Call activities for any I/O or external operations
        var result = await context.CallActivityAsync<string>("ProcessDataActivity", new { timestamp, id });
        
        return result;
    }
    
    [FunctionName("ProcessDataActivity")]
    public static async Task<string> ProcessDataActivity([ActivityTrigger] object input)
    {
        // Activities can use any APIs - no restrictions
        var httpResult = await new HttpClient().GetStringAsync("https://api.example.com");
        var currentTime = DateTime.Now; // This is fine in activities
        var newId = Guid.NewGuid(); // This is fine in activities
        
        return httpResult;
    }
}
```

### Before and After Comparison

**❌ Problematic Code:**
```csharp
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

**✅ Fixed Code:**
```csharp
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

## Additional Resources

### Documentation

For more information about DTF determinism constraints and best practices:

- **[Durable Functions Code Constraints](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-code-constraints)** - Official Microsoft documentation
- **[Durable Functions Overview](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview)** - Introduction to Durable Functions
- **[Orchestrator Function Constraints](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-checkpointing-and-replay)** - Understanding replay behavior

### Related Tools

- **[Microsoft.Azure.WebJobs.Extensions.DurableTask](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.DurableTask/)** - The Durable Functions runtime
- **[Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)** - Local development tools

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup

1. Clone the repository
2. Open in Visual Studio 2022+ or VS Code
3. Build the solution: `dotnet build`
4. Run tests: `dotnet test`

### Adding New Rules

To add a new determinism rule:

1. Create a new analyzer in `src/DtfDeterminismAnalyzer/Analyzers/`
2. Add diagnostic descriptor in `DiagnosticDescriptors.cs`
3. Create corresponding tests in `tests/DtfDeterminismAnalyzer.Tests/`
4. Optionally add a code fix provider

## Changelog

### v1.0.0
- Initial release with 10 determinism rules
- Code fixes for DateTime, GUID, and Thread.Sleep violations
- Support for Azure Functions v3 and v4

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Made with ❤️ for the DTF community**