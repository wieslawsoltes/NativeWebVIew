# Phase 23 Fingerprint Baseline Contract Gate and Refresh Workflow Checklist

## Scope

- Add repository baseline file for diagnostics conformance fingerprints.
- Extend conformance script to optionally enforce fingerprint baseline drift gate.
- Emit current fingerprint contract artifact from conformance runs.
- Add helper script for deterministic fingerprint baseline refresh.
- Wire CI/release/docs/plan/changelog for fingerprint baseline gate usage.

## Implemented

- Added repository fingerprint baseline:
  - `ci/baselines/diagnostics-fingerprint-baseline.txt`
  - Stores expected per-scenario fingerprints for:
    - `pass`
    - `require-ready`
    - `regression`
    - `baseline-sync`
    - `multi-gate`
- Extended conformance automation:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Added `--fingerprint-baseline <path>` option.
  - Generates deterministic pass-case baseline from preflight snapshot.
  - Writes current fingerprint contract file:
    - `fingerprint-current.txt`
  - Validates baseline coverage, format, duplicates, and per-scenario fingerprint equality when baseline gate is enabled.
  - Maintains fingerprint parity/format/version checks.
  - Updated to avoid Bash 4-only associative arrays (portable with default macOS Bash).
- Added fingerprint baseline refresh helper:
  - `scripts/update-diagnostics-fingerprint-baseline.sh`
  - Runs conformance script and writes refreshed `diagnostics-fingerprint-baseline.txt`.
- Updated CI and release workflows:
  - `.github/workflows/ci.yml`
  - `.github/workflows/release.yml`
  - Conformance step now enforces `--fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt`.
- Updated docs and README:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/quickstart.md`
  - `docs/platform-prerequisites.md`
  - `README.md`
- Updated master plan:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/validate-diagnostics-exit-code-contract.sh`
- `bash -n scripts/update-diagnostics-fingerprint-baseline.sh`
- `shellcheck scripts/validate-diagnostics-exit-code-contract.sh scripts/update-diagnostics-fingerprint-baseline.sh scripts/run-platform-diagnostics-report.sh scripts/update-blocking-baseline.sh`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase23-exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase23-exit-code-contract-gated --baseline ci/baselines/blocking-issues-baseline.txt --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase23-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase23-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase23-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase23-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase23-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
