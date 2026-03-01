# CI and Release

## Pull request CI

Workflow: `.github/workflows/ci.yml`

Runs on:

- `ubuntu-latest`
- `macos-latest`
- `windows-latest`

Stages:

1. Quality gate (`dotnet format --verify-no-changes --severity warn`) on Ubuntu.
2. Changelog fragment validation (`./scripts/validate-changelog-fragments.sh`) on Ubuntu.
3. Restore, build, and test (`Debug`) on Windows, macOS, and Linux.
   - Includes platform prerequisite diagnostics coverage in core tests.
4. Ubuntu `Release` build plus diagnostics exit-code contract validation and desktop/mobile-browser sample smoke runs.
   - Smoke scripts enable `NATIVEWEBVIEW_DIAGNOSTICS_REQUIRE_READY=1` to fail on blocking diagnostic issues.
   - Exit-code contract validation runs `scripts/validate-diagnostics-exit-code-contract.sh` with fingerprint baseline enforcement (`ci/baselines/diagnostics-fingerprint-baseline.txt`).
5. Generate platform diagnostics JSON + markdown reports (`scripts/run-platform-diagnostics-report.sh --allow-not-ready`).
6. Compare blocking diagnostics against baseline (`ci/baselines/blocking-issues-baseline.txt`) and fail on new baseline regressions.
7. Enforce baseline sync (`--require-baseline-sync`) so resolved/stale baseline entries fail the pipeline.
8. Publish diagnostics + regression + gate-evaluation markdown to workflow step summary.
   - Publishes exit-code contract conformance summary and fingerprint-baseline comparison markdown when fingerprint gating is enabled.
9. Publish machine-readable regression evaluation JSON artifact for policy/gate auditing.
   - Includes gate-specific exit code classification (`10`, `11`, `12`, `13`).
   - Includes structured `gateFailures` metadata with remediation recommendations.
   - Includes deterministic evaluation `fingerprint` and schema version (`fingerprintVersion`) for artifact correlation and drift detection.
   - Conformance artifact directory also includes machine-readable `exit-code-contract-summary.json`.
10. Package dry-run (`Release`) on Ubuntu.
11. Upload test, package, and diagnostics artifacts.

## Tag release

Workflow: `.github/workflows/release.yml`

Trigger:

- Push tag `v*` (for example `v0.1.0`)

Stages:

1. Resolve package version from tag.
2. Validate changelog fragments.
3. Restore, build, and test in `Release`.
4. Run diagnostics exit-code contract validation and desktop/mobile-browser sample smoke checks.
   - Exit-code contract validation runs `scripts/validate-diagnostics-exit-code-contract.sh` with fingerprint baseline enforcement (`ci/baselines/diagnostics-fingerprint-baseline.txt`).
5. Generate platform diagnostics JSON + markdown reports (`scripts/run-platform-diagnostics-report.sh --allow-not-ready`).
6. Compare blocking diagnostics against baseline (`ci/baselines/blocking-issues-baseline.txt`) and fail on new baseline regressions.
7. Enforce baseline sync (`--require-baseline-sync`) so resolved/stale baseline entries fail the pipeline.
8. Publish machine-readable regression evaluation JSON artifact for policy/gate auditing.
   - Includes gate-specific exit code classification (`10`, `11`, `12`, `13`).
   - Includes structured `gateFailures` metadata with remediation recommendations.
   - Includes deterministic evaluation `fingerprint` and schema version (`fingerprintVersion`) for artifact correlation and drift detection.
9. Pack all NuGet packages with `.snupkg` symbols.
10. Generate release notes from changelog fragments.
11. Append diagnostics + regression + gate-evaluation + exit-code conformance markdown summaries to release notes.
12. Upload package, release-note, and diagnostics artifacts.
13. Push `.nupkg` and `.snupkg` to nuget.org when `NUGET_API_KEY` secret is configured.
14. Create a GitHub release using generated release notes and attached artifacts.

## Docs Site Workflow

Workflow: `.github/workflows/docs.yml`

Stages:

1. Install MkDocs dependencies.
2. Build docs with `mkdocs build --strict`.
3. On pushes to `main`/`master`, publish built site to GitHub Pages.

Local docs build:

```bash
python -m pip install -r docs/requirements.txt
mkdocs build --strict
```

## Extended Validation Workflow

Workflow: `.github/workflows/extended-validation.yml`

Trigger:

- Manual (`workflow_dispatch`)
- Weekly schedule (`cron`)

Stages:

1. Browser docs validation via `scripts/run-browser-playwright-smoke.sh`.
2. iOS mobile/browser contract smoke with simulator boot on `macos-latest`.
3. Android mobile/browser contract smoke inside an emulator on `ubuntu-latest`.
4. Upload diagnostic artifacts (Playwright report, iOS simulator logs, Android logcat).

## Local packaging

```bash
dotnet build NativeWebView.sln -c Release
./scripts/run-platform-diagnostics-report.sh --configuration Release --no-build --platform all --output artifacts/diagnostics/platform-diagnostics-report.json --markdown-output artifacts/diagnostics/platform-diagnostics-report.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --blocking-baseline-output artifacts/diagnostics/current-blocking-baseline.txt --comparison-markdown-output artifacts/diagnostics/blocking-regression.md --comparison-json-output artifacts/diagnostics/blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/gate-evaluation.md --require-baseline-sync --allow-not-ready
./scripts/validate-diagnostics-exit-code-contract.sh --configuration Release --no-build --output-dir artifacts/diagnostics/exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt
dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages
```

When fingerprint baseline gating is enabled, conformance outputs include `fingerprint-baseline-comparison.md` and `fingerprint-baseline-comparison.json` under the exit-code contract output directory.
Conformance outputs also include `exit-code-contract-summary.json` for machine-readable per-scenario outcome auditing.

Refresh baseline when intentional prerequisite changes are accepted:

```bash
./scripts/update-blocking-baseline.sh --configuration Release --platform all --output ci/baselines/blocking-issues-baseline.txt
./scripts/update-diagnostics-fingerprint-baseline.sh --configuration Release --output ci/baselines/diagnostics-fingerprint-baseline.txt
```
