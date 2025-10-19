# Automation Scripts

This folder contains automation scripts for project maintenance and development.

## Files

- **github-setup-commands.sh** - Initial GitHub repository setup commands
- **release.sh** - Automated release tagging and version management
- **test-ci-cd-local.sh** - Local testing of CI/CD pipeline
- **test-codefixes.ps1** - Cross-platform code fix testing automation (PowerShell)

## Usage

### Bash Scripts
Make sure scripts are executable before running:
```bash
chmod +x scripts/*.sh
```

### PowerShell Scripts
```powershell
# Run from repository root
.\scripts\test-codefixes.ps1
```

## Requirements

- Git
- .NET 8.0+ SDK
- Bash shell (or WSL on Windows) for .sh scripts
- PowerShell Core for .ps1 scripts