# Code Fixes Not Working in Visual Studio?

If code fixes aren't appearing when you press `Ctrl+.` on analyzer errors:

## Quick Fix

```powershell
# From repository root
.\scripts\test-codefixes.ps1
```

This script automatically configures the samples to use NuGet package references instead of project references, which enables code fixes in Visual Studio.

## Need More Help?

For comprehensive development workflows, troubleshooting, and cross-platform setup, see the **[Local Development Guide](local-development.md)**.