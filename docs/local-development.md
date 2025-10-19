# Local Development Guide

> **End Users:** For configuring the analyzer in your projects, see the [Configuration Guide](configuration.md) for rule severity and suppression options.

This guide covers all aspects of local development for the DTF Determinism Analyzer, including cross-platform setup, code fix testing, and various development workflows.

## Quick Start

### 1. Repository Setup

```bash
# Clone the repository
git clone https://github.com/kokosda/dtf-determinism-analyzer.git
cd dtf-determinism-analyzer

# Add local NuGet source
dotnet nuget add source "./local-nuget-packages" --name "Local"
```

### 2. Test Code Fixes (Any Platform)

```powershell
# From repository root - works on Windows, macOS, and Linux
.\scripts\test-codefixes.ps1

# On Linux/macOS, you might need:
pwsh ./scripts/test-codefixes.ps1
```

## Development Workflows

### Option 1: Automated NuGet Testing (Recommended)

**Best for:** Visual Studio code fix testing, production-like experience

```powershell
# Make your code fix changes
# Then run the automated script
.\scripts\test-codefixes.ps1

# With custom version
.\scripts\test-codefixes.ps1 -Version "1.0.4-myfeature"
```

**What it does:**
1. ✅ Auto-detects repository root (works from any subdirectory)
2. ✅ Builds & packs analyzer with unique timestamp version
3. ✅ Updates `Directory.Packages.props` with new version
4. ✅ Clears NuGet cache and restores packages
5. ✅ Ready for Visual Studio testing immediately

### Option 2: Project References (Fast Development)

**Best for:** Daily development, fast iteration, analyzer logic changes

```xml
<!-- In sample projects - for development -->
<ItemGroup>
  <ProjectReference Include="../../src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

**Limitations:**
- ❌ Code fixes may not appear in Visual Studio 2022
- ✅ Analyzers still detect and report errors
- ✅ Works fine in VS Code and command line builds

### Option 3: Hybrid Approach

**Best for:** Balanced development with targeted code fix testing

1. **Daily Development**: Use project references (faster builds)
2. **Code Fix Development**: Use unit tests for logic verification  
3. **Integration Testing**: Use `.\scripts\test-codefixes.ps1` for Visual Studio testing
4. **Final Validation**: Test with NuGet package before release

## Cross-Platform Support

### Automatic Repository Root Detection
The script automatically finds the repository root by looking for:
- `.git` directory
- `Directory.Packages.props` file

### Platform-Agnostic Paths
All paths are constructed using `Join-Path` for cross-platform compatibility:
```powershell
$repoRoot = Find-RepositoryRoot
$analyzerPath = Join-Path $repoRoot "src" "DtfDeterminismAnalyzer"
$localPackagePath = Join-Path $repoRoot "local-nuget-packages"
```

### Repository Structure
```
dtf-determinism-analyzer/
├── src/
│   ├── DtfDeterminismAnalyzer/
│   └── DtfDeterminismAnalyzer.CodeFixes/
├── samples/
│   ├── DurableFunctionsSample/
│   └── DurableTaskSample/
├── scripts/
│   └── test-codefixes.ps1             # Cross-platform development script
├── local-nuget-packages/          # Created automatically
│   └── *.nupkg                    # Local test packages
└── Directory.Packages.props       # Central package management
```

## Platform-Specific Instructions

### Windows
```powershell
# PowerShell (recommended)
.\scripts\test-codefixes.ps1

# Command Prompt
powershell -ExecutionPolicy Bypass -File scripts\test-codefixes.ps1
```

### macOS/Linux
```bash
# Install PowerShell first
# macOS: brew install powershell
# Ubuntu: snap install powershell --classic

# Run the script
pwsh ./scripts/test-codefixes.ps1
```

### VS Code (All Platforms)
1. Install PowerShell extension
2. Open integrated terminal
3. Run `.\scripts\test-codefixes.ps1`

## Manual Development Workflow

If you prefer manual control over the process:

### Step-by-Step Process

#### 1. Make Code Fix Changes
Edit your code fixes in `src/DtfDeterminismAnalyzer.CodeFixes/`

#### 2. Build & Pack
```powershell
# From repository root
cd src/DtfDeterminismAnalyzer
dotnet pack -c Release -p:PackageVersion=1.0.2-dev.1 -o "../../local-nuget-packages"
```

#### 3. Update Package Version
Edit `Directory.Packages.props`:
```xml
<PackageVersion Include="DtfDeterminismAnalyzer" Version="1.0.2-dev.1" />
```

#### 4. Clear Cache & Restore
```powershell
dotnet nuget locals all --clear
dotnet restore
```

#### 5. Test in Visual Studio
1. Close Visual Studio
2. Reopen Visual Studio
3. Open `samples\DtfSamples.sln`
4. Test your code fixes

### Version Management
For each code fix change, increment the version:
- `1.0.2-dev.1`
- `1.0.2-dev.2`
- `1.0.2-dev.3`
- etc.

## Testing Approaches

### 1. Visual Studio Integration Testing
```powershell
# Use automated script for best experience
.\scripts\test-codefixes.ps1

