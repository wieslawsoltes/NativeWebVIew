# Platform Diagnostics Report

Use the diagnostics reporter to generate a JSON summary for one or more platforms and optionally fail when blocking issues are detected.

## CLI sample

Project:

- `samples/NativeWebView.Sample.Diagnostics`

Example:

```bash
dotnet run --project samples/NativeWebView.Sample.Diagnostics/NativeWebView.Sample.Diagnostics.csproj -- \
  --platform all \
  --output artifacts/diagnostics/platform-diagnostics-report.json \
  --require-ready
```

Options:

- `--platform <value>`: `all` or a comma-separated subset of `windows,macos,linux,ios,android,browser`.
- `--output <path>`: output JSON file path.
- `--markdown-output <path>`: optional markdown summary output path.
- `--blocking-baseline <path>`: optional baseline file (`<Platform>|<IssueCode>`) for blocking issue regression checks.
- `--blocking-baseline-output <path>`: optional output path to write current blocking issue baseline entries.
- `--comparison-markdown-output <path>`: optional markdown output path for baseline comparison summary.
- `--comparison-json-output <path>`: optional JSON output path for baseline comparison evaluation summary.
- `--comparison-evaluation-markdown-output <path>`: optional markdown output path for gate evaluation summary.
- `--require-ready`: enable readiness gate (returns gate-specific non-zero exit code on failure).
- `--warnings-as-errors`: treat warnings as blocking when calculating readiness.
- `--allow-regression`: do not fail when new blocking issues are found against baseline.
- `--require-baseline-sync`: fail when the baseline contains resolved/stale entries (requires `--blocking-baseline`).

## Exit code contract

- `0`: no enabled gates failed.
- `10`: readiness gate failed (`--require-ready`).
- `11`: regression gate failed (new blocking issues with regression gating enabled).
- `12`: baseline-sync gate failed (`--require-baseline-sync` with stale baseline entries).
- `13`: multiple enabled gates failed in one run.

## Wrapper script

Use the repository wrapper script for CI/local consistency:

```bash
./scripts/run-platform-diagnostics-report.sh \
  --configuration Release \
  --no-build \
  --platform all \
  --output artifacts/diagnostics/platform-diagnostics-report.json \
  --markdown-output artifacts/diagnostics/platform-diagnostics-report.md \
  --blocking-baseline ci/baselines/blocking-issues-baseline.txt \
  --blocking-baseline-output artifacts/diagnostics/current-blocking-baseline.txt \
  --comparison-markdown-output artifacts/diagnostics/blocking-regression.md \
  --comparison-json-output artifacts/diagnostics/blocking-regression.json \
  --comparison-evaluation-markdown-output artifacts/diagnostics/gate-evaluation.md \
  --require-baseline-sync \
  --allow-not-ready
```

Additional script flag:

- `--allow-not-ready`: generate report without failing the command when blocking issues are present.
- By default, `run-platform-diagnostics-report.sh` enforces readiness (`--require-ready` behavior).
- By default, baseline comparison failures (new blocking issues) fail the command unless `--allow-regression` is set.
- `--require-baseline-sync` enforces baseline hygiene by failing when resolved baseline entries are detected.

## Report shape

Top-level fields:

- `generatedAtUtc`
- `warningsAsErrors`
- `isReady`
- `issueCount`
- `warningCount`
- `errorCount`
- `blockingIssueCount`
- `platforms`

Each platform entry includes:

- `platform`
- `providerName`
- `providerRegistered`
- `isReady`
- `issueCount`
- `warningCount`
- `errorCount`
- `blockingIssueCount`
- `issues[]` (`code`, `severity`, `message`, `recommendation`)

Markdown output includes:

- Overall readiness and counts.
- Platform summary table.
- Per-platform issue sections.

Blocking regression markdown output includes:

- Baseline/current blocking issue counts.
- New blocking issues.
- Resolved blocking issues.
- Regression flag (`Has Regression`).
- Stale/update flags (`Has Stale Baseline`, `Requires Baseline Update`).

