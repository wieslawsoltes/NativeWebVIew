# Phase 13 Diagnostics Markdown Summaries Checklist

## Scope

- Add human-readable diagnostics markdown formatter in core APIs.
- Emit markdown summary from diagnostics sample and wrapper script.
- Surface diagnostics markdown in CI step summary and release notes.
- Update docs and plan for markdown diagnostics outputs.

## Implemented

- Added diagnostics markdown formatter API:
  - `src/NativeWebView.Core/DiagnosticsMarkdownFormatter.cs`
  - `NativeWebViewDiagnosticsMarkdownFormatter.FormatReport(...)`
- Added markdown formatter tests:
  - `tests/NativeWebView.Core.Tests/DiagnosticsMarkdownFormatterTests.cs`
- Extended diagnostics sample CLI for markdown output:
  - `samples/NativeWebView.Sample.Diagnostics/Program.cs`
  - Added `--markdown-output <path>` option.
- Extended diagnostics wrapper script:
  - `scripts/run-platform-diagnostics-report.sh`
  - Added `--markdown-output <path>` forwarding and output verification.
- Updated CI workflow to publish markdown diagnostics summary:
  - `.github/workflows/ci.yml`
  - Writes markdown to `$GITHUB_STEP_SUMMARY` and uploads JSON/Markdown artifacts.
- Updated release workflow to include markdown diagnostics in release notes:
  - `.github/workflows/release.yml`
  - Generates markdown diagnostics, appends summary to release notes, uploads/attaches JSON+Markdown artifacts.
- Updated docs and README:
  - `docs/platform-diagnostics-report.md`
  - `docs/ci-and-release.md`
  - `docs/platform-prerequisites.md`
  - `docs/quickstart.md`
  - `README.md`
- Updated master implementation plan with Phase 13 milestone:
  - `plan/native-webview-native-control-implementation-plan.md`

## Validation

- `bash -n scripts/run-platform-diagnostics-report.sh`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `./scripts/run-platform-diagnostics-report.sh --configuration Debug --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-debug.json --markdown-output artifacts/diagnostics/platform-diagnostics-debug.md --require-ready`
- `dotnet format NativeWebView.sln --verify-no-changes --severity warn`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages -p:ContinuousIntegrationBuild=true`
