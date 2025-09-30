# CodeQL Setup Instructions

This repository uses **advanced CodeQL configuration** with enhanced security queries (`security-extended` and `security-and-quality`) for comprehensive security analysis.

## Resolving CodeQL Configuration Conflicts

If you encounter this error:
```
Error: Code Scanning could not process the submitted SARIF file:
CodeQL analyses from advanced configurations cannot be processed when the default setup is enabled
```

### Solution: Disable Default CodeQL Setup

1. **Navigate to Repository Settings:**
   - Go to your repository on GitHub
   - Click **Settings** tab
   - Select **Code security and analysis** from the sidebar

2. **Disable Default Setup:**
   - Find the **Code scanning** section
   - Under **CodeQL analysis**, click **Set up** → **Advanced**
   - Or if already enabled, click **Edit** → **Switch to advanced**
   - This disables the default setup and allows the custom workflow to run

3. **Verify Configuration:**
   - The custom workflow in `.github/workflows/security.yml` will now run successfully
   - You'll get enhanced security scanning with additional query suites

## Why Advanced Configuration?

Our advanced CodeQL setup provides:

- ✅ **Security-Extended Queries** - Additional security vulnerability detection
- ✅ **Security-and-Quality Queries** - Code quality and maintainability checks  
- ✅ **Custom Build Process** - Optimized for .NET projects
- ✅ **Enhanced Coverage** - More comprehensive analysis than default setup

## Alternative: Simplified Setup

If you prefer to use GitHub's default CodeQL setup instead:

1. **Remove Custom Workflow:** Delete or disable `.github/workflows/security.yml`
2. **Enable Default Setup:** Go to Settings → Code security and analysis → Enable default CodeQL analysis
3. **Trade-offs:** You'll lose the enhanced security queries but gain simpler maintenance

## Need Help?

- [CodeQL Documentation](https://docs.github.com/en/code-security/code-scanning/automatically-scanning-your-code-for-vulnerabilities-and-errors/about-code-scanning-with-codeql)
- [Advanced Security Setup](https://docs.github.com/en/code-security/code-scanning/automatically-scanning-your-code-for-vulnerabilities-and-errors/configuring-code-scanning-for-a-repository)