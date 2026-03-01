# Phase 20 Structured Gate Failure Metadata Checklist

## Scope

- Add structured gate failure metadata to diagnostics regression evaluation outputs.
- Reuse gate failure metadata for CLI error logging and gate evaluation markdown formatting.
- Enforce `gateFailures` JSON contract in diagnostics wrapper and conformance scripts.
- Update docs/plan/changelog to document the new remediation metadata contract.

## Implemented

- Added structured gate failure model:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluation.cs`
  - New `NativeWebViewDiagnosticsGateFailure` type with:
    - `Kind`
    - `ExitCode`
    - `Message`
    - `Recommendation`
  - Added `GateFailures` to evaluation payload.
  - Added stable mapping helpers:
    - `GetExitCodeForGate(...)`
    - `GetGateFailureMessage(...)`
    - `GetGateFailureRecommendation(...)`
- Updated gate evaluation markdown formatter:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluationMarkdownFormatter.cs`
  - Uses structured `GateFailures` entries including recommendation text.
- Updated diagnostics CLI gate failure output:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Replaced switch-based error strings with `GateFailures` metadata output.
- Updated script-level JSON contract checks:
  - `scripts/run-platform-diagnostics-report.sh`
  - Enforces `gateFailures` presence when comparison JSON output is enabled.
- Updated exit-code conformance automation:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Validates `gateFailures` exists in scenario evaluation JSON.
  - Validates scenario-specific structured gate entries and recommendation text.
- Updated tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluatorTests.cs`
  - Added stable-message and stable-recommendation contract assertions.
  - Added assertions for `GateFailures` content in single/multi-gate scenarios.
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluationMarkdownFormatterTests.cs`
  - Updated expected markdown content to include recommendation text.
- Updated docs and plan:
  - `docs/platform-diagnostics-report.md`
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `bash -n scripts/validate-diagnostics-exit-code-contract.sh`
- `shellcheck scripts/run-platform-diagnostics-report.sh scripts/validate-diagnostics-exit-code-contract.sh scripts/update-blocking-baseline.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase20-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase20-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase20-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase20-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase20-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase20-exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
