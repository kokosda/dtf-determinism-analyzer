# Durable Functions Sample with DTF Determinism Analyzer

This sample application demonstrates how to use the DTF Determinism Analyzer with Azure Durable Functions. It includes both problematic code patterns (that trigger analyzer warnings) and their corrected versions.

## Overview

The sample contains:

- **Problematic patterns** - Code that violates DTF determinism rules
- **Corrected patterns** - Fixed versions that follow best practices
- **Complex examples** - Real-world orchestration patterns done correctly
- **Activity functions** - Where non-deterministic operations belong

## Project Structure

```
DurableFunctionsSample/
├── Program.cs                    # Function app entry point
├── host.json                     # Function host configuration
├── local.settings.json           # Local development settings
├── HttpTriggers.cs               # HTTP endpoints to start orchestrations
├── ProblematicOrchestrator.cs    # ❌ Examples that trigger analyzer warnings
├── CorrectedOrchestrator.cs      # ✅ Fixed versions following rules
├── ComplexOrchestrator.cs        # ✅ Advanced patterns done correctly
└── Activities.cs                 # Activity functions (no restrictions)
```

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure Storage Emulator](https://docs.microsoft.com/azure/storage/common/storage-use-emulator) or [Azurite](https://github.com/Azure/Azurite)

### Running the Sample

1. **Start the storage emulator:**
   ```bash
   # Using Azurite (recommended)
   azurite --silent --location ./azurite --debug ./azurite/debug.log
   
   # Or using Azure Storage Emulator
   AzureStorageEmulator.exe start
   ```

2. **Build the solution:**
   ```bash
   cd samples/DurableFunctionsSample
   dotnet build
   ```

3. **Run the function app:**
   ```bash
   func start
   ```

4. **Test the endpoints:**
   ```bash
   # Start problematic orchestrator (will show analyzer warnings during build)
   curl -X POST http://localhost:7071/api/StartProblematicOrchestrator
   
   # Start corrected orchestrator (analyzer-compliant)
   curl -X POST http://localhost:7071/api/StartCorrectedOrchestrator
   
   # Start complex example
   curl -X POST http://localhost:7071/api/StartComplexExample
   ```

## Analyzer Integration

The sample project references the DTF Determinism Analyzer as an analyzer-only dependency:

```xml
<ProjectReference Include="..\..\src\DtfDeterminismAnalyzer\DtfDeterminismAnalyzer.csproj" 
                  PrivateAssets="all" 
                  IncludeAssets="analyzers" />
```

When you build the project, you'll see analyzer warnings for the problematic orchestrator:

```
ProblematicOrchestrator.cs(25,27): warning DFA0001: Don't use DateTime.Now, DateTime.UtcNow, or Stopwatch in orchestrators
ProblematicOrchestrator.cs(28,27): warning DFA0001: Don't use DateTime.Now, DateTime.UtcNow, or Stopwatch in orchestrators
ProblematicOrchestrator.cs(31,31): warning DFA0002: Don't use Guid.NewGuid() in orchestrators
ProblematicOrchestrator.cs(34,9): warning DFA0007: Don't use Thread.Sleep or other blocking operations in orchestrators
```

## Examples Explained

### ❌ Problematic Patterns (`ProblematicOrchestrator.cs`)

```csharp
[Function(nameof(RunProblematicOrchestrator))]
public async Task<string> RunProblematicOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
{
    var startTime = DateTime.Now;        // ❌ DFA0001: Non-deterministic time
    var correlationId = Guid.NewGuid();  // ❌ DFA0002: Non-deterministic GUID
    Thread.Sleep(1000);                  // ❌ DFA0007: Blocking operation
    
    // ... rest of orchestrator
}
```

**Problems:**
- `DateTime.Now` returns different values on replay
- `Guid.NewGuid()` generates different GUIDs on replay
- `Thread.Sleep()` blocks the orchestrator thread

### ✅ Corrected Patterns (`CorrectedOrchestrator.cs`)

```csharp
[Function(nameof(RunCorrectedOrchestrator))]
public async Task<string> RunCorrectedOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
{
    var startTime = context.CurrentUtcDateTime.ToLocalTime();  // ✅ Replay-safe time
    var correlationId = context.NewGuid();                     // ✅ Deterministic GUID
    await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1));  // ✅ Durable timer
    
    // ... rest of orchestrator
}
```

**Solutions:**
- Use `context.CurrentUtcDateTime` for consistent time across replays
- Use `context.NewGuid()` for deterministic GUID generation
- Use `context.CreateTimer()` for durable delays

### 🔧 Complex Patterns (`ComplexOrchestrator.cs`)

The complex orchestrator demonstrates:

- **Parallel execution** with `Task.WhenAll()`
- **Sub-orchestrators** for breaking down complex workflows
- **Retry policies** for handling failures
- **Fan-out/Fan-in** patterns
- **Conditional logic** based on input parameters

### 🏃‍♂️ Activity Functions (`Activities.cs`)

Activities have **no determinism restrictions**:

```csharp
[Function(nameof(ProcessDataActivity))]
public string ProcessDataActivity([ActivityTrigger] dynamic input)
{
    // ✅ Activities can use any APIs
    var processedAt = DateTime.Now;      // OK in activities
    var processingId = Guid.NewGuid();   // OK in activities
    var file = File.ReadAllText("...");  // OK in activities
    var env = Environment.GetEnvironmentVariable("PATH");  // OK in activities
    
    return "processed";
}
```

## Code Fixes

The analyzer provides automatic code fixes for some violations:

1. **Build the project** - You'll see analyzer warnings
2. **Open in IDE** - Visual Studio, VS Code, or Rider
3. **Click the lightbulb** (💡) icon next to the warning
4. **Apply the fix** - The analyzer will automatically correct the code

### Available Fixes

| Rule | Fix Available | Description |
|------|---------------|-------------|
| DFA0001 | ✅ | Replaces `DateTime.Now/UtcNow` with `context.CurrentUtcDateTime` |
| DFA0002 | ✅ | Replaces `Guid.NewGuid()` with `context.NewGuid()` |
| DFA0007 | ✅ | Replaces `Thread.Sleep()` with `context.CreateTimer()` |
| Others | ❌ | Manual fix required (guidance provided in analyzer message) |

## Best Practices Demonstrated

1. **Deterministic Operations in Orchestrators:**
   - Use context APIs for time and GUIDs
   - Avoid direct I/O, environment access, and random operations
   - Use durable timers instead of blocking calls

2. **Activity Function Design:**
   - Move all non-deterministic operations to activities
   - Activities can use any APIs without restrictions
   - Keep activities focused and testable

3. **Error Handling:**
   - Use retry policies for transient failures
   - Handle exceptions deterministically in orchestrators
   - Log errors in activities for debugging

4. **Complex Orchestration Patterns:**
   - Use sub-orchestrators to break down complex workflows
   - Implement fan-out/fan-in for parallel processing
   - Use conditional logic based on deterministic inputs

## Troubleshooting

### Common Issues

**Q: I don't see analyzer warnings**
A: Make sure the DTF Determinism Analyzer project is built and the reference is correct.

**Q: Functions don't start**
A: Ensure the storage emulator is running and `local.settings.json` has the correct connection string.

**Q: Code fixes don't appear**
A: Restart your IDE and ensure the analyzer assembly is properly loaded.

### Debug Tips

- Enable detailed MSBuild output to see analyzer loading
- Check the function app logs for runtime errors
- Use Azure Storage Explorer to inspect orchestration state

## Learn More

- [DTF Determinism Analyzer Documentation](../../README.md)
- [Azure Durable Functions Documentation](https://docs.microsoft.com/azure/azure-functions/durable/)
- [Durable Functions Code Constraints](https://docs.microsoft.com/azure/azure-functions/durable/durable-functions-code-constraints)