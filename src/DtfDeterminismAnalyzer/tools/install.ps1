param($installPath, $toolsPath, $package, $project)

# This script runs when the analyzer package is installed
# It's included for NuGet package compatibility but modern SDKs handle analyzers automatically

Write-Host "DTF Determinism Analyzer has been installed successfully."
Write-Host "The analyzer will automatically detect DTF orchestration code and validate determinism constraints."
Write-Host "Configure rules in .editorconfig or GlobalAnalyzerConfigFiles if needed."