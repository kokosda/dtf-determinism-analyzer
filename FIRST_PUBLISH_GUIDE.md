# Quick Start: Publishing Your First Package

Since you don't have the package on NuGet.org yet, here's the exact steps to get your first package published and then migrate to Trusted Publishers.

## Step 1: Create Your First Package (Using API Key)

### 1.1 Get NuGet API Key
1. Go to https://www.nuget.org/account/apikeys
2. Click "Create"
3. Set:
   - **Key Name**: `DTF Determinism Analyzer - GitHub Actions`
   - **Select Scopes**: ✅ Push new packages and package versions
   - **Select Packages**: Leave empty (applies to all)
   - **Glob Pattern**: `*` (or leave empty)
4. Copy the generated API key

### 1.2 Add GitHub Secret (Repository Secret - Recommended)
1. Go to https://github.com/kokosda/dtf-determinism-analyzer/settings/secrets/actions
2. Click "New repository secret" (under Repository secrets section)
3. Set:
   - **Name**: `NUGET_API_KEY`
   - **Secret**: Paste your API key from step 1.1
4. Click "Add secret"

**Alternative: Environment Secret (More Secure)**
If you want extra protection:
1. Go to Repository → Settings → Environments
2. Create environment named "production"
3. Add protection rules (optional: required reviewers, branch restrictions)
4. Add environment secret `NUGET_API_KEY` 
5. Uncomment the `environment:` section in your workflow

### 1.3 Update Workflow for First Publish
Edit `.github/workflows/ci-cd.yml`:

```yaml
# Find this line (around line 135):
run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate

# Replace it with:
run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### 1.4 Create Your First Release
1. Go to https://github.com/kokosda/dtf-determinism-analyzer/releases
2. Click "Create a new release"
3. Set:
   - **Tag**: `v1.0.0`
   - **Title**: `DTF Determinism Analyzer v1.0.0`
   - **Description**: `Initial release with 10 determinism rules and automatic code fixes`
4. Click "Publish release"
5. Monitor the workflow at: https://github.com/kokosda/dtf-determinism-analyzer/actions

### 1.5 Verify Package Creation
- Check that your package appears at: https://www.nuget.org/packages/DtfDeterminismAnalyzer
- It may take a few minutes to appear after successful publishing

## Step 2: Migrate to Trusted Publishers (Recommended)

### 2.1 Configure Trusted Publisher on NuGet.org
1. Go to https://www.nuget.org/packages/DtfDeterminismAnalyzer/Manage
2. Click the "Trusted Publishers" tab
3. Click "Add trusted publisher"
4. Fill in:
   - **Subject type**: GitHub Actions
   - **GitHub owner**: `kokosda`
   - **GitHub repository**: `dtf-determinism-analyzer`
   - **GitHub workflow**: `.github/workflows/ci-cd.yml`
   - **GitHub environment**: (leave empty)
5. Click "Add"

### 2.2 Update Workflow for Trusted Publishers
Edit `.github/workflows/ci-cd.yml`:

```yaml
# Change this line back:
run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate

# To this:
run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### 2.3 Clean Up (Optional)
1. Go to https://github.com/kokosda/dtf-determinism-analyzer/settings/secrets/actions
2. Delete the `NUGET_API_KEY` secret (no longer needed)
3. Go to https://www.nuget.org/account/apikeys
4. Delete or expire the API key (no longer needed)

### 2.4 Test Trusted Publishers
1. Create another release (e.g., `v1.0.1`) with a minor change
2. Verify it publishes successfully without the API key

## Alternative: Manual First Upload

If you prefer to do the first upload manually:

```bash
# Build the package locally
cd /Users/kokosda/repos/dtf-determinism-analyzer
dotnet pack src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj --configuration Release --output ./dist

# Upload manually (replace YOUR_API_KEY with your actual key)
dotnet nuget push ./dist/DtfDeterminismAnalyzer.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

Then skip to Step 2 (Trusted Publishers setup).

## Summary

- ✅ **First publish**: Requires API key (one-time setup)
- ✅ **Future publishes**: Use Trusted Publishers (more secure, no secrets)
- ✅ **Your package is ready**: All metadata and configuration looks perfect
- ✅ **Workflow will work**: Just needs the API key for the first push

Choose your preferred approach and you'll have your package on NuGet.org shortly! 🚀