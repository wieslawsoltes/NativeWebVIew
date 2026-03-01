# Phase 16 Regression Evaluation JSON and Gate Auditability Checklist

## Scope

- Add regression evaluation API with explicit gate policy outcomes.
- Extend diagnostics sample and wrapper script with comparison evaluation JSON output.
- Publish regression evaluation JSON artifacts in CI/release workflows.
- Update docs and plan for JSON gate-auditing workflows.

## Implemented

- Added regression evaluation API:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluation.cs`
  - Added:
    - `NativeWebViewDiagnosticsRegressionEvaluation`
    - `NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(...)`
  - Includes policy flags and computed gate outcomes:
    - `WouldFailRequireReady`
    - `WouldFailRegressionGate`
    - `WouldFailBaselineSyncGate`
    - `EffectiveExitCode`
- Added evaluator unit tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluatorTests.cs`
- Extended diagnostics sample CLI:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Added:
    - `--comparison-json-output <path>`
    - JSON export of regression evaluation object
    - shared JSON writer helper used for report/evaluation artifacts
- Extended diagnostics wrapper script:
  - `scripts/run-platform-diagnostics-report.sh`
  - Added:
    - `--comparison-json-output <path>` forwarding
    - output creation + schema-field checks (`effectiveExitCode`)
- Updated CI diagnostics artifact generation:
  - `.github/workflows/ci.yml`
  - writes and uploads `artifacts/diagnostics/ci-blocking-regression.json`
- Updated release diagnostics artifact generation and attachments:
  - `.github/workflows/release.yml`
  - writes/uploads/attaches `artifacts/diagnostics/release-blocking-regression-<version>.json`
- Updated docs and README command examples:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/platform-prerequisites.md`
  - `docs/quickstart.md`
  - `README.md`
- Updated master implementation plan with Phase 16 milestone:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `bash -n scripts/update-blocking-baseline.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-debug.json --markdown-output artifacts/diagnostics/platform-diagnostics-debug.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --blocking-baseline-output artifacts/diagnostics/current-blocking-baseline-debug.txt --comparison-markdown-output artifacts/diagnostics/blocking-regression-debug.md --comparison-json-output artifacts/diagnostics/blocking-regression-debug.json --require-baseline-sync --allow-not-ready`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/invalid-phase16-md.json --comparison-markdown-output artifacts/diagnostics/invalid-phase16-md-regression.md` (expected non-zero because baseline is missing)
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/invalid-phase16.json --comparison-json-output artifacts/diagnostics/invalid-phase16-regression.json` (expected non-zero because baseline is missing)
- `./scripts/update-blocking-baseline.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/refreshed-blocking-baseline-debug.txt`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
