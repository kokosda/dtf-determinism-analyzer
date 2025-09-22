# DtfDeterminismAnalyzer NuGet Package Constitution

This constitution defines the non-negotiable principles and quality bars for any NuGet package produced by this project.

## Core Principles

### I. Deterministic & Reproducible Builds (NON-NEGOTIABLE)
- Always build and pack in Release with deterministic settings.
- Required MSBuild properties (set in packable projects and enforced in CI):
	- ContinuousIntegrationBuild=true
	- Deterministic=true (SDK default; keep enabled)
	- DebugType=portable (Portable PDBs)
	- EmbedUntrackedSources=true (for Source Link reproducibility)
	- IncludeSymbols=true and SymbolPackageFormat=snupkg
	- TreatWarningsAsErrors=true (library projects)
	- PathMap mapping local paths to stable roots (e.g., $(MSBuildProjectDirectory)=/)
- No machine-specific paths or timestamps embedded in assemblies or packages.
- CI uses pinned toolchain versions; a clean clone must produce identical nupkg/snupkg artifacts.

### II. Semantic Versioning 2.0.0
- Use SemVer 2.0.0 for PackageVersion.
- Versioning is derived from git tags. Pre-releases use -alpha.N, -beta.N, or -rc.N.
- Breaking public API or behavior changes require a MAJOR increment and a migration guide.
- Additive, backward-compatible features increment MINOR.
- Bug fixes only increment PATCH.
- Deprecate with [Obsolete] for at least one MINOR before removal. Maintain a clear CHANGELOG.md.

### III. Clear Metadata & Discoverability
- Required package metadata:
	- PackageId, Title, Description, Authors
	- License via PackageLicenseExpression (SPDX) or PackageLicenseFile + included file
	- RepositoryUrl and RepositoryType=git; include RepositoryCommit on CI builds
	- PackageReadmeFile=README.md and include README.md in the package
	- PackageIcon and included icon asset (no deprecated icon URL)
	- Tags and ProjectUrl for discoverability
- Keep README concise with quickstart, usage, and links to docs/samples.

### IV. Target Frameworks & Compatibility
- Prefer multi-targeting for libraries when feasible:
	- net8.0 for modern .NET
	- netstandard2.0 for broad compatibility (when the API allows)
- Enable nullable reference types and address warnings.
- Consider trimming/AOT friendliness: avoid reflection where possible; annotate with DynamicDependency or UnconditionalSuppressMessage only when necessary.
- Package analyzers/source generators correctly (under analyzers/, build/ assets) with PrivateAssets where appropriate.

### V. Source Link, PDBs, and Symbols
- Enable Source Link for Git hosting; ensure source indexing works from public commits.
- Emit portable PDBs; include XML documentation files in the nupkg.
- Publish .snupkg symbol packages to the Microsoft Symbol Server (https://symbols.nuget.org/upload).

### VI. Signing & Integrity
- Strong-name sign public assemblies with the organization key for externally-consumed packages.
- If available, sign nupkg artifacts with a valid code-signing certificate (nuget sign) and verify signatures in CI.
- Respect and verify repository signatures from NuGet.org; do not disable integrity checks (SHA-512).

### VII. Dependency Hygiene
- Keep dependencies minimal, stable, and justified.
- Avoid floating versions; use pinned or well-bounded version ranges.
- Prefer Central Package Management (Directory.Packages.props) for consistency.
- Use PrivateAssets=all for compile-time-only dependencies (e.g., analyzers).
- Monitor transitive dependencies for vulnerabilities and breaking changes.

### VIII. Quality Gates
- Unit and integration tests are mandatory for public surface changes; maintain a meaningful coverage target.
- Enable .NET analyzers; treat important categories as errors. Nullable warnings must be addressed.
- Enforce public API approvals (e.g., PublicApiAnalyzer) and require API review for breaking changes.
- Benchmark critical paths when performance could regress; maintain performance budgets.

### IX. Documentation & Developer Experience
- Ship XML docs for all public APIs; include example snippets that compile.
- Provide a practical README (quick start, key APIs, configuration, limitations).
- Maintain sample projects and minimal reproductions for complex scenarios.
- Release Notes are included per version and summarized in CHANGELOG.md.

### X. CI/CD & Publishing
- Build, test, and pack on PRs; publish only from signed, tagged releases.
- dotnet pack uses Release configuration with deterministic and Source Link validation enabled.
- Push .nupkg to nuget.org and .snupkg to the symbol server.
- Automate versioning from git tags and generate release notes from commits/PRs.

## Security & Compliance
- Maintain an SBOM for releases when feasible.
- Run vulnerability scans regularly (e.g., dotnet list package --vulnerable) and address issues promptly.
- Adhere to license policy; include license files and correct SPDX identifiers.
- Protect signing keys; never commit secrets. Enable secret scanning in the repository.

## Development Workflow
- Open an issue and proposal for any breaking change; obtain approval and document the migration plan.
- Use pre-release channels (-alpha/-beta/-rc) for testing significant changes before stable.
- Avoid experimental public APIs; prefer internal or [EditorBrowsable(Never)] and clearly mark preview status when unavoidable.
- Tests, docs, and samples are updated in the same PR as API changes.

## Governance
- This constitution supersedes other packaging practices for this repository.
- All PRs must include a checklist confirming compliance with Core Principles where applicable.
- Amendments require documentation, reviewer approval, and updates to CI and templates to enforce the new rules.

Version: 1.0.0 | Ratified: 2025-09-18 | Last Amended: 2025-09-18