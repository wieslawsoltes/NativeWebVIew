# Phase 7 Release Notes and Docs Site Checklist

## Scope

- Generate release notes from changelog fragments.
- Validate changelog fragments in CI before merge/release.
- Build and publish documentation site from markdown docs.

## Implemented

- Added changelog fragment system:
  - `changelog/README.md`
  - `changelog/fragments/2026-03-01-phase6.md`
- Added tooling scripts:
  - `scripts/validate-changelog-fragments.sh`
  - `scripts/build-release-notes.sh`
- Updated CI workflow (`.github/workflows/ci.yml`):
  - Runs changelog fragment validation in quality gate.
- Updated release workflow (`.github/workflows/release.yml`):
  - Validates changelog fragments.
  - Generates `artifacts/release-notes.md` from fragments.
  - Publishes GitHub release with generated body.
- Added docs site support:
  - `mkdocs.yml`
  - `docs/requirements.txt` with pinned-compatible docs tooling versions.
  - `docs/index.md`
  - `.github/workflows/docs.yml` for build and GitHub Pages deploy.
- Updated user-facing docs:
  - `README.md`
  - `docs/ci-and-release.md`

## Validation

- `./scripts/validate-changelog-fragments.sh`
- `./scripts/build-release-notes.sh --version 0.1.0 --output artifacts/release-notes.md`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages`
- `mkdocs build --strict`
