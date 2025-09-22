param($installPath, $toolsPath, $package, $project)

# This script runs when the analyzer package is uninstalled
# It's included for NuGet package compatibility but modern SDKs handle analyzers automatically

Write-Host "DTF Determinism Analyzer has been uninstalled."
Write-Host "Analyzer rules are no longer active in this project."