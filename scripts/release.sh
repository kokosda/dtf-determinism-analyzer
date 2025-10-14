#!/bin/bash

# DTF Determinism Analyzer - Release Tagging Script
# This script automates version tagging and release preparation

set -euo pipefail

# Configuration
PROJECT_FILE="src/DtfDeterminismAnalyzer/DtfDeterminismAnalyzer.csproj"
CHANGELOG_FILE="CHANGELOG.md"
README_FILE="README.md"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to get current version from project file
get_current_version() {
    grep "<Version>" "$PROJECT_FILE" | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/' | tr -d '[:space:]'
}

# Function to validate semantic version format
validate_version() {
    local version=$1
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        log_error "Invalid version format: $version"
        log_error "Expected format: MAJOR.MINOR.PATCH (e.g., 1.2.3)"
        return 1
    fi
    return 0
}

# Function to update version in project file
update_project_version() {
    local new_version=$1
    local assembly_version="${new_version}.0"
    
    log_info "Updating version in $PROJECT_FILE..."
    
    # Create backup
    cp "$PROJECT_FILE" "${PROJECT_FILE}.backup"
    
    # Update Version
    sed -i.tmp "s|<Version>.*</Version>|<Version>$new_version</Version>|" "$PROJECT_FILE"
    
    # Update AssemblyVersion  
    sed -i.tmp "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>$assembly_version</AssemblyVersion>|" "$PROJECT_FILE"
    
    # Update FileVersion
    sed -i.tmp "s|<FileVersion>.*</FileVersion>|<FileVersion>$assembly_version</FileVersion>|" "$PROJECT_FILE"
    
    # Clean up temp files
    rm -f "${PROJECT_FILE}.tmp"
    
    log_success "Updated project file to version $new_version"
}

# Function to update changelog
update_changelog() {
    local version=$1
    local date=$(date +%Y-%m-%d)
    
    log_info "Updating CHANGELOG.md..."
    
    # Create backup
    cp "$CHANGELOG_FILE" "${CHANGELOG_FILE}.backup"
    
    # Replace [Unreleased] with version and date
    sed -i.tmp "s/## \[Unreleased\]/## [$version] - $date/" "$CHANGELOG_FILE"
    
    # Add new [Unreleased] section
    sed -i.tmp "/## \[$version\] - $date/i\\
## [Unreleased]\\
\\
### Added\\
- Features and improvements in development\\
\\
### Changed\\
- Updates and modifications in development\\
\\
### Fixed\\
- Bug fixes in development\\
\\
" "$CHANGELOG_FILE"
    
    # Clean up temp files
    rm -f "${CHANGELOG_FILE}.tmp"
    
    log_success "Updated CHANGELOG.md for version $version"
}

# Function to create git tag
create_git_tag() {
    local version=$1
    local tag_name="v$version"
    
    log_info "Creating git tag: $tag_name"
    
    # Check if tag already exists
    if git tag -l | grep -q "^$tag_name$"; then
        log_warning "Tag $tag_name already exists"
        read -p "Do you want to delete and recreate it? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            git tag -d "$tag_name" || true
            git push origin ":refs/tags/$tag_name" || true
        else
            log_error "Aborting due to existing tag"
            return 1
        fi
    fi
    
    # Create annotated tag with changelog info
    local tag_message="Release $version

$(sed -n "/## \[$version\]/,/## \[/p" "$CHANGELOG_FILE" | head -n -1 | tail -n +2)"
    
    git tag -a "$tag_name" -m "$tag_message"
    
    log_success "Created git tag: $tag_name"
}

# Function to commit version changes
commit_version_changes() {
    local version=$1
    
    log_info "Committing version changes..."
    
    # Add changed files
    git add "$PROJECT_FILE" "$CHANGELOG_FILE"
    
    # Commit changes
    git commit -m "chore: bump version to $version

- Updated project version to $version
- Updated CHANGELOG.md with release notes
- Prepared for release tagging"
    
    log_success "Committed version changes"
}

# Function to display current status
show_status() {
    local current_version=$(get_current_version)
    
    echo "=========================================="
    echo "DTF Determinism Analyzer - Version Status"
    echo "=========================================="
    echo "Current Version: $current_version"
    echo "Project File: $PROJECT_FILE"
    echo "Changelog: $CHANGELOG_FILE"
    echo
    echo "Recent Git Tags:"
    git tag -l | tail -5 || echo "No tags found"
    echo "=========================================="
}

# Function to push changes and tags
push_release() {
    local version=$1
    local tag_name="v$version"
    
    log_info "Pushing changes and tags to origin..."
    
    # Push commits
    git push origin
    
    # Push tags  
    git push origin "$tag_name"
    
    log_success "Pushed release $version to origin"
}

# Main release function
release() {
    local new_version=$1
    
    # Validate input
    if ! validate_version "$new_version"; then
        return 1
    fi
    
    local current_version=$(get_current_version)
    
    log_info "Preparing release: $current_version → $new_version"
    
    # Check if working directory is clean
    if [[ -n $(git status --porcelain) ]]; then
        log_warning "Working directory is not clean"
        git status --short
        read -p "Continue with uncommitted changes? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_error "Aborting release due to uncommitted changes"
            return 1
        fi
    fi
    
    # Confirm release
    echo
    echo "=========================================="
    echo "Release Summary"
    echo "=========================================="
    echo "Current Version: $current_version"
    echo "New Version: $new_version"  
    echo "Tag: v$new_version"
    echo "=========================================="
    echo
    read -p "Proceed with release? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_error "Release cancelled by user"
        return 1
    fi
    
    # Execute release steps
    update_project_version "$new_version"
    update_changelog "$new_version"
    commit_version_changes "$new_version"
    create_git_tag "$new_version"
    
    log_success "Release $new_version prepared successfully!"
    echo
    echo "Next steps:"
    echo "  1. Review the changes: git log --oneline -5"
    echo "  2. Push to remote: ./release.sh push $new_version"
    echo "  3. Create GitHub release from tag v$new_version"
    echo "  4. Publish NuGet package via CI/CD"
}

# Function to show help
show_help() {
    echo "DTF Determinism Analyzer - Release Management Script"
    echo
    echo "Usage: $0 [COMMAND] [VERSION]"
    echo
    echo "Commands:"
    echo "  release VERSION    Create new release with specified version"
    echo "  push VERSION       Push release changes and tags to origin"
    echo "  status             Show current version and git status"
    echo "  help               Show this help message"
    echo
    echo "Examples:"
    echo "  $0 release 1.0.1   # Create patch release"
    echo "  $0 release 1.1.0   # Create minor release"
    echo "  $0 release 2.0.0   # Create major release"
    echo "  $0 push 1.0.1      # Push release 1.0.1"
    echo "  $0 status           # Show current status"
    echo
    echo "Version format: MAJOR.MINOR.PATCH (semantic versioning)"
}

# Main script logic
main() {
    local command=${1:-"help"}
    
    case $command in
        "release")
            if [[ $# -ne 2 ]]; then
                log_error "Release command requires version argument"
                show_help
                exit 1
            fi
            release "$2"
            ;;
        "push")
            if [[ $# -ne 2 ]]; then
                log_error "Push command requires version argument"
                show_help
                exit 1
            fi
            push_release "$2"
            ;;
        "status")
            show_status
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            log_error "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Execute main function with all arguments
main "$@"