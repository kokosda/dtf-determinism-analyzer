# Local GitHub Actions Testing Guide

## Quick Method (Available Now) ✅

Use the local CI/CD script we just created:
```bash
./test-ci-cd-local.sh
```

## Advanced Method with `act` (Requires Docker)

### 1. Install Docker Desktop
Download from: https://www.docker.com/products/docker-desktop/

### 2. Once Docker is running, test specific workflows:

#### Test the main CI/CD build and test jobs:
```bash
# Test build-and-test job
act -j build-and-test

# Test code-analysis job  
act -j code-analysis

# Test all jobs that run on push
act push

# Test pull request workflow
act pull_request
```

#### Test quality checks:
```bash
# Test documentation and quality workflows
act -W .github/workflows/quality.yml

# Test specific quality job
act -j quality-gates
```

#### Test with specific environment:
```bash
# Test with specific .NET version
act -j build-and-test --env DOTNET_VERSION=8.0.x

# Test with container architecture for M-series Mac
act -j build-and-test --container-architecture linux/amd64
```

### 3. Useful `act` flags:

```bash
# List all available jobs
act --list

# Dry run (show what would run)
act --dryrun

# Run with verbose logging
act -v

# Use specific runner image
act -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest

# Set secrets (if needed)
act --secret-file .secrets
```

### 4. Create .actrc file for default settings:
```bash
echo "--container-architecture linux/amd64" > ~/.actrc
echo "-P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest" >> ~/.actrc
```

## Testing Your Specific Fix

The key test is that sample projects can restore without NuGet source errors:

```bash
# This should work now (was failing before)
cd samples/DurableFunctionsSample
dotnet restore
# No more NU1301 errors! ✅

cd ../DurableTaskSample  
dotnet restore
# Works as before ✅
```

## Manual CI/CD Steps Simulation

You can also manually run the exact same steps as GitHub Actions:

```bash
# Match the CI environment
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=true
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# Run the pipeline steps
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --verbosity normal
dotnet pack src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj --configuration Release --no-build
```