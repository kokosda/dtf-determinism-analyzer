# GitHub Copilot Instructions for DTF Determinism Analyzer

## Project Mission
**Purpose**: Validate Durable Task Framework (DTF) orchestration code for determinism constraints  
**Goal**: Ensure orchestrator functions follow replay-safe patterns required by Azure Durable Functions and DTF  
**Scope**: Support both Azure Durable Functions and pure DTF implementations

## Technical Context  
- **Language/Version**: C# 12.0, .NET 8.0+
- **Framework**: Microsoft.CodeAnalysis (Roslyn Analyzers)
- **Testing**: NUnit, Microsoft.CodeAnalysis.Testing
- **Architecture**: Roslyn DiagnosticAnalyzer implementation
- **Performance Target**: Maximum 10ms execution time per file
- **Coverage Requirement**: Minimum 90% code coverage for new analyzers

## Development Principles (Follow CONTRIBUTING.md)

### Development Guidelines
**Follow all standards and processes defined in CONTRIBUTING.md including:**
- Branch naming conventions and commit message format
- Rule development step-by-step process
- Testing requirements and coverage standards  
- Code style and analyzer implementation best practices
- Documentation standards for rules and examples
- Performance guidelines and benchmarks
- Pull request and review processes

## Repository Rules
- **NEVER push to repository without explicit instruction** - Always wait for user confirmation before executing `git push` or similar commands that modify the remote repository
