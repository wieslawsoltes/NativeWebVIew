# Phase 10 Platform Prerequisite Diagnostics Checklist

## Scope

- Implement startup prerequisite diagnostics for all supported platforms.
- Make diagnostics available through `NativeWebViewBackendFactory` and `NativeWebViewRuntime`.
- Document diagnostic usage and supported environment knobs.

## Implemented

- Added diagnostics API to `NativeWebView.Core`:
  - `NativeWebViewDiagnosticSeverity`
  - `NativeWebViewDiagnosticIssue`
  - `NativeWebViewPlatformDiagnostics`
- Extended `NativeWebViewBackendFactory` with diagnostics registration/retrieval:
  - `RegisterPlatformDiagnostics(...)`
  - `TryGetPlatformDiagnostics(...)`
  - `GetPlatformDiagnosticsOrDefault(...)`
- Extended runtime API:
  - `NativeWebViewRuntime.GetCurrentPlatformDiagnostics()`
  - `NativeWebViewRuntime.GetPlatformDiagnostics(...)`
- Added platform diagnostics providers and module registration wiring for:
  - Windows, macOS, Linux, iOS, Android, Browser
- Added diagnostics unit tests:
  - `tests/NativeWebView.Core.Tests/PlatformDiagnosticsTests.cs`
- Added docs:
  - `docs/platform-prerequisites.md`
  - Updated `README.md`, `docs/quickstart.md`, `docs/index.md`, `mkdocs.yml`, and `docs/ci-and-release.md`
- Added diagnostics visibility to sample output:
  - `samples/NativeWebView.Sample.Desktop/Program.cs`
  - `samples/NativeWebView.Sample.MobileBrowser/Program.cs`

## Validation

- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --platform all`
- `./scripts/run-ios-simulator-contract-smoke.sh --configuration Debug --skip-simulator-boot`
- `./scripts/run-android-emulator-contract-smoke.sh --configuration Debug`
- `./scripts/run-browser-playwright-smoke.sh --python python3 --venv .venv-docs --no-browser-install`
