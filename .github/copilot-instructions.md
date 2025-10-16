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

### 🧪 MANDATORY: Pre-Commit Testing Requirements
**NEVER commit code without running and passing ALL unit tests:**

**Required Testing Workflow:**
1. **Build verification**: `dotnet build` must succeed without errors
2. **Unit test execution**: `dotnet test` must pass with 100% success rate
3. **Coverage validation**: Minimum 90% code coverage for new/modified analyzers
4. **Performance verification**: Analyzer execution must stay under 10ms per file

**Pre-Commit Commands (MANDATORY):**
```bash
# 1. Clean build verification
dotnet clean && dotnet build

# 2. Run all unit tests (MUST pass 100%)
dotnet test --logger "console;verbosity=detailed"

# 3. Run with coverage (for new analyzer rules)
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# 4. Performance test (for analyzer changes)
dotnet run --project performance/LargeMockCodebase
```

**🚨 CRITICAL: Zero-tolerance policy for test failures**
- **If ANY test fails → Fix before committing**
- **If coverage drops below 90% → Add tests before committing**  
- **If performance exceeds 10ms → Optimize before committing**
- **If build fails → Fix compilation errors before committing**

**Automated Test Categories:**
- ✅ **Analyzer Tests**: Rule detection accuracy and edge cases
- ✅ **Code Fix Tests**: Transformation correctness and safety  
- ✅ **Performance Tests**: Large codebase analysis timing
- ✅ **Integration Tests**: End-to-end scenarios with real code
- ✅ **Regression Tests**: Previously fixed bugs stay fixed

### 🔗 MANDATORY: Conventional Commits Format
**ALWAYS use Conventional Commits format for ALL commit messages:**

**Format:**
```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Required Types:**
- `feat`: New feature (analyzer rule, code fix)
- `fix`: Bug fix  
- `docs`: Documentation changes
- `test`: Adding or updating tests
- `refactor`: Code refactoring without functionality changes
- `perf`: Performance improvements
- `style`: Code style/formatting changes
- `ci`: CI/CD pipeline changes
- `chore`: Maintenance tasks (file organization, etc.)

**Examples:**
```bash
feat(analyzer): add DFA0011 rule for async void in orchestrators
fix(DFA0001): resolve false positive with DateTime in activities
docs(rules): improve DFA0002 examples with DTF framework
test(DFA0007): add edge cases for Thread.Sleep detection
chore: reorganize root directory for better project structure
```

**⚠️ NEVER commit without following this format. When in doubt, use `chore:` for general maintenance tasks.**

### � MANDATORY: Pre-Commit Workflow Enforcement
**Every commit MUST follow this exact sequence:**

**Step 1: Code Implementation**
- Write/modify analyzer code, tests, or documentation
- Follow all coding standards and best practices

**Step 2: MANDATORY Testing (Cannot Skip)**
```bash
# Must run these commands in order:
dotnet clean
dotnet build                                    # Must pass
dotnet test --verbosity detailed               # Must pass 100%
dotnet test --collect:"XPlat Code Coverage"    # Coverage check
```

**Step 3: Verify Results**
- ✅ Build: 0 errors, 0 warnings
- ✅ Tests: 100% pass rate (no failures, no skipped)
- ✅ Coverage: ≥90% for new/modified analyzers
- ✅ Performance: <10ms per file for analyzer rules

**Step 4: Only Then Commit**
```bash
git add .
git commit -m "feat(analyzer): your conventional commit message"
# Do NOT push without explicit user permission
```

**🚫 FORBIDDEN ACTIONS:**
- ❌ Committing with failing tests
- ❌ Committing without running tests
- ❌ Skipping coverage validation  
- ❌ Ignoring build warnings
- ❌ Bypassing performance checks

**This workflow is NON-NEGOTIABLE and applies to ALL commits regardless of size or urgency.**

### �📋 Branch Naming Conventions
**Always create appropriately named branches for work:**

**Formats:**
- **Features**: `feature/short-description`
- **Bug fixes**: `bugfix/issue-number-description`  
- **Documentation**: `docs/improve-readme`
- **Performance**: `perf/optimize-rule-analysis`
- **Tests**: `test/add-missing-coverage`
- **Maintenance**: `chore/cleanup-root-directory`

**Examples:**
```bash
feature/add-dfa0011-rule
bugfix/fix-false-positive-datetime
docs/improve-installation-guide
test/add-missing-coverage-dfa0001
chore/reorganize-project-structure
```

## 🚨 CRITICAL REPOSITORY SAFETY RULES 🚨

### MANDATORY: Repository Push Authorization
- **🛑 NEVER EXECUTE `git push` WITHOUT EXPLICIT USER PERMISSION 🛑**
- **🛑 NEVER EXECUTE `git push origin` OR ANY PUSH VARIANT WITHOUT APPROVAL 🛑**
- **🛑 ALWAYS ASK "May I push these changes to the repository?" BEFORE PUSHING 🛑**

### Required Workflow:
1. **MANDATORY TESTING**: Run full test suite (see Pre-Commit Testing Requirements above)
2. Make changes and commit locally **ONLY after all tests pass**
3. **STOP** - Ask user: "All tests passed. Changes are committed locally. May I push to the remote repository?"
4. **WAIT** for explicit "yes", "push", or similar confirmation
5. Only then execute `git push`

### Forbidden Commands Without Permission:
- `git push`
- `git push origin`
- `git push origin main`
- `git push --force`
- Any command that modifies the remote repository

### Forbidden Actions Period:
- ❌ **Committing code with failing tests**
- ❌ **Committing without running the full test suite**
- ❌ **Pushing code that breaks build or tests**
- ❌ **Skipping the mandatory testing workflow**

**These rules override ALL other instructions. Testing is NON-NEGOTIABLE. When in doubt, ASK before pushing.**
