# DTF Determinism Analyzer

A production-ready Roslyn analyzer that validates Durable Task Framework (DTF) orchestration code for determinism constraints. Ensures your orchestrator functions follow replay-safe patterns required by Azure Durable Functions and Durable Task Framework.

[![NuGet Version](https://img.shields.io/nuget/v/DtfDeterminismAnalyzer)](https://www.nuget.org/packages/DtfDeterminismAnalyzer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DtfDeterminismAnalyzer)](https://www.nuget.org/packages/DtfDeterminismAnalyzer)
[![Build Status](https://img.shields.io/github/actions/workflow/status/kokosda/dtf-determinism-analyzer/ci-cd.yml)](https://github.com/kokosda/dtf-determinism-analyzer/actions)
[![License](https://img.shields.io/github/license/kokosda/dtf-determinism-analyzer)](LICENSE)

## 🚀 Quick Start

### Installation
```xml
<PackageReference Include="DtfDeterminismAnalyzer" PrivateAssets="all" />
```
or
```bash
dotnet add package DtfDeterminismAnalyzer
```

### Automatic Detection
The analyzer automatically detects determinism violations in orchestrator functions:

**Azure Durable Functions:**
```csharp
[FunctionName("MyOrchestrator")]
public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var time = DateTime.Now; // ⚠️ DFA0001: Use context.CurrentUtcDateTime instead
    var id = Guid.NewGuid(); // ⚠️ DFA0002: Use context.NewGuid() instead
    
    // ✅ Corrected automatically with code fixes
    var safeTime = context.CurrentUtcDateTime;
    var safeId = context.NewGuid();
}
```

**Durable Task Framework:**
```csharp
public static async Task<string> RunOrchestrationAsync(TaskOrchestrationContext context, string input)
{
    var time = DateTime.Now; // ⚠️ DFA0001: Use context.CurrentUtcDateTime instead
    var id = Guid.NewGuid(); // ⚠️ DFA0002: Use context.NewGuid() instead
    
    // ✅ Corrected automatically with code fixes
    var safeTime = context.CurrentUtcDateTime;
    var safeId = context.NewGuid();
}
```

### Code Fixes
Press `Ctrl+.` (Windows) or `Cmd+.` (macOS) to apply automatic fixes for common violations.

## 📖 Documentation

| Topic | Link |
|-------|------|
| **📦 Installation & Setup** | [Installation Guide](docs/installation.md) |
| **📋 All Rules & Examples** | [Complete Rules Documentation](docs/rules.md) |
| **⚙️ Configuration** | [Configuration Guide](docs/configuration.md) |
| **🔧 Troubleshooting** | [Troubleshooting Guide](docs/troubleshooting.md) |
| **� Code Fixes** | [Code Fixes Guide](docs/code-fixes.md) |
| **�💡 Code Examples** | [Complete Examples](docs/examples.md) |
| **🛠️ Local Development** | [Local Development Guide](docs/local-development.md) |

## ✅ Supported Rules

| Rule | Description | Auto-Fix |
|------|-------------|----------|
| **DFA0001** | DateTime.Now, DateTime.UtcNow, Stopwatch | ✅ |
| **DFA0002** | Guid.NewGuid() calls | ✅ |
| **DFA0003** | Random without deterministic seed | ❌ |
| **DFA0004** | Direct I/O operations | ❌ |
| **DFA0005** | Environment variable access | ❌ |
| **DFA0006** | Static mutable state access | ❌ |
| **DFA0007** | Thread.Sleep and blocking operations | ✅ |
| **DFA0008** | Non-durable async operations | ❌ |
| **DFA0009** | Threading APIs (Task.Run, Thread) | ❌ |
| **DFA0010** | Non-durable input bindings | ❌ |

## 🎯 Supported Frameworks

**Azure Durable Functions** (.NET 6+)
```csharp
[FunctionName("Orchestrator")]
public static async Task<string> Run([OrchestrationTrigger] IDurableOrchestrationContext context)
```

**Durable Task Framework** (.NET Framework 4.6.1+, .NET Core 2.1+)
```csharp
public class MyOrchestration : TaskOrchestration<string, string>
{
    public override async Task<string> RunTask(OrchestrationContext context, string input)
}
```

## 🛠️ IDE Support

- **Visual Studio** 2019+ (16.3+) and 2022
- **VS Code** with C# extension
- **JetBrains Rider** 2020.3+

## ⚙️ Quick Configuration

Create or update `.editorconfig`:

```ini
[*.cs]
# Critical determinism violations
dotnet_diagnostic.DFA0001.severity = error
dotnet_diagnostic.DFA0002.severity = error
dotnet_diagnostic.DFA0006.severity = error

# Important but non-breaking
dotnet_diagnostic.DFA0004.severity = warning
dotnet_diagnostic.DFA0007.severity = warning
```

## 📦 Sample Projects

Explore working examples in the repository:

- **[Azure Functions Sample](samples/DurableFunctionsSample/)** - Complete Azure Functions project with 40+ violations for learning
- **[DTF Sample](samples/DurableTaskSample/)** - Pure Durable Task Framework implementation

## 🤝 Contributing

We welcome contributions! See our [Contributing Guidelines](CONTRIBUTING.md) for details.

**Development Setup:**
```bash
git clone https://github.com/kokosda/dtf-determinism-analyzer.git
cd dtf-determinism-analyzer

# Quick start with automated script (cross-platform)
```powershell
.\scripts\test-codefixes.ps1
```

# Manual setup
dotnet build
dotnet test
```

For comprehensive development workflows, code fix testing, and cross-platform setup instructions, see the **[Local Development Guide](docs/local-development.md)**.

## 📋 Project Status

- ✅ **170+ Unit Tests** covering all analyzer rules
- ✅ **Automated CI/CD** pipeline with GitHub Actions  
- ✅ **NuGet Package** published and maintained
- ✅ **Security Scanning** with Dependabot and CodeQL

## 📚 Learn More

- [Durable Functions Code Constraints](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-code-constraints) - Microsoft Documentation
- [Understanding Replay Behavior](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-checkpointing-and-replay) - Why determinism matters

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

**Made with ❤️ for the DTF community**