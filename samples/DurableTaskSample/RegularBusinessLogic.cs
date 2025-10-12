namespace DurableTaskSample;

/// <summary>
/// Regular class with methods that don't have orchestration context.
/// The analyzer should NOT flag violations in these methods.
/// </summary>
public class RegularBusinessLogic
{
    /// <summary>
    /// Regular method - not an orchestrator, so analyzer should ignore violations.
    /// </summary>
    public static async Task<string> ProcessBusinessLogic(string input)
    {
        // These operations are fine in regular methods (not orchestrators):
        DateTime now = DateTime.Now;
        var id = Guid.NewGuid();
        int random = new Random().Next(1, 100);
        Thread.Sleep(100);
        await Task.Delay(100);
        string file = await File.ReadAllTextAsync("test.txt").ConfigureAwait(false) ?? "";
        string? env = Environment.GetEnvironmentVariable("PATH");

        return $"Regular processing: {input} - {now} - {id} - {random} - {file.Length} - {env?.Length}";
    }
}
