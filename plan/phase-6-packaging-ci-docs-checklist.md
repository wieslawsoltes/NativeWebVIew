# Phase 6 Packaging, CI, Docs, and Release Checklist

## Scope

- Ship-ready NuGet package configuration across all package projects.
- Multi-OS CI workflow for restore/build/test and packaging artifacts.
- Tag-driven release workflow for versioned package publishing.
- Project documentation set and README.

## Implemented

- Added package/release infrastructure in `Directory.Build.props`:
  - SourceLink and repository publishing metadata.
  - Symbol package generation (`.snupkg`).
  - Package readme/license inclusion.
  - CI build detection and deterministic packaging knobs.
  - Non-packable defaults for sample executables and test projects.
- Added SourceLink package version in `Directory.Packages.props`.
- Added GitHub Actions workflows:
  - `.github/workflows/ci.yml` for quality gate (`dotnet format`), matrix `build + test` on Windows/macOS/Linux, sample smoke runs, and Ubuntu pack artifact generation.
  - `.github/workflows/release.yml` for `v*` tag-based versioned packing, sample smoke runs, conditional NuGet publish, and GitHub release-notes publishing.
- Added docs and README:
  - `README.md`
  - `docs/quickstart.md`
  - `docs/nativewebview.md`
  - `docs/nativewebdialog.md`
  - `docs/webauthenticationbroker.md`
  - `docs/interop/environment-options.md`
  - `docs/interop/native-browser-interop.md`
  - `docs/platforms/windows.md`
  - `docs/platforms/macos.md`
  - `docs/platforms/linux.md`
  - `docs/platforms/ios.md`
  - `docs/platforms/android.md`
  - `docs/platforms/browser.md`
  - `docs/ci-and-release.md`

## Validation

- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages`
