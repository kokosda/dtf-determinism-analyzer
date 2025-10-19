param(
    [string]$Version = "1.0.2-dev$([DateTime]::Now.ToString('HHmmss'))"
)

Write-Host "Testing code fixes with version: $Version" -ForegroundColor Green

# Find repository root by looking for .git directory or Directory.Packages.props
function Find-RepositoryRoot {
    $currentPath = Get-Location
    while ($currentPath -ne $null -and $currentPath.Path -ne [System.IO.Path]::GetPathRoot($currentPath.Path)) {
        if ((Test-Path (Join-Path $currentPath.Path ".git")) -or (Test-Path (Join-Path $currentPath.Path "Directory.Packages.props"))) {
            return $currentPath.Path
        }
        $currentPath = $currentPath.Parent
    }
    throw "Could not find repository root. Make sure you're running this script from within the repository."
}

# Get repository root and set up paths
$repoRoot = Find-RepositoryRoot
$analyzerPath = Join-Path $repoRoot "src" "DtfDeterminismAnalyzer"
$localPackagePath = Join-Path $repoRoot "local-nuget-packages"
$packagesPropsFile = Join-Path $repoRoot "Directory.Packages.props"
$samplesPath = Join-Path $repoRoot "samples"

Write-Host "Repository root: $repoRoot" -ForegroundColor Yellow
Write-Host "Local packages: $localPackagePath" -ForegroundColor Yellow

# Ensure local package directory exists
if (!(Test-Path $localPackagePath)) {
    New-Item -ItemType Directory -Path $localPackagePath -Force
    Write-Host "Created local packages directory: $localPackagePath" -ForegroundColor Yellow
}

# Build and pack
Write-Host "Building and packing analyzer..." -ForegroundColor Cyan
Set-Location $analyzerPath
dotnet pack -c Release -p:PackageVersion=$Version -o $localPackagePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Pack failed"
    exit 1
}

# Update Directory.Packages.props for Central Package Management
Write-Host "Updating Directory.Packages.props..." -ForegroundColor Cyan

if (Test-Path $packagesPropsFile) {
    Write-Host "  Updating DtfDeterminismAnalyzer version"
    $content = Get-Content $packagesPropsFile -Raw
    # Update the version in PackageVersion for DtfDeterminismAnalyzer
    $content = $content -replace '(<PackageVersion Include="DtfDeterminismAnalyzer"[^>]*Version=")[^"]*(")', "`${1}$Version`$2"
    Set-Content $packagesPropsFile -Value $content -NoNewline
} else {
    Write-Warning "Directory.Packages.props not found: $packagesPropsFile"
}

# Clear cache and restore
Write-Host "Clearing NuGet cache..." -ForegroundColor Cyan
dotnet nuget locals all --clear

Write-Host "Restoring packages..." -ForegroundColor Cyan
Set-Location $samplesPath
dotnet restore

Write-Host "" 
Write-Host "✅ Code fix testing setup complete!" -ForegroundColor Green
Write-Host "📦 Package version: $Version" -ForegroundColor Yellow
Write-Host "🚀 Ready to test in Visual Studio!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "1. Open Visual Studio 2022" -ForegroundColor Gray
Write-Host "2. Open samples\DtfSamples.sln" -ForegroundColor Gray  
Write-Host "3. Navigate to ProblematicOrchestrator.cs" -ForegroundColor Gray
Write-Host "4. Test your code fixes on analyzer errors" -ForegroundColor Gray