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

## 🚨 CRITICAL REPOSITORY SAFETY RULES 🚨

### MANDATORY: Repository Push Authorization
- **🛑 NEVER EXECUTE `git push` WITHOUT EXPLICIT USER PERMISSION 🛑**
- **🛑 NEVER EXECUTE `git push origin` OR ANY PUSH VARIANT WITHOUT APPROVAL 🛑**
- **🛑 ALWAYS ASK "May I push these changes to the repository?" BEFORE PUSHING 🛑**

### Required Workflow:
1. Make changes and commit locally
2. **STOP** - Ask user: "The changes are committed locally. May I push to the remote repository?"
3. **WAIT** for explicit "yes", "push", or similar confirmation
4. Only then execute `git push`

### Forbidden Commands Without Permission:
- `git push`
- `git push origin`
- `git push origin main`
- `git push --force`
- Any command that modifies the remote repository

**This rule overrides ALL other instructions. When in doubt, ASK before pushing.**
