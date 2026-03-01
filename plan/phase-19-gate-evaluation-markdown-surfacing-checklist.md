# Phase 19 Gate Evaluation Markdown Surfacing Checklist

## Scope

- Add a human-readable markdown formatter for diagnostics gate evaluation outcomes.
- Extend diagnostics CLI + wrapper script with gate evaluation markdown output option.
- Enforce gate evaluation markdown generation in exit-code contract conformance automation.
- Publish gate evaluation markdown artifacts in CI workflow summary and release notes.

## Implemented

- Added gate evaluation markdown formatter:
  - `src/NativeWebView.Core/DiagnosticsRegressionEvaluationMarkdownFormatter.cs`
  - Outputs policy flags, readiness/comparison state, effective exit code, and failing gate details.
- Added formatter unit tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsRegressionEvaluationMarkdownFormatterTests.cs`
- Extended diagnostics sample CLI:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Added `--comparison-evaluation-markdown-output <path>`.
  - Writes gate evaluation markdown using the new formatter.
- Extended wrapper script:
  - `scripts/run-platform-diagnostics-report.sh`
  - Added `--comparison-evaluation-markdown-output`.
  - Validates generated markdown artifact and expected heading.
- Extended conformance automation:
  - `scripts/validate-diagnostics-exit-code-contract.sh`
  - Produces/validates gate evaluation markdown for all scenarios.
  - Verifies expected `Effective Exit Code: <code>` in markdown outputs.
- Updated CI/release workflows:
  - `.github/workflows/ci.yml`
  - `.github/workflows/release.yml`
  - Gate evaluation markdown is generated, published, and uploaded as artifact.
  - Release notes now append gate evaluation markdown summary.
- Updated docs and plan:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/quickstart.md`
  - `docs/platform-prerequisites.md`
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
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/phase19-platform-diagnostics.json --markdown-output artifacts/diagnostics/phase19-platform-diagnostics.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/phase19-blocking-regression.md --comparison-json-output artifacts/diagnostics/phase19-blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/phase19-gate-evaluation.md --require-baseline-sync --allow-not-ready`
- `./scripts/validate-diagnostics-exit-code-contract.sh --configuration Debug --no-build --output-dir artifacts/diagnostics/phase19-exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
- `./scripts/validate-changelog-fragments.sh`
