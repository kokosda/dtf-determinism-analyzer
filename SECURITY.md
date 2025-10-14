# Security Policy

## Reporting Security Vulnerabilities

The DTF Determinism Analyzer team takes security seriously. We appreciate your efforts to responsibly disclose any security vulnerabilities you may find.

### How to Report a Security Vulnerability

**To report a security vulnerability, please use one of the following methods:**

#### Option 1: GitHub Security Advisories (Recommended)
1. Navigate to the [Security tab](https://github.com/kokosda/dtf-determinism-analyzer/security) of this repository
2. Click on "Advisories" in the left sidebar
3. Click "Report a vulnerability" to create a private security advisory
4. Fill out the form with details about the vulnerability

*Note: If you don't see the "Report a vulnerability" option, this feature may not yet be enabled for this repository.*

#### Option 2: Private Email
If GitHub Security Advisories are not available, please email the maintainer directly with:
- Subject: "SECURITY: [Brief Description]"
- Detailed description of the vulnerability
- Steps to reproduce (if applicable)
- Potential impact assessment

We will respond within 48 hours to coordinate the disclosure privately.

#### Option 3: Public GitHub Issue (Non-sensitive issues only)
For security-related issues that are **not sensitive** (e.g., general security questions, documentation improvements):
- Open a GitHub issue with the title "SECURITY: [Brief Description]"
- **Do not include sensitive details** in public issues

## Supported Versions

We provide security updates for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | ✅ Yes            |
| < 1.0   | ❌ No             |

Security updates will be provided for:
- The latest major version (1.x.x)
- The latest patch release within a major version

## What Constitutes a Security Vulnerability

For the DTF Determinism Analyzer, security vulnerabilities may include but are not limited to:

### High Priority
- **Code injection vulnerabilities** - Malicious code execution through analyzer input
- **Path traversal vulnerabilities** - Unauthorized file system access during analysis
- **Information disclosure** - Sensitive information leaked through analyzer output or logs
- **Denial of service** - Analyzer causing excessive resource consumption or crashes

### Medium Priority
- **False negatives** - Analyzer failing to detect actual determinism violations that could lead to runtime failures
- **Dependency vulnerabilities** - Security issues in third-party packages used by the analyzer

### Low Priority
- **False positives** - Analyzer incorrectly flagging safe code (primarily a reliability issue)

## Response Timeline

We are committed to responding to security reports in a timely manner:

- **Initial acknowledgment**: Within 48 hours of report submission
- **Initial assessment**: Within 5 business days
- **Status updates**: Every 7 days until resolution
- **Security fix release**: Based on severity
  - Critical: Within 7 days
  - High: Within 14 days  
  - Medium: Within 30 days
  - Low: Next regular release cycle

## Security Update Process

When a security vulnerability is confirmed:

1. We will work with the reporter to understand and reproduce the issue
2. A fix will be developed and tested
3. A security advisory will be published
4. A patched version will be released
5. Users will be notified through:
   - GitHub Security Advisories
   - Release notes
   - NuGet package updates

## Escalation

If you do not receive an acknowledgment of your report within 48 hours, or if you are not satisfied with our response within 7 business days, you may:

1. Contact GitHub Support directly
2. Reach out through the project's public GitHub issues (for non-sensitive matters)

## Security Best Practices for Users

To use the DTF Determinism Analyzer securely:

### For CI/CD Integration
- Always use the latest version of the analyzer
- Run the analyzer in isolated build environments
- Review analyzer output for any unexpected warnings or errors
- Keep your .NET SDK and build tools updated

### For Local Development
- Install the analyzer through official NuGet packages only
- Verify package signatures when possible
- Report any suspicious analyzer behavior immediately

## Security Research and Bug Bounty

Currently, we do not offer a formal bug bounty program. However, we greatly appreciate security research and will:

- Acknowledge security researchers in our security advisories (with permission)
- Provide attribution in release notes for significant security improvements
- Consider feature requests that enhance security posture

## Additional Security Resources

- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [NuGet Package Security](https://docs.microsoft.com/en-us/nuget/policies/security)
- [GitHub Security Best Practices](https://docs.github.com/en/code-security)

---

Thank you for helping keep the DTF Determinism Analyzer and its users safe!