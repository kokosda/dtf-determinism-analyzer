param($installPath, $toolsPath, $package, $project)

# This script runs when the code fixes package is installed
# It's included for NuGet package compatibility but modern SDKs handle code fix providers automatically

Write-Host "DTF Determinism Analyzer Code Fixes have been installed successfully."
Write-Host "Code fix providers will automatically suggest fixes for DTF determinism violations."
Write-Host "Use Ctrl+. (quick actions) to apply suggested fixes in the IDE."