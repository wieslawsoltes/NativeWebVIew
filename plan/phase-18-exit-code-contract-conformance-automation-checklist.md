# Phase 18 Exit Code Contract Conformance Automation Checklist

## Scope

- Add an automated script to validate diagnostics gate exit code contract scenarios.
- Ensure conformance script verifies regression evaluation JSON outputs.
- Integrate conformance validation into CI and release workflows.
- Update docs/README/plan for local and pipeline usage.

## Implemented

- Added conformance validation script:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Validates expected exit codes for scenarios:
    - `pass` -> `0`
    - `require-ready` -> `10`
    - `regression` -> `11`
    - `baseline-sync` -> `12`
    - `multi-gate` -> `13`
  - Verifies generated evaluation JSON files include `effectiveExitCode` and expected gate names for multi-gate scenario.
  - Produces artifacts:
    - `exit-code-contract-summary.md`
    - `exit-code-contract-summary.csv`
    - per-scenario logs and JSON outputs
- Updated CI workflow:
  - `.github/workflows/ci.yml`
  - Added `Validate diagnostics exit code contract` step.
  - Publishes conformance summary markdown into `$GITHUB_STEP_SUMMARY`.
  - Uploads conformance artifacts under `artifacts/diagnostics/exit-code-contract/*`.
- Updated release workflow:
  - `.github/workflows/release.yml`
  - Added `Validate diagnostics exit code contract` step.
  - Uploads and attaches conformance artifacts under `artifacts/diagnostics/exit-code-contract-<version>/*`.
- Updated docs and README:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/quickstart.md`
  - `docs/platform-prerequisites.md`
  - `README.md`
- Updated master implementation plan with Phase 18 milestone:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/validate-diagnostics-exit-code-contract.sh`
- `shellcheck scripts/validate-diagnostics-exit-code-contract.sh scripts/run-platform-diagnostics-report.sh scripts/update-blocking-baseline.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/exit-code-contract-debug --baseline ci/baselines/blocking-issues-baseline.txt`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
