# Phase 22 Fingerprint Parity and Schema Version Enforcement Checklist

## Scope

- Add fingerprint schema version metadata to diagnostics regression evaluation outputs.
- Enforce fingerprint schema version and JSON/markdown parity in diagnostics scripts.
- Surface scenario fingerprints in conformance summary markdown.
- Update tests/docs/plan/changelog for the new fingerprint contract requirements.

## Implemented

- Added fingerprint schema version field:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluation.cs`
  - New `CurrentFingerprintVersion` constant (`1`).
  - New serialized property `FingerprintVersion`.
  - Fingerprint hash input now includes `fingerprintVersion`.
- Updated evaluation markdown formatter:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluationMarkdownFormatter.cs`
  - Includes `Fingerprint Version: <value>` in markdown output.
- Updated diagnostics CLI output:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Logs evaluation summary with `effectiveExitCode`, `fingerprintVersion`, and `fingerprint`.
- Updated wrapper script:
  - `scripts/run-platform-diagnostics-report.sh`
  - Validates `fingerprintVersion: 1` in comparison JSON.
  - Validates `Fingerprint Version: 1` in evaluation markdown.
  - Validates JSON/markdown fingerprint parity when both artifacts are requested.
- Updated conformance script:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Generates deterministic pass-case baseline from preflight diagnostics snapshot.
  - Validates `fingerprintVersion: 1` in scenario JSON outputs.
  - Validates `Fingerprint Version: 1` in scenario markdown outputs.
  - Validates JSON/markdown fingerprint parity for comparison-enabled scenarios.
  - Extends summary markdown with per-case fingerprint column.
- Updated tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluatorTests.cs`
    - Added `FingerprintVersion_UsesStableCurrentVersion`.
    - Added assertions for `FingerprintVersion` in existing evaluation tests.
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluationMarkdownFormatterTests.cs`
    - Added assertions for `Fingerprint Version: 1` in markdown output.
- Updated docs/plan/changelog:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `README.md`
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `bash -n scripts/validate-diagnostics-exit-code-contract.sh`
- `shellcheck scripts/run-platform-diagnostics-report.sh scripts/validate-diagnostics-exit-code-contract.sh scripts/update-blocking-baseline.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `dotnet test tests/NativeWebView.Core.Tests/NativeWebView.Core.Tests.csproj -c Debug`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase22-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase22-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase22-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase22-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase22-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase22-exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
