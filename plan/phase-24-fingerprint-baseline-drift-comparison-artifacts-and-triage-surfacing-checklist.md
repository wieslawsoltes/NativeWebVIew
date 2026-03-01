# Phase 24 Fingerprint Baseline Drift Comparison Artifacts and Triage Surfacing Checklist

## Scope

- Add fingerprint baseline comparison artifacts that include expected vs actual fingerprints for all conformance scenarios.
- Ensure conformance mismatch failures report complete drift context instead of failing on first mismatch.
- Surface fingerprint comparison markdown in CI summary and release notes.
- Update diagnostics docs and README for new fingerprint drift artifacts.

## Implemented

- Extended diagnostics conformance automation:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Adds JSON-safe path escaping helper for artifact emission.
  - Writes fingerprint drift artifacts when `--fingerprint-baseline` is enabled:
    - `fingerprint-baseline-comparison.json`
    - `fingerprint-baseline-comparison.md`
  - Compares all scenarios (`pass`, `require-ready`, `regression`, `baseline-sync`, `multi-gate`) in one pass.
  - Fails only after writing comparison artifacts, with all mismatch details in stderr.
- Updated CI summary publication:
  - `.github/workflows/ci.yml`
  - Appends `fingerprint-baseline-comparison.md` to workflow summary when available.
- Updated release notes append flow:
  - `.github/workflows/release.yml`
  - Appends exit-code contract summary and fingerprint baseline comparison markdown from conformance artifacts.
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
- `shellcheck scripts/validate-diagnostics-exit-code-contract.sh scripts/update-diagnostics-fingerprint-baseline.sh scripts/run-platform-diagnostics-report.sh scripts/update-blocking-baseline.sh`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase24-exit-code-contract-gated --baseline ci/baselines/blocking-issues-baseline.txt --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt`
- negative mismatch check with temporary fingerprint baseline override (expected non-zero) and verification of generated mismatch artifacts:
  - `artifacts/diagnostics/phase24-exit-code-contract-mismatch/fingerprint-baseline-comparison.md`
  - `artifacts/diagnostics/phase24-exit-code-contract-mismatch/fingerprint-baseline-comparison.json`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase24-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase24-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase24-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase24-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase24-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
