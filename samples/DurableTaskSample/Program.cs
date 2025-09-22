using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DurableTaskSample;

/// <summary>
/// Main program demonstrating core Durable Task Framework usage
/// with the DTF Determinism Analyzer.
/// 
/// This sample focuses on demonstrating analyzer behavior rather than runtime execution.
/// The key insight is that the analyzer detects orchestrator methods based on:
/// - Methods with TaskOrchestrationContext parameters
/// - Methods inheriting from TaskOrchestration base classes
/// - Azure Functions with [OrchestrationTrigger] attributes
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🔧 DTF Determinism Analyzer - Core Durable Task Framework Sample");
        Console.WriteLine("================================================================");
        Console.WriteLine();
        Console.WriteLine("This sample demonstrates how the DTF Determinism Analyzer detects");
        Console.WriteLine("determinism violations in core DTF orchestration methods.");
        Console.WriteLine();
        Console.WriteLine("Key detection patterns:");
        Console.WriteLine("- Methods with TaskOrchestrationContext parameters");
        Console.WriteLine("- Classes inheriting from TaskOrchestration base classes");
        Console.WriteLine("- Azure Functions with [OrchestrationTrigger] attributes");
        Console.WriteLine();

        Console.WriteLine("📋 To see analyzer violations:");
        Console.WriteLine("   1. Run: dotnet build");
        Console.WriteLine("   2. Observe DFA0001-DFA0010 error messages");
        Console.WriteLine("   3. Compare ProblematicDtfOrchestrator vs CorrectedDtfOrchestrator");
        Console.WriteLine();

        // Demonstrate the orchestrator classes
        DemonstrateOrchestratorPatterns();
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(true);
    }

    private static void DemonstrateOrchestratorPatterns()
    {
        Console.WriteLine("� ProblematicDtfOrchestrator violations:");
        Console.WriteLine("   - DFA0001: DateTime.Now, DateTime.UtcNow usage");
        Console.WriteLine("   - DFA0002: Guid.NewGuid() usage");  
        Console.WriteLine("   - DFA0003: Random() without deterministic seed");
        Console.WriteLine("   - DFA0004: File.ReadAllTextAsync() I/O operations");
        Console.WriteLine("   - DFA0005: Environment.GetEnvironmentVariable() access");
        Console.WriteLine("   - DFA0006: Static state access and modification");
        Console.WriteLine("   - DFA0007: Thread.Sleep() blocking operations");
        Console.WriteLine("   - DFA0008: Task.Delay() and HttpClient async operations");
        Console.WriteLine("   - DFA0009: lock statements (threading APIs)");
        Console.WriteLine();

        Console.WriteLine("✅ CorrectedDtfOrchestrator fixes:");
        Console.WriteLine("   - Uses context.CurrentUtcDateTime instead of DateTime.Now");
        Console.WriteLine("   - Uses context.NewGuid() instead of Guid.NewGuid()");
        Console.WriteLine("   - Uses context.CreateTimer() instead of Thread.Sleep/Task.Delay");
        Console.WriteLine("   - Moves I/O operations to activities (simulated)");
        Console.WriteLine("   - Uses deterministic Random with context-based seed");
        Console.WriteLine();

        Console.WriteLine("🔄 ComplexDtfOrchestrator demonstrates:");
        Console.WriteLine("   - Advanced deterministic patterns");
        Console.WriteLine("   - Proper context usage for timing and IDs");
        Console.WriteLine("   - Activity delegation patterns");
        Console.WriteLine();

        Console.WriteLine("✅ RegularBusinessLogic shows:");
        Console.WriteLine("   - Methods without TaskOrchestrationContext are NOT analyzed");
        Console.WriteLine("   - Regular business logic can use any APIs freely");
        Console.WriteLine("   - Analyzer only targets orchestration contexts");
        Console.WriteLine();
    }
}