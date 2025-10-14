#!/bin/bash

# Local CI/CD Test Script
# This simulates the GitHub Actions CI/CD pipeline locally

set -e  # Exit on any error

echo "🚀 Starting Local CI/CD Test"
echo "=============================="

# Set environment variables (matching GitHub Actions)
export DOTNET_VERSION='8.0.x'
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=true
export DOTNET_CLI_TELEMETRY_OPTOUT=1

echo "📦 Environment Setup"
echo "- .NET Version: $DOTNET_VERSION"
echo "- Skip First Time Experience: $DOTNET_SKIP_FIRST_TIME_EXPERIENCE"
echo "- No Logo: $DOTNET_NOLOGO"
echo "- Telemetry Opt Out: $DOTNET_CLI_TELEMETRY_OPTOUT"
echo ""

# Step 1: Clean (simulate fresh checkout)
echo "🧹 Step 1: Clean workspace"
dotnet clean --configuration Release
echo "✅ Clean completed"
echo ""

# Step 2: Restore dependencies
echo "📥 Step 2: Restore dependencies"
dotnet restore
echo "✅ Restore completed"
echo ""

# Step 3: Build solution (matching CI/CD)
echo "🔨 Step 3: Build solution"
dotnet build --configuration Release --no-restore
BUILD_RESULT=$?
if [ $BUILD_RESULT -eq 0 ]; then
    echo "✅ Build completed successfully"
else
    echo "❌ Build failed with exit code $BUILD_RESULT"
fi
echo ""

# Step 4: Run tests (matching CI/CD)
echo "🧪 Step 4: Run tests"
dotnet test --configuration Release --no-build --verbosity normal
TEST_RESULT=$?
if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ All tests passed"
else
    echo "❌ Tests failed with exit code $TEST_RESULT"
fi
echo ""

# Step 5: Test sample projects (this was causing the CI/CD issue)
echo "🔍 Step 5: Test sample projects (the fix validation)"
echo ""

echo "Testing DurableFunctionsSample:"
cd samples/DurableFunctionsSample
dotnet restore --verbosity normal
SAMPLE1_RESTORE=$?
if [ $SAMPLE1_RESTORE -eq 0 ]; then
    echo "✅ DurableFunctionsSample restore successful"
    
    # Count analyzer violations (this should work now)
    VIOLATIONS=$(dotnet build --verbosity minimal 2>&1 | grep -c 'error DFA' || echo "0")
    echo "📊 DurableFunctionsSample: $VIOLATIONS DFA violations detected (expected: >0)"
else
    echo "❌ DurableFunctionsSample restore failed"
fi
cd ../..
echo ""

echo "Testing DurableTaskSample:"
cd samples/DurableTaskSample
dotnet restore --verbosity normal
SAMPLE2_RESTORE=$?
if [ $SAMPLE2_RESTORE -eq 0 ]; then
    echo "✅ DurableTaskSample restore successful"
    
    # Count analyzer violations
    VIOLATIONS=$(dotnet build --verbosity minimal 2>&1 | grep -c 'error DFA' || echo "0")
    echo "📊 DurableTaskSample: $VIOLATIONS DFA violations detected (expected: >0)"
else
    echo "❌ DurableTaskSample restore failed"
fi
cd ../..
echo ""

# Step 6: Package creation test (matching CI/CD pack step)
echo "📦 Step 6: Test NuGet package creation"
dotnet pack src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj --configuration Release --no-build --output ./test-artifacts
PACK_RESULT=$?
if [ $PACK_RESULT -eq 0 ]; then
    echo "✅ Package creation successful"
    ls -la ./test-artifacts/
    rm -rf ./test-artifacts
else
    echo "❌ Package creation failed"
fi
echo ""

# Summary
echo "📋 Local CI/CD Test Summary"
echo "=============================="
echo "Build: $([ $BUILD_RESULT -eq 0 ] && echo "✅ PASS" || echo "❌ FAIL")"
echo "Tests: $([ $TEST_RESULT -eq 0 ] && echo "✅ PASS" || echo "❌ FAIL")"
echo "Sample 1: $([ $SAMPLE1_RESTORE -eq 0 ] && echo "✅ PASS" || echo "❌ FAIL")"
echo "Sample 2: $([ $SAMPLE2_RESTORE -eq 0 ] && echo "✅ PASS" || echo "❌ FAIL")"
echo "Package: $([ $PACK_RESULT -eq 0 ] && echo "✅ PASS" || echo "❌ FAIL")"
echo ""

if [ $BUILD_RESULT -eq 0 ] && [ $TEST_RESULT -eq 0 ] && [ $SAMPLE1_RESTORE -eq 0 ] && [ $SAMPLE2_RESTORE -eq 0 ] && [ $PACK_RESULT -eq 0 ]; then
    echo "🎉 All CI/CD steps would succeed on GitHub!"
    exit 0
else
    echo "⚠️  Some CI/CD steps would fail on GitHub"
    exit 1
fi