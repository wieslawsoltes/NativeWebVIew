# Platform Prerequisites and Diagnostics

`NativeWebView` exposes runtime prerequisite diagnostics so you can validate host setup before creating controls.

## API

Use the backend factory after registering a platform module:

```csharp
using NativeWebView.Core;
using NativeWebView.Platform.Linux;

var factory = new NativeWebViewBackendFactory()
    .UseNativeWebViewLinux();

var diagnostics = factory.GetPlatformDiagnosticsOrDefault(NativeWebViewPlatform.Linux);
if (!diagnostics.IsReady)
{
    throw new InvalidOperationException("Platform prerequisites are not satisfied.");
}

foreach (var issue in diagnostics.Issues)
{
    Console.WriteLine($"{issue.Severity}: {issue.Code} - {issue.Message}");
}
```

You can also use runtime auto-registration:

```csharp
var diagnostics = NativeWebViewRuntime.GetCurrentPlatformDiagnostics();
```

## Diagnostic semantics

- `Info`: informative status message.
- `Warning`: host mismatch or non-blocking prerequisite risk.
- `Error`: blocking prerequisite issue; treat as startup failure.

## Environment variables recognized by diagnostics

- `NATIVEWEBVIEW_WEBVIEW2_RUNTIME_PATH` (Windows): optional runtime path override (must exist when set).
- `NATIVEWEBVIEW_WEBKITGTK_VERSION` (Linux): expected installed WebKitGTK version (`4.1+`).
- `ANDROID_API_LEVEL` (Android): API level used for minimum-level enforcement (`24+`).
- `NATIVEWEBVIEW_BROWSER_POPUP_SUPPORT` (Browser): set `false`/`0` to force popup-support warning.

## Recommended startup pattern

1. Register the platform backend module.
2. Read diagnostics for the active platform.
3. Fail fast when any issue has `Error` severity.
4. Log warnings with remediation guidance.

## Strict validation helper

Use `NativeWebViewDiagnosticsValidator` when you want policy-driven gating:

```csharp
var diagnostics = NativeWebViewRuntime.GetCurrentPlatformDiagnostics();

// Fails on Error issues (default policy).
NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics);

// Optional strict mode: warnings are also treated as blockers.
NativeWebViewDiagnosticsValidator.EnsureReady(
    diagnostics,
    warningsAsErrors: true);
```

Sample applications in this repository support:

- `NATIVEWEBVIEW_DIAGNOSTICS_REQUIRE_READY=1`
- `NATIVEWEBVIEW_DIAGNOSTICS_WARNINGS_AS_ERRORS=1`

When enabled, samples call `NativeWebViewDiagnosticsValidator.EnsureReady(...)` and exit on blocking issues.

## Cross-platform diagnostics report

Generate a JSON report for one or more platforms:

```bash
./scripts/run-platform-diagnostics-report.sh \
  --configuration Release \
  --no-build \
  --platform all \
  --output artifacts/diagnostics/platform-diagnostics-report.json \
  --markdown-output artifacts/diagnostics/platform-diagnostics-report.md \
  --blocking-baseline ci/baselines/blocking-issues-baseline.txt \
  --comparison-markdown-output artifacts/diagnostics/blocking-regression.md \
  --comparison-json-output artifacts/diagnostics/blocking-regression.json \
  --comparison-evaluation-markdown-output artifacts/diagnostics/gate-evaluation.md \
  --require-baseline-sync \
  --allow-not-ready

./scripts/validate-diagnostics-exit-code-contract.sh \
  --configuration Release \
  --no-build \
  --output-dir artifacts/diagnostics/exit-code-contract \
  --baseline ci/baselines/blocking-issues-baseline.txt \
  --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt
```

Use `--allow-not-ready` to produce the report without failing the command when blocking issues are present.
Conformance outputs include `exit-code-contract-summary.json` for machine-readable per-scenario results.
Fingerprint baseline gating also emits `fingerprint-baseline-comparison.md` and `fingerprint-baseline-comparison.json` for drift triage.
Use `./scripts/update-blocking-baseline.sh` to regenerate `ci/baselines/blocking-issues-baseline.txt` when baseline updates are intentional.
