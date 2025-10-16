using Microsoft.DurableTask;

namespace DurableTaskSample;

/// <summary>
/// Simple test file to verify code fixes are working.
/// Open this file in Visual Studio 2022 and try Ctrl+. on the violations below.
/// </summary>
public class CodeFixTest
{
    /// <summary>
    /// Simple orchestrator method for testing code fixes.
    /// </summary>
    public static async Task<string> TestOrchestrationAsync(TaskOrchestrationContext context, string input)
    {
        // DFA0001: This should offer a code fix to replace with context.CurrentUtcDateTime
        DateTime now = DateTime.Now;
        
        // DFA0002: This should offer a code fix to replace with context.NewGuid()
        Guid id = Guid.NewGuid();
        
        // DFA0007: This should offer a code fix to replace with await context.CreateTimer(TimeSpan.FromMilliseconds(1000))
        Thread.Sleep(1000);
        
        return $"Input: {input}, Time: {now}, ID: {id}";
    }
}