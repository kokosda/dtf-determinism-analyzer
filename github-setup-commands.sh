#!/bin/bash

GITHUB_USERNAME="kokosda"
REPO_NAME="dtf-determinism-analyzer"

echo "Setting up GitHub remote and pushing..."

# Add the remote repository
git remote add origin https://github.com/${GITHUB_USERNAME}/${REPO_NAME}.git

# Verify remote was added
git remote -v

# Push main branch and tags
git push -u origin main

# Push the release tag
git push origin v1.0.0

echo "✅ Repository setup complete!"
echo "Next: Create GitHub release at https://github.com/${GITHUB_USERNAME}/${REPO_NAME}/releases/new"