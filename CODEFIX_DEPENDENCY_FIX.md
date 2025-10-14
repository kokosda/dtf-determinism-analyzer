# CodeFix Dependency Fix

## Issue
Visual Studio 2022 was not suggesting code fixes for DFA analyzer rules due to dependency loading issues.

## Root Cause
The `DtfDeterminismAnalyzer.CodeFixes.dll` assembly could not load at runtime in Visual Studio because:

1. Code fix providers depend on `Microsoft.CodeAnalysis.Workspaces` assemblies
2. These dependencies were marked with `PrivateAssets="all"` so they weren't included in the NuGet package
3. Visual Studio couldn't resolve the dependencies, causing MEF composition to fail
4. Result: Code fix providers were not discoverable/loadable

## Solution
Modified `src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj` to include the required runtime dependencies in the NuGet package:

```xml
<!-- Include required CodeAnalysis.Workspaces dependencies for code fixes -->
<None Include="$(NuGetPackageRoot)microsoft.codeanalysis.workspaces.common/4.8.0/lib/netstandard2.0/Microsoft.CodeAnalysis.Workspaces.dll" Pack="true" PackagePath="analyzers\dotnet\cs\Microsoft.CodeAnalysis.Workspaces.dll" Visible="false" />
<None Include="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.workspaces/4.8.0/lib/netstandard2.0/Microsoft.CodeAnalysis.CSharp.Workspaces.dll" Pack="true" PackagePath="analyzers\dotnet\cs\Microsoft.CodeAnalysis.CSharp.Workspaces.dll" Visible="false" />

<!-- Include System.Composition dependency for MEF exports -->
<None Include="$(NuGetPackageRoot)system.composition.attributedmodel/9.0.9/lib/netstandard2.0/System.Composition.AttributedModel.dll" Pack="true" PackagePath="analyzers\dotnet\cs\System.Composition.AttributedModel.dll" Visible="false" />
```

## Verification
The updated NuGet package now includes:
- `DtfDeterminismAnalyzer.dll` (88KB)
- `DtfDeterminismAnalyzer.CodeFixes.dll` (33KB)  
- `Microsoft.CodeAnalysis.Workspaces.dll` (5.6MB)
- `Microsoft.CodeAnalysis.CSharp.Workspaces.dll` (1.1MB)
- `System.Composition.AttributedModel.dll` (21KB)

All assemblies can now load successfully, enabling Visual Studio to discover and offer the code fixes.

## Test
After installing the updated package, Visual Studio should now show code fix suggestions for:
- DFA0001: DateTime/Stopwatch API violations
- DFA0002: Guid.NewGuid() violations  
- DFA0007: Thread.Sleep violations