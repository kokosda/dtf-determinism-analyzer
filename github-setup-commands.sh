#!/bin/bash

# GitHub Repository Setup Commands
# Run these commands after creating the GitHub repository

# Replace YOUR_USERNAME with your actual GitHub username
GITHUB_USERNAME="YOUR_USERNAME"
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