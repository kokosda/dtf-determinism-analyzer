# Installation Guide

## NuGet Package Installation

### Package Manager Console
```powershell
Install-Package DtfDeterminismAnalyzer
```

### .NET CLI
```bash
dotnet add package DtfDeterminismAnalyzer
```

### PackageReference (Recommended)
Add to your `.csproj` file:

```xml
<PackageReference Include="DtfDeterminismAnalyzer" Version="1.0.0" PrivateAssets="all" />
```

> **Important**: Always use `PrivateAssets="all"` to ensure the analyzer is not transitively included in projects that reference your library.

## Supported Frameworks

### Azure Durable Functions
- **Azure Functions v3**: .NET Core 3.1, .NET 5, .NET 6
- **Azure Functions v4**: .NET 6, .NET 7, .NET 8+
- **Isolated Worker**: .NET 5, .NET 6, .NET 7, .NET 8+

### Durable Task Framework
- **.NET Framework**: 4.6.1+
- **.NET Core**: 2.1+
- **.NET**: 5.0+

## IDE Support

The analyzer works with:
- **Visual Studio 2019** (16.3+)
- **Visual Studio 2022** (all versions)
- **VS Code** with C# extension
- **JetBrains Rider** (2020.3+)

## Verification

After installation, create a simple orchestrator with a known violation to verify the analyzer is working:

```csharp
[FunctionName("TestOrchestrator")]
public static async Task<string> TestOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var now = DateTime.Now; // This should show DFA0001 warning
    return now.ToString();
}
```

You should see a warning with rule ID `DFA0001` indicating the analyzer is active.

## Project Configuration

### EditorConfig Setup (Optional)
Create or update `.editorconfig` in your project root:

```ini
[*.cs]
# DTF Determinism Rules - Customize severities
dotnet_diagnostic.DFA0001.severity = error      # DateTime/Stopwatch APIs
dotnet_diagnostic.DFA0002.severity = warning    # Guid.NewGuid()
dotnet_diagnostic.DFA0003.severity = suggestion # Random without seed
# ... configure other rules as needed
```

### Global Suppression (If Needed)
If you need to disable specific rules globally, add to `GlobalSuppressions.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("DTF", "DFA0001", Justification = "Legacy code - planned refactor")]
```

## Troubleshooting Installation

### Analyzer Not Running
1. Verify `PrivateAssets="all"` is set in PackageReference
2. Restart your IDE
3. Clean and rebuild the solution
4. Check that you're targeting a supported .NET version

### Rules Not Appearing
1. Ensure your methods are properly decorated with orchestration attributes
2. Check that the Roslyn analyzer infrastructure is working (try other analyzers)
3. Verify the package was installed correctly in the project (not solution level)

### Performance Issues
If the analyzer is causing build performance issues:
1. Exclude test projects if they don't contain orchestrators
2. Use selective rule configuration to enable only needed rules
3. Consider running analysis only in CI/CD for large codebases