# Phase 12 Diagnostics Reporting and Artifacts Checklist

## Scope

- Add reusable diagnostics-report generation for selected/all platforms.
- Provide a CLI/reporting sample and wrapper script for local and CI execution.
- Publish diagnostics report artifacts in CI and release workflows.
- Document diagnostics report usage and JSON schema.

## Implemented

- Added diagnostics report model + reporter APIs:
  - `src/NativeWebView.Core/DiagnosticsReport.cs`
  - `NativeWebViewDiagnosticsReporter.CreateReport(...)`
  - `NativeWebViewDiagnosticsReport`
  - `NativeWebViewPlatformDiagnosticsReportEntry`
- Added reporter unit tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsReporterTests.cs`
- Added diagnostics sample application:
  - `samples/NativeWebView.Sample.Diagnostics/NativeWebView.Sample.Diagnostics.csproj`
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
- Added CI/release wrapper script:
  - `scripts/run-platform-diagnostics-report.sh`
- Added CI workflow diagnostics report execution and artifact upload:
  - `.github/workflows/ci.yml`
- Added release workflow diagnostics report execution, artifact upload, and GitHub release attachment:
  - `.github/workflows/release.yml`
- Added docs and docs navigation updates:
  - `docs/platform-diagnostics-report.md`
  - `docs/platform-prerequisites.md`
  - `docs/quickstart.md`
  - `docs/ci-and-release.md`
  - `mkdocs.yml`
  - `README.md`
- Added Phase 12 milestone to master plan:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --platform all`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-debug.json --require-ready`
- `./scripts/run-ios-simulator-contract-smoke.sh --configuration Debug --skip-simulator-boot`
- `./scripts/run-android-emulator-contract-smoke.sh --configuration Debug`
- `./scripts/run-browser-playwright-smoke.sh --python python3 --venv .venv-docs --no-browser-install`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
