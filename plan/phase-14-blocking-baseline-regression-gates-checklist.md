# Phase 14 Blocking Baseline Regression Gates Checklist

## Scope

- Add reusable APIs for blocking diagnostics baseline parsing/serialization/comparison.
- Extend diagnostics sample and wrapper script for baseline-gated regression checks.
- Enforce baseline comparison in CI/release and publish regression markdown artifacts.
- Update docs and plan for baseline regression workflows.

## Implemented

- Added blocking diagnostics regression APIs:
  - `src/NativeWebView.Core/DiagnosticsRegression.cs`
  - `NativeWebViewDiagnosticsRegressionAnalyzer.GetBlockingIssues(...)`
  - `NativeWebViewDiagnosticsRegressionAnalyzer.ParseBaselineLines(...)`
  - `NativeWebViewDiagnosticsRegressionAnalyzer.CompareBlockingIssues(...)`
  - `NativeWebViewDiagnosticsRegressionAnalyzer.SerializeBaselineLines(...)`
  - `NativeWebViewDiagnosticsRegressionResult`
- Added regression markdown formatter:
  - `src/NativeWebView.Core/DiagnosticsRegressionMarkdownFormatter.cs`
- Added regression unit tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionAnalyzerTests.cs`
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionMarkdownFormatterTests.cs`
- Extended diagnostics CLI sample options:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Added baseline/compare options:
    - `--blocking-baseline`
    - `--blocking-baseline-output`
    - `--comparison-markdown-output`
    - `--allow-regression`
- Extended wrapper script options and output checks:
  - `scripts/run-platform-diagnostics-report.sh`
- Added repository baseline file:
  - `ci/baselines/blocking-issues-baseline.txt`
- Updated CI baseline regression gate and artifact publication:
  - `.github/workflows/ci.yml`
- Updated release baseline regression gate, release-note append, and artifact attachments:
  - `.github/workflows/release.yml`
- Updated docs and README:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/platform-prerequisites.md`
  - `docs/quickstart.md`
  - `README.md`
- Updated master implementation plan with Phase 14 milestone:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-debug.json --markdown-output artifacts/diagnostics/platform-diagnostics-debug.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --blocking-baseline-output artifacts/diagnostics/current-blocking-baseline-debug.txt --comparison-markdown-output artifacts/diagnostics/blocking-regression-debug.md --allow-not-ready`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
