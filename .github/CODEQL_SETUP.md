# Security Scanning Setup

This repository uses a **hybrid security approach** combining GitHub's default CodeQL setup with custom dependency security scanning.

## Current Configuration

### ✅ **Default CodeQL Setup (Automatic)**
- GitHub automatically scans the code for security vulnerabilities
- No configuration required - works out of the box
- Provides comprehensive code analysis for C# projects
- Runs on pushes and pull requests automatically

### ✅ **Custom Dependency Security (Workflow)**
- `.github/workflows/security.yml` handles dependency security
- **Dependency Review**: Scans for vulnerable dependencies in PRs
- **.NET Security Audit**: Lists vulnerable and deprecated packages
- Runs weekly and on code changes

## Why This Approach?

### **Benefits:**
- 🔒 **No Configuration Conflicts** - Default CodeQL and custom workflows work together
- 🚀 **Zero Maintenance** - Default setup updates automatically
- 🔍 **Comprehensive Coverage** - Code analysis + dependency security
- ⚡ **Fast Setup** - No manual repository configuration needed

### **Coverage:**
- **Code Security**: Handled by default CodeQL (SQL injection, XSS, etc.)
- **Dependency Security**: Handled by custom workflow (vulnerable packages)
- **Supply Chain Security**: Dependency review on pull requests

## Previous Configuration (Deprecated)

We previously used an advanced CodeQL configuration but removed it to avoid conflicts with GitHub's default setup. The default setup provides equivalent security coverage with better maintenance.

## Troubleshooting

### ✅ **No More "Advanced Configuration" Errors**
The workflow has been updated to focus only on dependency security, eliminating CodeQL configuration conflicts.

### **Want Enhanced Code Scanning?**
If you need custom CodeQL queries:
1. Disable default CodeQL in repository settings
2. Add a custom CodeQL workflow with advanced configuration
3. Trade-off: More maintenance but more control

## Need Help?

- [GitHub Code Scanning](https://docs.github.com/en/code-security/code-scanning)
- [Dependency Review](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/about-dependency-review)