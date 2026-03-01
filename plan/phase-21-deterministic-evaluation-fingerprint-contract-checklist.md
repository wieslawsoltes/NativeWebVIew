# Phase 21 Deterministic Evaluation Fingerprint Contract Checklist

## Scope

- Add deterministic evaluation fingerprint metadata to diagnostics regression evaluation artifacts.
- Surface fingerprint in machine-readable JSON and human-readable gate evaluation markdown outputs.
- Enforce fingerprint presence/format in diagnostics wrapper and conformance scripts.
- Add tests that validate fingerprint determinism and sensitivity to gate outcome changes.

## Implemented

- Added deterministic fingerprint field:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluation.cs`
  - New `Fingerprint` property on evaluation payload.
  - Fingerprint generation is stable for equivalent policy/comparison/gate states and excludes `GeneratedAtUtc`.
  - Fingerprint format: 64-char lowercase SHA-256 hex.
- Updated gate evaluation markdown formatter:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluationMarkdownFormatter.cs`
  - Includes `Fingerprint: <hash>` line in summary section.
- Updated diagnostics wrapper script:
  - `scripts/run-platform-diagnostics-report.sh`
  - Validates comparison JSON includes `fingerprint` with expected hex format.
  - Validates gate evaluation markdown includes fingerprint line.
- Updated conformance automation:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Validates fingerprint presence/format across evaluation JSON and markdown scenario outputs.
- Added/updated tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluatorTests.cs`
    - `EvaluationFingerprint_IgnoresGeneratedTimestamp`
    - `EvaluationFingerprint_ChangesWhenGateOutcomeChanges`
    - Base evaluation test now asserts fingerprint format.
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluationMarkdownFormatterTests.cs`
    - Asserts fingerprint line is rendered.
- Updated docs/README/plan:
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
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase21-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase21-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase21-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase21-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase21-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase21-exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
