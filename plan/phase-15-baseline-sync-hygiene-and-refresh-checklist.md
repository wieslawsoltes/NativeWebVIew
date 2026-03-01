# Phase 15 Baseline Sync Hygiene and Refresh Checklist

## Scope

- Add stale-baseline/update-needed regression result flags.
- Extend diagnostics sample and wrapper script with baseline-sync gate support.
- Add a baseline refresh helper script for deterministic baseline regeneration.
- Enforce baseline-sync gate in CI/release and update docs/plan.

## Implemented

- Extended diagnostics regression result flags:
  - `src/NativeWebView.Core/DiagnosticsRegression.cs`
  - Added:
    - `HasStaleBaseline`
    - `RequiresBaselineUpdate`
- Extended regression markdown summary output:
  - `src/NativeWebView.Core/DiagnosticsRegressionMarkdownFormatter.cs`
  - Added stale/update status lines.
- Extended regression tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionAnalyzerTests.cs`
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionMarkdownFormatterTests.cs`
- Extended diagnostics sample CLI baseline-sync policy:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Added `--require-baseline-sync` option and fail-fast behavior for stale baseline entries.
- Extended wrapper script baseline-sync forwarding:
  - `scripts/run-platform-diagnostics-report.sh`
  - Added `--require-baseline-sync` and guard requiring `--blocking-baseline`.
- Added baseline refresh automation script:
  - `scripts/update-blocking-baseline.sh`
  - Regenerates blocking baseline from current diagnostics via wrapper script.
- Enforced baseline-sync gate in CI/release diagnostics jobs:
  - `.github/workflows/ci.yml`
  - `.github/workflows/release.yml`
- Updated docs and README for baseline-sync and refresh workflow:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/platform-prerequisites.md`
  - `docs/quickstart.md`
  - `README.md`
- Updated master implementation plan with Phase 15 milestone:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `bash -n scripts/update-blocking-baseline.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-debug.json --markdown-output artifacts/diagnostics/platform-diagnostics-debug.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --blocking-baseline-output artifacts/diagnostics/current-blocking-baseline-debug.txt --comparison-markdown-output artifacts/diagnostics/blocking-regression-debug.md --require-baseline-sync --allow-not-ready`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/stale-check.json --blocking-baseline /tmp/nwv_stale_baseline.txt --require-baseline-sync --allow-not-ready` (expected non-zero when baseline contains stale entries)
- `./scripts/update-blocking-baseline.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/refreshed-blocking-baseline-debug.txt`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