# Then open Visual Studio 2022
# Navigate to ProblematicOrchestrator.cs
# Press Ctrl+. on analyzer errors
```

### 2. Unit Test Development
Most code fix testing can be done through unit tests:

```csharp
[Test]
public async Task DateTime_Now_ShouldProvideCodeFix()
{
    var test = new CodeFixTest<DateTimeAnalyzer, DateTimeCodeFixProvider, NUnitVerifier>
    {
        TestCode = @"
            var now = DateTime.Now;  // Should trigger DFA0001
        ",
        FixedCode = @"
            var now = context.CurrentUtcDateTime;  // Should be the fix
        "
    };
    
    await test.RunAsync();
}
```

### 3. Command Line Verification
```powershell
# Build and test all at once
dotnet test --logger "console;verbosity=detailed" | Select-String "CodeFix"
```

## IDE Integration

### Visual Studio 2022
1. Run `.\scripts\test-codefixes.ps1`
2. Open `samples\DtfSamples.sln`
3. Code fixes available via Ctrl+.

### VS Code
1. Run `.\scripts\test-codefixes.ps1`
2. Open workspace or folder
3. Use C# extension for IntelliSense
4. Code fixes available through Quick Fix (Ctrl+.)

### JetBrains Rider
1. Run `.\scripts\test-codefixes.ps1`
2. Open solution
3. Code fixes available through Alt+Enter

## Troubleshooting

### Script Execution Policy (Windows)
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### PowerShell Not Found (macOS/Linux)
```bash
# Install PowerShell
curl -sSL https://aka.ms/install-powershell.sh | bash
```

### NuGet Source Issues
```powershell
# Remove and re-add local source
dotnet nuget remove source "Local"
dotnet nuget add source "./local-nuget-packages" --name "Local"
```

### Repository Root Not Found
Ensure you're running the script from within the repository directory tree. The script looks for `.git` or `Directory.Packages.props` to locate the root.

### Code Fixes Not Appearing
1. **Use NuGet package approach** instead of project references
2. **Run** `.\scripts\test-codefixes.ps1` to ensure proper setup
3. **Close and reopen** Visual Studio after package updates
4. **Check** that the latest package version is being used

### Build Failures
```powershell
# Clean and rebuild
dotnet clean
dotnet build

# Clear all caches
dotnet nuget locals all --clear
```

## Performance Tips

### Fast Development Iteration
- Use project references for analyzer development
- Use unit tests for code fix logic verification
- Only use NuGet packages when testing Visual Studio integration

### Automated Testing
- Set up the script once: `.\scripts\test-codefixes.ps1`
- Use timestamp-based versions for unique packages
- Let the script handle cache clearing and restoration

### Version Management
- Use meaningful version suffixes: `1.0.2-fix-datetime`
- Increment patch versions for releases: `1.0.3`, `1.0.4`
- Use dev versions for testing: `1.0.2-dev001`

## Best Practices

### Development Workflow
1. **Start with unit tests** for code fix logic
2. **Use project references** for fast analyzer iteration
3. **Switch to NuGet packages** for Visual Studio testing
4. **Validate with automated script** before commits

### Code Fix Development
1. **Write failing unit test** first
2. **Implement code fix** logic
3. **Run unit tests** to verify logic
4. **Test in Visual Studio** using `.\scripts\test-codefixes.ps1`
5. **Verify user experience** with real code samples

### Version Control
1. **Never commit** local package files (`.nupkg`)
2. **Reset package version** in `Directory.Packages.props` before commits
3. **Document breaking changes** in code fix behavior
4. **Tag releases** with semantic versions

This comprehensive guide covers all aspects of local development, from quick setup to advanced workflows, ensuring a smooth development experience across all platforms.