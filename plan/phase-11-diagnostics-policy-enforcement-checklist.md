# Phase 11 Diagnostics Policy Enforcement Checklist

## Scope

- Add diagnostics validation helpers for fail-fast startup policy.
- Enforce diagnostics readiness in sample smoke scripts used by CI/release.
- Document strict diagnostics modes.

## Implemented

- Added diagnostics policy helper:
  - `src/NativeWebView.Core/DiagnosticsValidator.cs`
  - `NativeWebViewDiagnosticsValidator.IsReady(...)`
  - `NativeWebViewDiagnosticsValidator.EnsureReady(...)`
- Added validator unit tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsValidatorTests.cs`
- Updated sample applications to optionally enforce diagnostics policy via environment variables:
  - `NATIVEWEBVIEW_DIAGNOSTICS_REQUIRE_READY`
  - `NATIVEWEBVIEW_DIAGNOSTICS_WARNINGS_AS_ERRORS`
  - Files:
    - `samples/NativeWebView.Sample.Desktop/Program.cs`
    - `samples/NativeWebView.Sample.MobileBrowser/Program.cs`
- Updated smoke wrappers to enforce diagnostics readiness:
  - `scripts/run-desktop-sample-smoke.sh`
  - `scripts/run-mobile-browser-sample-smoke.sh`
- Updated docs:
  - `docs/platform-prerequisites.md`
  - `docs/quickstart.md`
  - `docs/ci-and-release.md`
  - `README.md`
- Updated master plan with Phase 11 milestone:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-desktop-sample-smoke.sh scripts/run-mobile-browser-sample-smoke.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --platform all`
- `./scripts/run-ios-simulator-contract-smoke.sh --configuration Debug --skip-simulator-boot`
- `./scripts/run-android-emulator-contract-smoke.sh --configuration Debug`
- `./scripts/run-browser-playwright-smoke.sh --python python3 --venv .venv-docs --no-browser-install`
