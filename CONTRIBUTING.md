# Contributing to DTF Determinism Analyzer

Thank you for your interest in contributing to DTF Determinism Analyzer! 🎉 

We welcome contributions from developers of all skill levels. Whether you're fixing a bug, adding a new analyzer rule, improving documentation, or sharing feedback, your contribution helps make durable orchestrations more reliable for everyone.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Ways to Contribute](#ways-to-contribute)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Adding New Analyzer Rules](#adding-new-analyzer-rules)
- [Testing Guidelines](#testing-guidelines)
- [Documentation Guidelines](#documentation-guidelines)
- [Code Style and Standards](#code-style-and-standards)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Performance Guidelines](#performance-guidelines)
- [Release Process](#release-process)
- [Getting Help](#getting-help)

## Code of Conduct

This project follows the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/about/code-of-conduct). By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

**In short: Be respectful, be inclusive, and help create a welcoming environment for everyone.**

## Ways to Contribute

### 🐛 Bug Reports
- Report analyzer false positives or false negatives
- Report performance issues
- Report documentation errors or unclear explanations

### 💡 Feature Requests  
- Propose new analyzer rules for DTF determinism
- Suggest improvements to existing rules
- Request new code fix providers

### 💻 Code Contributions
- Fix bugs in existing analyzers
- Implement new analyzer rules
- Add code fix providers
- Improve performance
- Add or improve tests

### 📚 Documentation
- Improve rule documentation with better examples
- Add troubleshooting guides
- Translate documentation
- Write blog posts or tutorials

### 🧪 Testing
- Test the analyzer with real-world projects
- Report compatibility issues
- Improve test coverage

## Getting Started

### Prerequisites

- **.NET SDK 8.0+** (for development)
- **Visual Studio 2022** (17.0+) or **VS Code** with C# extension
- **Git** for version control

### Development Setup

For comprehensive development setup including cross-platform support, code fix testing, and various development workflows, see the **[Local Development Guide](docs/local-development.md)**.

**Quick Setup:**

1. **Fork and Clone**
   ```bash
   git clone https://github.com/your-username/dtf-determinism-analyzer.git
   cd dtf-determinism-analyzer
   ```

2. **Automated Development Setup**
   ```bash
   # Cross-platform script for code fix testing
   .\scripts\test-codefixes.ps1
   ```

3. **Manual Setup**
   ```bash
   dotnet restore
   ```

3. **Build the Solution**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

5. **Verify Everything Works**
   - Open the solution in your IDE
   - Build should complete without errors
   - All tests should pass
   - Sample projects should build with expected analyzer warnings

### Repository Structure

```
├── src/
│   ├── DtfDeterminismAnalyzer/              # Main analyzer project
│   │   ├── Analyzers/                       # Individual analyzer implementations
│   │   ├── CodeFixes/                       # Code fix providers
│   │   └── DiagnosticDescriptors.cs         # Rule definitions
│   └── DtfDeterminismAnalyzer.CodeFixes/    # Code fixes package
├── tests/
│   └── DtfDeterminismAnalyzer.Tests/        # Unit tests
├── samples/
│   ├── DurableFunctionsSample/              # Azure Functions examples
│   └── DurableTaskSample/                   # DTF examples
├── docs/                                    # Documentation
├── performance/                             # Performance testing
└── .github/                                 # GitHub templates and workflows
```

## Development Workflow

### Branch Strategy

- **`main`**: Production-ready code, protected branch
- **`develop`**: Integration branch for features (if used)
- **Feature branches**: `feature/add-dfa0011-rule`, `bugfix/fix-false-positive`
- **Release branches**: `release/1.1.0` (for release preparation)

### Branch Naming Conventions

- **Features**: `feature/short-description`
- **Bug fixes**: `bugfix/issue-number-description`
- **Documentation**: `docs/improve-readme`
- **Performance**: `perf/optimize-rule-analysis`
- **Tests**: `test/add-missing-coverage`

### Commit Message Conventions

We use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: New feature (analyzer rule, code fix)
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Adding or updating tests
- `refactor`: Code refactoring without functionality changes
- `perf`: Performance improvements
- `style`: Code style/formatting changes
- `ci`: CI/CD pipeline changes

**Examples:**
```bash
feat(analyzer): add DFA0011 rule for async void in orchestrators
fix(DFA0001): resolve false positive with DateTime in activities  
docs(rules): improve DFA0002 examples with DTF framework
test(DFA0007): add edge cases for Thread.Sleep detection
```

## Adding New Analyzer Rules

### Rule ID Conventions

- **Format**: `DFA####` (DTF Analyzer + 4-digit number)
- **Next available**: Check existing rules and use the next sequential number
- **Categories**: Group related rules (time=0001-0010, threading=0007-0009, etc.)

### Step-by-Step Process

1. **Plan the Rule**
   - Identify the non-deterministic pattern
   - Research DTF/Azure Functions documentation
   - Check if similar rules exist in other analyzers
   - Define scope: Azure Functions only, DTF only, or both

2. **Create the Analyzer**
   ```bash
   # Create new file: src/DtfDeterminismAnalyzer/Analyzers/DFA0011Analyzer.cs
   ```

3. **Add Diagnostic Descriptor**
   ```csharp
   // In src/DtfDeterminismAnalyzer/DiagnosticDescriptors.cs
   public static readonly DiagnosticDescriptor DFA0011 = new(
       id: "DFA0011",
       title: "Don't use async void in orchestrators",
       messageFormat: "Async void method '{0}' should return Task in orchestrator context",
       category: Categories.Determinism,
       defaultSeverity: DiagnosticSeverity.Warning,
       isEnabledByDefault: true,
       helpLinkUri: "https://github.com/kokosda/dtf-determinism-analyzer/docs/rules.md#dfa0011");
   ```

4. **Implement the Analyzer**
   ```csharp
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   public class DFA0011Analyzer : DiagnosticAnalyzer
   {
       public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
           ImmutableArray.Create(DiagnosticDescriptors.DFA0011);

       public override void Initialize(AnalysisContext context)
       {
           context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
           context.EnableConcurrentExecution();
           context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
       }

       private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
       {
           // Implementation here
       }
   }
   ```

5. **Add Code Fix (if applicable)**
   ```bash
   # Create: src/DtfDeterminismAnalyzer.CodeFixes/DFA0011CodeFixProvider.cs
   ```

6. **Write Comprehensive Tests**
   ```csharp
   // In tests/DtfDeterminismAnalyzer.Tests/DFA0011AnalyzerTests.cs
   ```

7. **Update Documentation**
   - Add to `docs/rules.md`
   - Update rule summary tables
   - Add examples for both Azure Functions and DTF

8. **Update Samples**
   - Add violations to sample projects (for educational purposes)
   - Verify the analyzer detects them correctly

### Analyzer Implementation Best Practices

**Performance:**
- Use `SyntaxNodeAction` over `SemanticModelAction` when possible
- Cache expensive computations
- Avoid allocations in hot paths
- Use `context.EnableConcurrentExecution()`

**Accuracy:**
- Use semantic model for type checking: `context.SemanticModel.GetSymbolInfo()`
- Check method signatures, not just names
- Verify orchestrator context before reporting diagnostics
- Handle edge cases (generics, inheritance, etc.)

**Maintainability:**
- Keep analyzers focused on single rules
- Use helper methods from `AnalysisContextExtensions`
- Follow existing code patterns in the project
- Add comprehensive XML documentation

## Testing Guidelines

### Test Structure

We use **NUnit** with **Microsoft.CodeAnalysis.Testing** framework:

```csharp
[TestFixture]
public class DFA0011AnalyzerTests
{
    [Test]
    public async Task AsyncVoidInOrchestrator_ShouldTriggerWarning()
    {
        const string testCode = @"
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

public class TestOrchestrator
{
    [FunctionName(""TestOrchestrator"")]
    public static async void RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        // Should trigger DFA0011
    }
}";

        var expected = Diagnostic(DiagnosticDescriptors.DFA0011)
            .WithLocation(8, 30)
            .WithArguments("RunOrchestrator");

        await VerifyCSharpDiagnosticAsync(testCode, expected);
    }
}
```

### Test Categories

1. **Positive Tests**: Verify rule triggers when it should
2. **Negative Tests**: Verify rule doesn't trigger false positives
3. **Edge Cases**: Inheritance, generics, complex scenarios
4. **Both Frameworks**: Test Azure Functions AND DTF patterns
5. **Code Fix Tests**: Verify automatic fixes work correctly

### Test Requirements

- **Minimum 90% code coverage** for new analyzers
- **Test both positive and negative cases**
- **Include edge cases** and complex scenarios
- **Test performance** with large code samples
- **Verify diagnostic locations** are precise
- **Test code fixes** produce correct output

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=DFA0011AnalyzerTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Documentation Guidelines

### Rule Documentation

Each rule must include:

1. **Clear title and description**
2. **Problem explanation** with why it matters for determinism
3. **Before/after code examples** for both Azure Functions and DTF
4. **Detailed explanation** of the fix
5. **Edge cases** and considerations

### Documentation Standards

- **Use consistent formatting** following existing docs
- **Provide working code examples** that compile
- **Include both frameworks** when applicable
- **Add troubleshooting** for common issues
- **Keep examples realistic** and practical

### Adding Documentation

1. **Rule Documentation**: Add detailed examples to `docs/rules.md`
2. **Configuration**: Update `docs/configuration.md` if new settings added
3. **Troubleshooting**: Add common issues to `docs/troubleshooting.md`
4. **Examples**: Add complete examples to `docs/examples.md`

## Code Style and Standards

### C# Coding Standards

We follow [.NET Runtime Coding Style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md):

- **PascalCase**: Public members, types, namespaces
- **camelCase**: Private fields, local variables, parameters
- **Prefix private fields** with underscore: `_privateField`
- **Use `var`** when type is obvious
- **Prefer expression bodies** for simple members
- **Use nullable reference types** where appropriate

### EditorConfig

The project includes `.editorconfig` with formatting rules:
- **4 spaces** for indentation (no tabs)
- **UTF-8** encoding
- **LF** line endings
- **Trim trailing whitespace**
- **Insert final newline**

### Analyzer-Specific Standards

```csharp
// ✅ Good analyzer implementation
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DFA0001Analyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(DiagnosticDescriptors.DFA0001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (!OrchestrationContextDetector.IsInOrchestratorMethod(context))
            return;

        // Analyzer logic here
    }
}
```

### Code Review Checklist

- [ ] Follows existing code style and patterns
- [ ] Includes comprehensive tests with good coverage
- [ ] Updates documentation appropriately  
- [ ] Handles error cases gracefully
- [ ] Performance considerations addressed
- [ ] Compatible with both Azure Functions and DTF (when applicable)
- [ ] Diagnostic messages are clear and actionable

## Pull Request Process

### Before Submitting

1. **Create an issue** for discussion (unless it's a trivial fix)
2. **Fork the repository** and create a feature branch
3. **Write tests** before implementing (TDD approach encouraged)
4. **Ensure all tests pass** locally
5. **Update documentation** as needed
6. **Test with sample projects** to verify behavior

### PR Requirements

- [ ] **Descriptive title** following conventional commits
- [ ] **Clear description** of what changes and why
- [ ] **Link to related issue** (use "Closes #123" or "Fixes #123")
- [ ] **All tests passing** (CI will verify this)
- [ ] **Documentation updated** if needed
- [ ] **No merge conflicts** with main branch

### PR Template

```markdown
## Description
Brief description of changes and motivation.

## Related Issue
Closes #123

## Type of Change
- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that causes existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] Added tests for new functionality
- [ ] All existing tests pass
- [ ] Tested with sample projects

## Documentation
- [ ] Updated rule documentation
- [ ] Updated configuration docs (if applicable)
- [ ] Updated troubleshooting guide (if applicable)
```

### Review Process

1. **Automated checks** must pass (build, tests, code analysis)
2. **At least one maintainer review** required
3. **Address feedback** and push updates
4. **Squash merge** when approved (maintainers will handle this)

## Issue Reporting

### Bug Reports

Use the GitHub issue templates and include:

- **Clear description** of the problem
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Environment information**:
  - OS and version
  - .NET SDK version  
  - IDE and version
  - Analyzer package version
- **Minimal code sample** that reproduces the issue
- **Relevant configuration** files (`.editorconfig`, project file)

### Feature Requests

- **Describe the use case** and problem being solved
- **Explain the proposed solution**
- **Provide examples** of the desired behavior
- **Consider backward compatibility** implications
- **Research similar features** in other analyzers

### Security Issues

**DO NOT** create public issues for security vulnerabilities. Instead:
1. Email the maintainers directly
2. Include detailed description and reproduction steps
3. Allow reasonable time for response before public disclosure

## Performance Guidelines

Analyzers run during compilation and must be performant:

### Performance Requirements

- **No more than 10ms** average execution time per file
- **Linear or better** time complexity relative to file size
- **Minimal memory allocations** in hot paths
- **No blocking I/O operations**

### Performance Best Practices

```csharp
// ✅ Good: Use syntax-only analysis when possible
context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);

// ❌ Avoid: Semantic model analysis unless necessary
context.RegisterSemanticModelAction(AnalyzeSemanticModel);

// ✅ Good: Cache expensive operations
private static readonly Lazy<ISet<string>> BlockedMethods = new(() => 
    new HashSet<string> { "DateTime.get_Now", "DateTime.get_UtcNow" });

// ✅ Good: Early exit from analysis
if (!memberAccess.Expression.ToString().Contains("DateTime"))
    return; // Quick syntax check before semantic analysis
```

### Performance Testing

Run performance tests before submitting:

```bash
# Run performance benchmarks
cd performance
./run_performance_tests.sh
```

## Release Process

### Versioning

We follow [Semantic Versioning](https://semver.org/):
- **Major** (1.0.0): Breaking changes to public API
- **Minor** (1.1.0): New features, new analyzer rules
- **Patch** (1.0.1): Bug fixes, performance improvements

### Release Checklist

See `.github/ISSUE_TEMPLATE/release-checklist.md` for detailed steps.

### NuGet Publishing

Releases are automated via GitHub Actions when tags are created:

```bash
# Create and push release tag
git tag v1.1.0
git push origin v1.1.0
```

## Getting Help

### Documentation
- **[Project Documentation](docs/)** - Comprehensive guides
- **[Rule Documentation](docs/rules.md)** - All analyzer rules with examples
- **[Troubleshooting Guide](docs/troubleshooting.md)** - Common issues and solutions

### Community
- **GitHub Discussions** - Ask questions, share ideas, get help
- **GitHub Issues** - Report bugs, request features
- **Stack Overflow** - Use tag `dtf-determinism-analyzer`

### Maintainers

For urgent issues or questions:
- Create a GitHub issue with `question` label
- Join discussions in existing issues/PRs
- Email maintainers for security issues only

---

## Recognition

Contributors are recognized in:
- **README.md** - Major contributors listed
- **Release notes** - Contributors credited for each release  
- **All Contributors** - Bot that tracks all types of contributions

## License

By contributing to DTF Determinism Analyzer, you agree that your contributions will be licensed under the same [MIT License](LICENSE) that covers the project.

---

**Thank you for contributing to DTF Determinism Analyzer!** 🚀

Your efforts help make durable orchestrations more reliable and deterministic for developers worldwide.