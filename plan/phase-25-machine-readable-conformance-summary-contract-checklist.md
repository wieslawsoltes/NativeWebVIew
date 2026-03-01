# Phase 25 Machine-Readable Conformance Summary Contract Checklist

## Scope

- Add machine-readable conformance summary JSON output for diagnostics exit-code contract automation.
- Include per-scenario expected/actual outcomes and fingerprints in summary JSON.
- Include fingerprint baseline gate metadata in summary JSON.
- Document conformance summary JSON usage in README/docs and plan.

## Implemented

- Extended diagnostics conformance automation:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Added `exit-code-contract-summary.json` output.
  - Added summary JSON contract fields:
    - `allPassed`
    - `caseCount`
    - `cases[]` with `name`, `expectedExitCode`, `actualExitCode`, `passed`, `fingerprint`, `logFile`, `evaluationJson`, `evaluationMarkdown`
    - `fingerprintBaseline` with `enabled`, `baselinePath`, `currentPath`, `allMatched`, `mismatchCount`, `comparisonMarkdown`, `comparisonJson`
  - Added summary JSON contract validation checks for required top-level fields.
  - Preserved existing markdown/csv outputs and fingerprint baseline comparison gate behavior.
- Updated documentation:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/quickstart.md`
  - `docs/platform-prerequisites.md`
  - `README.md`
- Updated master plan:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/validate-diagnostics-exit-code-contract.sh`
- `shellcheck scripts/validate-diagnostics-exit-code-contract.sh scripts/update-diagnostics-fingerprint-baseline.sh scripts/run-platform-diagnostics-report.sh scripts/update-blocking-baseline.sh`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase25-exit-code-contract-gated --baseline ci/baselines/blocking-issues-baseline.txt --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase25-exit-code-contract-ungated --baseline ci/baselines/blocking-issues-baseline.txt`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase25-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase25-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase25-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase25-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase25-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
