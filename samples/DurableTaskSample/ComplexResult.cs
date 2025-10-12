namespace DurableTaskSample;

/// <summary>
/// Data model for complex orchestrator result.
/// </summary>
public class ComplexResult
{
    public Guid ProcessingId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status { get; set; } = "";
    public string FinalResult { get; set; } = "";
    public List<string> Steps { get; set; } = new();
}