Blocking regression JSON output includes:

- Evaluation policy flags (`requireReady`, `failOnRegression`, `requireBaselineSync`).
- Gate outcomes (`wouldFailRequireReady`, `wouldFailRegressionGate`, `wouldFailBaselineSyncGate`).
- Final gate result (`effectiveExitCode`).
- Fingerprint schema version (`fingerprintVersion`, currently `1`).
- Deterministic evaluation fingerprint (`fingerprint`) for run-to-run contract auditing.
- Structured failing gate metadata (`gateFailures[]`) with `kind`, `exitCode`, `message`, and `recommendation`.
- Embedded comparison details when baseline comparison is enabled.

Gate evaluation markdown output includes:

- Policy flags and readiness/comparison state.
- Effective gate exit code and primary/combined failure classification.
- Deterministic evaluation fingerprint for artifact correlation.
- Optional baseline comparison snapshot counts.
- Failing gate list with gate-specific exit codes, descriptions, and recommendations.

## Baseline refresh helper

Use the helper script to regenerate the baseline from current diagnostics:

```bash
./scripts/update-blocking-baseline.sh \
  --configuration Release \
  --platform all \
  --output ci/baselines/blocking-issues-baseline.txt
```

Optional strict regeneration:

```bash
./scripts/update-blocking-baseline.sh --warnings-as-errors
```

## Exit code contract validation script

Use the dedicated validation script to exercise all gate outcomes (`0`, `10`, `11`, `12`, `13`):

```bash
./scripts/validate-diagnostics-exit-code-contract.sh \
  --configuration Release \
  --no-build \
  --output-dir artifacts/diagnostics/exit-code-contract \
  --baseline ci/baselines/blocking-issues-baseline.txt
```

Optional fingerprint baseline gate:

```bash
./scripts/validate-diagnostics-exit-code-contract.sh \
  --configuration Release \
  --no-build \
  --output-dir artifacts/diagnostics/exit-code-contract \
  --baseline ci/baselines/blocking-issues-baseline.txt \
  --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt
```

Outputs include:

- `exit-code-contract-summary.md`
- `exit-code-contract-summary.csv`
- `exit-code-contract-summary.json` (machine-readable conformance summary with per-case outcomes/fingerprints)
- scenario logs and generated evaluation/report JSON files
- per-scenario gate evaluation markdown files (`*-gate-evaluation.md`)
- conformance markdown summary includes per-scenario evaluation fingerprints
- generated current fingerprint contract file (`fingerprint-current.txt`)
- fingerprint baseline drift comparison markdown (`fingerprint-baseline-comparison.md`) when `--fingerprint-baseline` is enabled
- fingerprint baseline drift comparison JSON (`fingerprint-baseline-comparison.json`) when `--fingerprint-baseline` is enabled

Refresh fingerprint baseline when fingerprint contract changes are intentional:

```bash
./scripts/update-diagnostics-fingerprint-baseline.sh \
  --configuration Release \
  --output ci/baselines/diagnostics-fingerprint-baseline.txt
```

## CI and release usage

- CI (`.github/workflows/ci.yml`) generates and uploads `artifacts/diagnostics/ci-platform-diagnostics.json`.
- CI (`.github/workflows/ci.yml`) also publishes diagnostics, exit-code contract, and fingerprint baseline comparison markdown into the workflow run summary.
- CI and release compare blocking issues against `ci/baselines/blocking-issues-baseline.txt` and enforce baseline-sync hygiene.
- CI and release also publish regression evaluation JSON artifacts (`ci-blocking-regression.json`, `release-blocking-regression-<version>.json`).
- CI and release publish gate evaluation markdown artifacts (`ci-gate-evaluation.md`, `release-gate-evaluation-<version>.md`) for human-readable triage.
- Release (`.github/workflows/release.yml`) generates JSON/markdown diagnostics artifacts, appends diagnostics + regression + gate evaluation + exit-code conformance summaries to release notes, and attaches all artifacts to the GitHub release.
