# Configuration Guide

> **For Contributors:** If you're developing the analyzer itself, see the [Local Development Guide](local-development.md) for build and testing workflows.

## Rule Severity Customization

Configure rule severities in your project's `.editorconfig` file:

```ini
[*.cs]
# DTF Determinism Rules
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

## Severity Levels

- **`error`**: Breaks the build (recommended for critical determinism violations)
- **`warning`**: Shows in IDE and build output (default for most rules)
- **`suggestion`**: Shows only in IDE (good for style preferences)
- **`none`**: Completely disables the rule

## Selective Rule Enabling

Enable only specific rules by setting others to `none`:

```ini
[*.cs]
# Only check for DateTime and GUID issues
dotnet_diagnostic.DFA0001.severity = error
dotnet_diagnostic.DFA0002.severity = error
dotnet_diagnostic.DFA0003.severity = none
dotnet_diagnostic.DFA0004.severity = none
dotnet_diagnostic.DFA0005.severity = none
dotnet_diagnostic.DFA0006.severity = none
dotnet_diagnostic.DFA0007.severity = none
dotnet_diagnostic.DFA0008.severity = none
dotnet_diagnostic.DFA0009.severity = none
dotnet_diagnostic.DFA0010.severity = none
```

## Project-Specific Configuration

### MSBuild Properties
Configure analyzer behavior in your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Disable all analyzers in test projects -->
  <RunAnalyzersDuringBuild Condition="'$(MSBuildProjectName)' == 'MyProject.Tests'">false</RunAnalyzersDuringBuild>
  
  <!-- Treat analyzer warnings as errors in CI -->
  <TreatWarningsAsErrors Condition="'$(CI)' == 'true'">true</TreatWarningsAsErrors>
</PropertyGroup>
```

### Conditional Configuration
Different rules for different environments:

```ini
[*.cs]
# Strict rules for production code
dotnet_diagnostic.DFA0001.severity = error
dotnet_diagnostic.DFA0002.severity = error

[**/Tests/**/*.cs]
# More lenient for test code
dotnet_diagnostic.DFA0001.severity = warning
dotnet_diagnostic.DFA0002.severity = warning

[**/Samples/**/*.cs]
# Allow violations in sample code (for demonstration)
dotnet_diagnostic.DFA0001.severity = none
dotnet_diagnostic.DFA0002.severity = none
```

## Global Suppressions

### Using GlobalSuppressions.cs
Create `GlobalSuppressions.cs` in your project root:

```csharp
using System.Diagnostics.CodeAnalysis;

// Suppress specific rule globally
[assembly: SuppressMessage("DTF", "DFA0001", Justification = "Legacy code - planned refactor")]

// Suppress rule for specific namespace
[assembly: SuppressMessage("DTF", "DFA0003", Scope = "namespace", Target = "MyProject.Legacy")]

// Suppress rule for specific type
[assembly: SuppressMessage("DTF", "DFA0004", Scope = "type", Target = "MyProject.LegacyOrchestrator")]
```

### Using Pragma Directives
Suppress rules locally in code:

```csharp
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    #pragma warning disable DFA0001
    var time = DateTime.Now; // Temporarily allowed
    #pragma warning restore DFA0001
    
    return time.ToString();
}
```

### Using Attributes
Suppress rules for specific members:

```csharp
[SuppressMessage("DTF", "DFA0001", Justification = "Reviewed: Required for legacy compatibility")]
public static async Task<string> LegacyOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var time = DateTime.Now;
    return time.ToString();
}
```

## Team Configuration

### Shared EditorConfig
For teams, commit `.editorconfig` to source control:

```ini
# Team-wide DTF analyzer configuration
[*.cs]
# Enforce critical determinism rules
dotnet_diagnostic.DFA0001.severity = error
dotnet_diagnostic.DFA0002.severity = error
dotnet_diagnostic.DFA0006.severity = error
dotnet_diagnostic.DFA0010.severity = error

# Warn on other violations
dotnet_diagnostic.DFA0003.severity = warning
dotnet_diagnostic.DFA0004.severity = warning
dotnet_diagnostic.DFA0005.severity = warning
dotnet_diagnostic.DFA0007.severity = warning
dotnet_diagnostic.DFA0008.severity = warning
dotnet_diagnostic.DFA0009.severity = warning
```

### CI/CD Integration
Configure different severities for CI vs. local development:

```xml
<!-- In .csproj -->
<PropertyGroup>
  <!-- In CI, treat all warnings as errors -->
  <TreatWarningsAsErrors Condition="'$(CI)' == 'true'">true</TreatWarningsAsErrors>
  
  <!-- In local development, allow warnings -->
  <TreatWarningsAsErrors Condition="'$(CI)' != 'true'">false</TreatWarningsAsErrors>
</PropertyGroup>
```

## Advanced Configuration

### Custom Rule Sets
Create custom ruleset files for different project types:

**OrchestrationProject.ruleset:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<RuleSet Name="DTF Orchestration Rules" ToolsVersion="16.0">
  <Rules AnalyzerId="DtfDeterminismAnalyzer" RuleNamespace="DtfDeterminismAnalyzer">
    <Rule Id="DFA0001" Action="Error" />
    <Rule Id="DFA0002" Action="Error" />
    <Rule Id="DFA0003" Action="Warning" />
    <!-- ... other rules ... -->
  </Rules>
</RuleSet>
```

Reference in `.csproj`:
```xml
<PropertyGroup>
  <CodeAnalysisRuleSet>OrchestrationProject.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>
```

### Performance Tuning
For large codebases, optimize analyzer performance:

```xml
<PropertyGroup>
  <!-- Skip analyzer on restore -->
  <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
  <RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
  
  <!-- Only analyze orchestrator files -->
  <AdditionalFiles Include="**/*Orchestrator*.cs" />
</PropertyGroup>
```