# Trusted Publishers Setup Guide

## Overview

This repository now uses **Trusted Publishers** for secure NuGet package publishing. This modern approach eliminates the need for long-lived API keys by using OpenID Connect (OIDC) tokens from GitHub Actions.

## Benefits of Trusted Publishers

✅ **Enhanced Security**
- No long-lived API keys to manage or rotate
- Tokens are short-lived and automatically managed
- Reduced risk of credential compromise

✅ **Simplified Management**
- No secrets to configure in GitHub
- Automatic authentication through OIDC
- No manual key rotation required

✅ **Better Auditability**
- Clear audit trail tied to specific workflows
- Repository-scoped permissions
- Transparent publishing process

## Setup Instructions

### Step 1: First-Time Package Creation

**Important**: Trusted Publishers requires the package to exist on NuGet.org first. For the initial package creation, you need to use an API key.

#### Option A: Create Package with API Key First (Recommended)

1. **Get NuGet API Key**:
   - Go to https://www.nuget.org/account/apikeys
   - Create new API key with "Push new packages and package versions" scope
   - Copy the key

2. **Add GitHub Secret**:
   - Go to GitHub repo → Settings → Secrets and variables → Actions
   - Add secret: `NUGET_API_KEY`
   - Paste your API key as the value

3. **Update Workflow for First Publish**:
   - Edit `.github/workflows/ci-cd.yml`
   - Uncomment the API key line in the publish step:
   ```yaml
   # Change from this:
   run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
   
   # To this for first publish:
   run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
   ```

4. **Create First Release**:
   - Create a GitHub release to trigger the workflow
   - Verify package appears on NuGet.org

#### Option B: Manual First Upload

Alternatively, you can manually upload the first version:

```bash
# Build and pack locally
dotnet pack src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj --configuration Release

# Push to NuGet.org manually
dotnet nuget push src/DtfDeterminismAnalyzer/bin/Release/DtfDeterminismAnalyzer.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Step 2: Configure Trusted Publisher on NuGet.org

**After** your package exists on NuGet.org:

1. **Sign in to NuGet.org** with your account
2. **Navigate to your package**: https://www.nuget.org/packages/DtfDeterminismAnalyzer
3. **Click "Manage Package"** (you must be an owner)
4. **Go to "Trusted Publishers" tab**
5. **Click "Add Trusted Publisher"**
6. **Fill in the configuration**:
   ```
   Subject type: GitHub Actions
   GitHub owner: kokosda
   GitHub repository: dtf-determinism-analyzer
   GitHub workflow: .github/workflows/ci-cd.yml
   GitHub environment: (leave empty for now)
   ```
7. **Save the configuration**

### Step 3: Switch to Trusted Publishers

**After** configuring the Trusted Publisher:

1. **Update Workflow**:
   - Edit `.github/workflows/ci-cd.yml`
   - Comment out the API key line and use the Trusted Publishers line:
   ```yaml
   # Change from this (first-time):
   # run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
   
   # To this (ongoing):
   run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
   ```

2. **Remove API Key Secret** (optional but recommended):
   - Go to GitHub repo → Settings → Secrets and variables → Actions
   - Delete the `NUGET_API_KEY` secret

### Step 4: Verify Workflow Configuration

The workflow has already been updated with the required configuration:

```yaml
publish-nuget:
  name: Publish to NuGet
  runs-on: ubuntu-latest
  needs: pack
  if: github.event_name == 'release' && github.event.action == 'published'
  permissions:
    id-token: write  # Required for OIDC token
    contents: read
  
  steps:
  - name: Publish to NuGet using Trusted Publishers
    run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
```

**Key Points:**
- ✅ `id-token: write` permission is set
- ✅ No `--api-key` parameter needed
- ✅ OIDC token is automatically provided by GitHub

### Step 5: Test the Setup

1. **Create a test release** to verify the setup:
   ```bash
   # Use the GitHub web interface or:
   gh release create v1.0.1 --title "Test Release" --notes "Testing Trusted Publishers"
   ```

2. **Monitor the workflow** in GitHub Actions
3. **Verify successful publication** on NuGet.org

## Advanced Configuration

### Option 1: Environment Protection (Recommended)

For additional security, you can require manual approval for releases:

1. **Create a GitHub Environment**:
   - Go to: Repository → Settings → Environments
   - Create environment named `production`
   - Add protection rules (required reviewers, deployment branches)

2. **Update the workflow**:
   ```yaml
   publish-nuget:
     # ... existing configuration ...
     environment:
       name: production
       url: https://www.nuget.org/packages/DtfDeterminismAnalyzer
   ```

3. **Update Trusted Publisher configuration**:
   - Set GitHub environment to: `production`

### Option 2: Multiple Environments

You can configure different Trusted Publishers for different environments:

```yaml
# For pre-releases
publish-nuget-preview:
  environment:
    name: preview
  # Uses separate Trusted Publisher for preview releases

# For stable releases  
publish-nuget-production:
  environment:
    name: production
  # Uses separate Trusted Publisher for production releases
```

## Troubleshooting

### Common Issues

**❌ Publishing fails with authentication error**
```
error: Response status code does not indicate success: 401 (Unauthorized)
```

**Solution**: Verify Trusted Publisher configuration matches exactly:
- Owner: `kokosda` 
- Repository: `dtf-determinism-analyzer`
- Workflow: `.github/workflows/ci-cd.yml`
- Environment: (empty or matches workflow environment)

**❌ OIDC token not available**
```
error: Unable to get OIDC token
```

**Solution**: Ensure workflow has required permissions:
```yaml
permissions:
  id-token: write
  contents: read
```

**❌ Package already exists**
```
error: Response status code does not indicate success: 409 (Conflict)
```

**Solution**: This is expected for duplicate versions. The `--skip-duplicate` flag handles this gracefully.

### Debugging Steps

1. **Check workflow permissions** in the job definition
2. **Verify Trusted Publisher configuration** on NuGet.org
3. **Ensure the workflow file path matches** exactly (case-sensitive)
4. **Check GitHub Actions logs** for detailed error messages

## Migration from API Keys

If you previously used API keys, here's what changed:

### Before (API Key approach)
```yaml
- name: Publish to NuGet
  run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

**Required**:
- Manual API key creation on NuGet.org
- GitHub secret configuration
- Periodic key rotation

### After (Trusted Publishers)
```yaml
permissions:
  id-token: write
  contents: read

steps:
- name: Publish to NuGet using Trusted Publishers
  run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
```

**Required**:
- One-time Trusted Publisher setup on NuGet.org
- Workflow permissions configuration
- No secrets or manual key management

## Security Considerations

### What Trusted Publishers Protects Against

✅ **Credential compromise**: No long-lived secrets to be stolen
✅ **Accidental exposure**: No API keys in logs or configuration
✅ **Unauthorized publishing**: Tokens are scoped to specific repositories
✅ **Man-in-the-middle attacks**: OIDC tokens include audience validation

### Best Practices

1. **Use environment protection** for production releases
2. **Limit workflow triggers** to specific events (releases)
3. **Monitor publishing activity** through NuGet.org audit logs
4. **Keep workflows in protected branches** (main/master)

## Resources

- [NuGet Trusted Publishers Documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-with-trusted-publishing)
- [GitHub OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [OpenID Connect Standard](https://openid.net/connect/)

## Support

If you encounter issues with Trusted Publishers:

1. **Check this guide** for common solutions
2. **Review GitHub Actions logs** for detailed error messages  
3. **Verify NuGet.org configuration** matches your repository setup
4. **Open an issue** in this repository with specific error details