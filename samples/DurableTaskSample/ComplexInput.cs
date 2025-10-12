namespace DurableTaskSample;

/// <summary>
/// Data model for complex orchestrator input.
/// </summary>
public class ComplexInput
{
    public string Data { get; set; } = "";
    public int ParallelCount { get; set; } = 3;
    public bool UseSubOrchestrator { get; set; } = true;
}
