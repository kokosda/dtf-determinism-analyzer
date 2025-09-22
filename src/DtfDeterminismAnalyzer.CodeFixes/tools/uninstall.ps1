param($installPath, $toolsPath, $package, $project)

# This script runs when the code fixes package is uninstalled
# It's included for NuGet package compatibility but modern SDKs handle code fix providers automatically

Write-Host "DTF Determinism Analyzer Code Fixes have been uninstalled."
Write-Host "Code fix suggestions are no longer available for DTF determinism violations."