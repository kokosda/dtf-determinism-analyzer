# Troubleshooting Guide

## Common Issues and Solutions

### Analyzer Not Running

**Symptoms:**
- No warnings appear for known violations
- Rules don't show up in IDE
- Analyzer seems completely inactive

**Solutions:**

1. **Check Package Installation**
   ```xml
   <!-- Ensure PrivateAssets="all" is set -->
   <PackageReference Include="DtfDeterminismAnalyzer" Version="1.0.0" PrivateAssets="all" />
   ```

2. **Restart IDE**
   - Close and reopen Visual Studio/VS Code/Rider
   - Clear analyzer cache: Delete `bin/` and `obj/` folders, then rebuild

3. **Verify Target Framework**
   - Ensure you're targeting a supported .NET version (.NET Framework 4.6.1+, .NET Core 2.1+, .NET 5+)
   - Check that Roslyn analyzers are supported in your project type

4. **Check Project Type**
   - Analyzers work in SDK-style projects
   - For legacy projects, ensure `<Analyzer>` references are properly configured

### Rules Not Triggering

**Symptoms:**
- Analyzer is active but specific rules don't trigger
- Expected violations are not detected

**Solutions:**

1. **Verify Orchestrator Detection**
   ```csharp
   // Ensure proper attributes are used
   [FunctionName("MyOrchestrator")]
   public static async Task<string> MyOrchestrator(
       [OrchestrationTrigger] IDurableOrchestrationContext context) // Must have this attribute
   {
       var time = DateTime.Now; // Should trigger DFA0001
   }
   ```

2. **Check Method Signatures**
   - Azure Functions: Must have `[OrchestrationTrigger]` parameter
   - DTF: Must inherit from `TaskOrchestration<TResult, TInput>`

3. **Verify Rule Configuration**
   ```ini
   [*.cs]
   # Make sure rule isn't disabled
   dotnet_diagnostic.DFA0001.severity = warning  # Not 'none'
   ```

### False Positives

**Symptoms:**
- Analyzer flags code that should be allowed
- Violations reported outside orchestrator functions

**Solutions:**

1. **Check Context Detection**
   - Ensure the analyzer correctly identifies orchestrator vs. non-orchestrator code
   - Verify attribute detection logic is working

2. **Use Selective Suppression**
   ```csharp
   [SuppressMessage("DTF", "DFA0001", Justification = "Reviewed: Safe in this context")]
   public static async Task<string> SpecialCase([OrchestrationTrigger] IDurableOrchestrationContext context)
   {
       var time = DateTime.Now; // Suppressed for valid reason
   }
   ```

3. **Configure Rule Severity**
   ```ini
   [*.cs]
   # Reduce severity if needed
   dotnet_diagnostic.DFA0001.severity = suggestion
   ```

### Code Fixes Not Available

**Symptoms:**
- No lightbulb icon appears
- Code fix suggestions don't show up
- Quick actions menu is empty

**Solutions:**

1. **Check IDE Support**
   - Visual Studio: Press `Ctrl+.` or click lightbulb
   - VS Code: Press `Ctrl+.` (Windows) / `Cmd+.` (macOS)
   - Rider: Press `Alt+Enter`

2. **Verify Fix Availability**
   - Only some rules have automatic fixes (DFA0001, DFA0002, DFA0007)
   - Complex violations require manual fixes

3. **Restart Language Service**
   - VS Code: Reload window (`Ctrl+Shift+P` → "Developer: Reload Window")
   - Visual Studio: Close/reopen solution

4. **Visual Studio Project Reference Issues**
   - See the [Code Fixes Guide](code-fixes.md) for comprehensive troubleshooting
   - Code fixes may not work with project references - use NuGet packages instead
   - Rider: Invalidate caches and restart

### Performance Issues

**Symptoms:**
- Slow build times
- IDE becomes unresponsive
- High CPU usage during analysis

**Solutions:**

1. **Optimize Configuration**
   ```xml
   <PropertyGroup>
     <!-- Reduce analysis scope -->
     <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
     <RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
   </PropertyGroup>
   ```

