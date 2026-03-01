# Phase 17 Gate-Specific Exit Code Contract Checklist

## Scope

- Add explicit gate failure classification for diagnostics evaluation.
- Return deterministic gate-specific exit codes from diagnostics CLI.
- Preserve multi-gate failure behavior with a combined exit code.
- Update docs and plan with exit code contract details.

## Implemented

- Extended regression evaluation model:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluation.cs`
  - Added `NativeWebViewDiagnosticsGateFailureKind`.
  - Added `FailingGates`, `PrimaryFailingGate`, and `HasMultipleGateFailures`.
  - Updated `EffectiveExitCode` mapping:
    - `0` success
    - `10` require-ready failure
    - `11` regression failure
    - `12` baseline-sync failure
    - `13` multiple gate failures
- Extended evaluator tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluatorTests.cs`
  - Added single-gate exit code assertions and multi-gate failure coverage.
- Updated diagnostics sample CLI gate handling:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Uses evaluation gate list and returns `evaluation.EffectiveExitCode`.
  - Added consolidated gate-failure printing and usage help exit code table.
- Updated diagnostics wrapper validation:
  - `scripts/run-platform-diagnostics-report.sh`
  - Added upfront `--comparison-markdown-output` / `--comparison-json-output` baseline dependency checks for deterministic wrapper behavior.
- Updated docs and plan:
  - `docs/platform-diagnostics-report.md`
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `shellcheck scripts/run-platform-diagnostics-report.sh scripts/update-blocking-baseline.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-debug.json --markdown-output artifacts/diagnostics/platform-diagnostics-debug.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --blocking-baseline-output artifacts/diagnostics/current-blocking-baseline-debug.txt --comparison-markdown-output artifacts/diagnostics/blocking-regression-debug.md --comparison-json-output artifacts/diagnostics/blocking-regression-debug.json --require-baseline-sync --allow-not-ready`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/invalid-phase17-md.json --comparison-markdown-output artifacts/diagnostics/invalid-phase17-md-regression.md` (expected non-zero because baseline is missing)
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/invalid-phase17-json.json --comparison-json-output artifacts/diagnostics/invalid-phase17-json-regression.json` (expected non-zero because baseline is missing)
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
