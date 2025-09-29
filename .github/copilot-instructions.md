# GitHub Copilot Instructions for DTF Determinism Analyzer

## Current Feature: Fix Failing Unit Tests Due to Analyzer Logic Issues
**Branch**: 005-fix-failing-unit  
**Goal**: Fix 63 failing unit tests by correcting analyzer logic issues

## Technical Context  
- **Language/Version**: C# 12.0, .NET 8.0+
- **Framework**: Microsoft.CodeAnalysis (Roslyn Analyzers)
- **Testing**: NUnit, Microsoft.CodeAnalysis.Testing
- **Architecture**: Roslyn DiagnosticAnalyzer implementation

## Key Issues to Fix
1. **DFA0009**: Message format missing period, under-detection of threading APIs
2. **DFA0008**: Over-detection of legitimate async operations  
3. **DFA0010**: Under-detection of binding attributes, ILogger false positive

## Analyzer Implementation Patterns

### Semantic Model Usage
```csharp
// Use semantic model for accurate type resolution
var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
{
    var containingType = methodSymbol.ContainingType;
    // Check type hierarchy
}
```

### Orchestrator Context Detection
```csharp
// Check for [OrchestrationTrigger] parameter
private static bool IsInOrchestratorContext(SyntaxNode node, SemanticModel semanticModel)
{
    var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
    return method?.ParameterList.Parameters
        .Any(p => HasOrchestrationTriggerAttribute(p, semanticModel)) == true;
}
```

### Diagnostic Reporting
```csharp
// Report diagnostic with exact location
var diagnostic = Diagnostic.Create(
    DiagnosticDescriptors.RuleName,
    location,
    messageFormat);
context.ReportDiagnostic(diagnostic);
```

## Recent Changes (Last 3 Features)
1. **004-fix-failing-unit**: Fixed test framework assembly references
2. **003-create-determinism**: Initial analyzer implementation  
3. **005-fix-failing-unit**: Fixing remaining test failures (CURRENT)

## Code Quality Standards
- Follow existing analyzer patterns in codebase
- Maintain backward compatibility with rule IDs
- Use semantic model analysis over syntax-only analysis
- Ensure exact diagnostic message formatting
- Comprehensive unit test coverage

## Priority Order
1. Fix DFA0009 message format (quick win)
2. Fix DFA0008 over-detection issues
3. Fix DFA0009 under-detection issues  
4. Fix DFA0010 binding detection issues
