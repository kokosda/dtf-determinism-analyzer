# DTF Determinism Analyzer - Core Durable Task Framework Sample

This sample demonstrates how the DTF Determinism Analyzer works with the core Durable Task Framework (not Azure Functions). It shows that the analyzer correctly identifies determinism violations in `TaskOrchestration<TOutput, TInput>` base class implementations.

## Overview

The sample contains:

- **Problematic orchestrator** - Uses `TaskOrchestration` base class with determinism violations
- **Corrected orchestrator** - Fixed version following DTF best practices  
- **Complex orchestrator** - Advanced patterns with fan-out/fan-in and sub-orchestrators
- **Activities** - Where non-deterministic operations belong (no analyzer restrictions)

## Project Structure

```
DurableTaskSample/
├── Program.cs                    # Console application entry point with DTF host setup
├── Orchestrators.cs              # TaskOrchestration implementations (both problematic and corrected)
├── Activities.cs                 # Activity implementations (no determinism restrictions)
├── DurableTaskSample.csproj      # Project file with DTF packages and analyzer reference
└── README.md                     # This documentation
```

## Key Differences from Azure Functions

| Aspect | Azure Functions | Core DTF |
|--------|-----------------|----------|
| Base Class | Uses `TaskOrchestrationContext` parameter | Inherits from `TaskOrchestration<TOutput, TInput>` |
| Attributes | `[Function]`, `[OrchestrationTrigger]` | No special attributes needed |
| Hosting | Azure Functions runtime | Generic .NET host with DTF services |
| Deployment | Azure Functions | Any .NET hosting environment |

## Analyzer Behavior

The DTF Determinism Analyzer correctly identifies violations in **both** Azure Functions and core DTF orchestrators:

### ✅ Detected in Core DTF:
- **DFA0001**: `DateTime.Now`, `DateTime.UtcNow` in `TaskOrchestration.RunAsync()`
- **DFA0002**: `Guid.NewGuid()` in orchestrator methods
- **DFA0003**: `new Random()` without deterministic seed
- **DFA0004**: File I/O operations (`File.ReadAllTextAsync()`)
- **DFA0005**: Environment variables (`Environment.GetEnvironmentVariable()`)
- **DFA0006**: Static state access and modification
- **DFA0007**: `Thread.Sleep()`, `lock` statements
- **DFA0008**: `Task.Delay()`, `HttpClient` calls
- **DFA0009**: Threading APIs usage
- **DFA0010**: Direct binding usage (when applicable)

### ✅ Properly Ignored in Activities:
- Activities can use any APIs without triggering warnings
- No determinism constraints on activity implementations
- Full access to I/O, HTTP, random, environment variables, etc.

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- DTF Determinism Analyzer (built from source)

### Build and Run

1. **Build the project** (observe analyzer warnings):
   ```bash
   cd samples/DurableTaskSample
   dotnet build
   ```
   
   Expected output shows analyzer violations:
   ```
   error DFA0001: Non-deterministic time API used in orchestrator
   error DFA0002: Non-deterministic GUID generated in orchestrator
   error DFA0003: Non-deterministic random used in orchestrator
   ...
   ```

2. **Run the sample** (demonstrates orchestration patterns):
   ```bash
   dotnet run
   ```
   
   Note: The sample will show how to start orchestrations. A full DTF runtime setup would be needed for execution.

## Code Examples

### ❌ Problematic Pattern (Triggers Analyzer)
```csharp
public class ProblematicDtfOrchestrator : TaskOrchestration<string, string>
{
    public override async Task<string> RunAsync(TaskOrchestrationContext context, string input)
    {
        // ❌ DFA0001: Non-deterministic time
        var now = DateTime.Now;
        
        // ❌ DFA0002: Non-deterministic GUID
        var id = Guid.NewGuid();
        
        // ❌ DFA0007: Blocking operation
        Thread.Sleep(1000);
        
        return await context.CallActivityAsync<string>("ProcessData", input);
    }
}
```

### ✅ Corrected Pattern (Analyzer Compliant)
```csharp
public class CorrectedDtfOrchestrator : TaskOrchestration<string, string>
{
    public override async Task<string> RunAsync(TaskOrchestrationContext context, string input)
    {
        // ✅ Use context for deterministic time
        var now = context.CurrentUtcDateTime;
        
        // ✅ Use context for deterministic GUID
        var id = context.NewGuid();
        
        // ✅ Use durable timer instead of blocking
        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(1));
        
        return await context.CallActivityAsync<string>("ProcessData", input);
    }
}
```

## Benefits of Core DTF Analysis

1. **Framework Agnostic**: Analyzer works beyond just Azure Functions
2. **Broader Coverage**: Any DTF-based application gets determinism validation  
3. **Migration Support**: Helps when moving between Azure Functions and other DTF hosts
4. **Library Validation**: Ensures DTF orchestration libraries follow best practices

## Orchestrator Detection

The analyzer uses the same orchestrator detection logic for core DTF:

- Methods inheriting from `TaskOrchestration<TOutput, TInput>`
- `RunAsync()` method implementations
- Any method with `TaskOrchestrationContext` parameter

## Learn More

- [Core DTF Documentation](https://github.com/microsoft/durabletask-dotnet)
- [DTF Determinism Analyzer](../../README.md)  
- [Azure Functions DTF Sample](../DurableFunctionsSample/README.md)
- [Durable Functions Code Constraints](https://docs.microsoft.com/azure/azure-functions/durable/durable-functions-code-constraints)

## Notes

This sample focuses on demonstrating analyzer behavior rather than a complete DTF runtime setup. For production usage, you would need:

- Proper DTF backend (Azure Storage, SQL Server, etc.)
- Activity registration and implementation
- Error handling and monitoring
- Deployment configuration