2. **Exclude Non-Orchestrator Projects**
   ```xml
   <!-- In test projects or utility libraries -->
   <PropertyGroup>
     <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
   </PropertyGroup>
   ```

3. **Use Selective Rules**
   ```ini
   [*.cs]
   # Enable only critical rules
   dotnet_diagnostic.DFA0001.severity = error
   dotnet_diagnostic.DFA0002.severity = error
   # Disable less critical rules
   dotnet_diagnostic.DFA0003.severity = none
   ```

### Version Compatibility Issues

**Symptoms:**
- Analyzer works in some projects but not others
- Inconsistent behavior across team members

**Solutions:**

1. **Check .NET SDK Version**
   ```bash
   dotnet --version  # Should be 3.1+ for best compatibility
   ```

2. **Verify Package Versions**
   ```xml
   <!-- Use Directory.Packages.props for consistency -->
   <PackageVersion Include="DtfDeterminismAnalyzer" Version="1.0.0" />
   ```

3. **Update Dependencies**
   ```bash
   dotnet update  # Update to latest compatible versions
   ```

## IDE-Specific Issues

### Visual Studio Issues

**Problem:** Analyzer not showing in Error List
**Solution:** 
- Go to Tools → Options → Text Editor → C# → Advanced
- Check "Enable full solution analysis"
- Restart Visual Studio

**Problem:** Code fixes not working in Visual Studio 2019
**Solution:**
- Update to latest version (16.3+)
- Install latest C# language extension

### VS Code Issues

**Problem:** Analyzer warnings not appearing
**Solution:**
- Install/update C# extension by Microsoft
- Check Output panel → C# for error messages
- Restart OmniSharp: `Ctrl+Shift+P` → "OmniSharp: Restart OmniSharp"

### Rider Issues

**Problem:** Rules not enforced during build
**Solution:**
- Go to File → Settings → Build, Execution, Deployment → Toolset and Build
- Ensure "Use MSBuild version" matches project requirements
- Clear caches: File → Invalidate Caches and Restart

## Advanced Troubleshooting

### Enable Analyzer Logging

Add to your project file to enable detailed logging:

```xml
<PropertyGroup>
  <ReportAnalyzer>true</ReportAnalyzer>
</PropertyGroup>
```

### MSBuild Diagnostics

Run MSBuild with detailed verbosity to see analyzer issues:

```bash
dotnet build -v detailed | findstr -i analyzer
```

### Check Analyzer Assembly Loading

Verify the analyzer is being loaded:

```xml
<PropertyGroup>
  <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
  <ReportAnalyzer>true</ReportAnalyzer>
</PropertyGroup>
```

Look for messages like:
```
Loading analyzer assembly: DtfDeterminismAnalyzer.dll
```

## Getting Help

If you're still experiencing issues:

1. **Check Existing Issues**: [GitHub Issues](https://github.com/kokosda/dtf-determinism-analyzer/issues)
2. **Create New Issue**: Include:
   - Operating System and version
   - IDE and version
   - .NET SDK version (`dotnet --version`)
   - Minimal reproducible example
   - Relevant configuration files (`.editorconfig`, project file)
3. **Join Discussions**: [GitHub Discussions](https://github.com/kokosda/dtf-determinism-analyzer/discussions)

### Issue Template

When reporting issues, please include:

```
**Environment:**
- OS: Windows 11 / macOS Monterey / Ubuntu 22.04
- IDE: Visual Studio 2022 (17.4.0) / VS Code (1.74.0) / Rider (2022.3)
- .NET SDK: 6.0.404
- Analyzer Version: 1.0.0

**Problem Description:**
[Describe what's not working]

**Expected Behavior:**
[What should happen]

**Actual Behavior:**
[What actually happens]

**Reproduction Steps:**
1. Create new project
2. Install analyzer
3. Add orchestrator code
4. Expected warning doesn't appear

**Configuration Files:**
[Include relevant .editorconfig, .csproj, etc.]
```