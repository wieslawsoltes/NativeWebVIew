# NativeWebView

NativeWebView provides a native-webview-first control stack for Avalonia without bundling Chromium.

## What You Get

- `NativeWebView` embedded control facade.
- `NativeWebDialog` dialog/window browser facade.
- `WebAuthenticationBroker` cross-platform auth facade.
- Shared feature/capability model and backend factory.
- Platform-specific backend packages for Windows, macOS, Linux, iOS, Android, and Browser.
- Optional airspace-mitigation rendering modes (`GpuSurface`, `Offscreen`) in the `NativeWebView` control.

## Package Layout

| Package | Purpose |
| --- | --- |
| `NativeWebView` | Avalonia control facade API. |
| `NativeWebView.Core` | Shared contracts, controllers, feature model, and backend factory. |
| `NativeWebView.Dialog` | Dialog facade API. |
| `NativeWebView.Auth` | Web authentication broker facade API. |
| `NativeWebView.Interop` | Native handle contracts and structs. |
| `NativeWebView.Platform.Windows` | Windows backend registration and implementation. |
| `NativeWebView.Platform.macOS` | macOS backend registration and implementation. |
| `NativeWebView.Platform.Linux` | Linux backend registration and implementation. |
| `NativeWebView.Platform.iOS` | iOS backend registration and implementation. |
| `NativeWebView.Platform.Android` | Android backend registration and implementation. |
| `NativeWebView.Platform.Browser` | Browser backend registration and implementation. |

## Platform Support Matrix

| Platform | Embedded WebView | GPU Surface Mode | Offscreen Mode | Dialog | Authentication Broker | Native Handles |
| --- | --- | --- | --- | --- | --- | --- |
| Windows | Yes | Yes | Yes | Yes | Yes | Yes |
| macOS | Yes | Yes | Yes | Yes | Yes | Yes |
| Linux | Yes | Yes | Yes | Yes | Yes | Yes |
| iOS | Yes | Yes | Yes | No | Yes | Yes |
| Android | Yes | Yes | Yes | No | Yes | Yes |
| Browser | Yes | Yes | Yes | No | Yes | Yes |

## Install

Add the shared package and the platform package(s) you need:

```bash
dotnet add package NativeWebView
dotnet add package NativeWebView.Core
dotnet add package NativeWebView.Platform.Windows
```

Use the platform package that matches your target runtime.

## Quick Start

```csharp
using NativeWebView.Controls;
using NativeWebView.Core;
using NativeWebView.Platform.Windows;

var factory = new NativeWebViewBackendFactory()
    .UseNativeWebViewWindows();

if (!factory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Windows, out var backend))
{
    throw new InvalidOperationException("Windows backend is not available.");
}

using var webView = new NativeWebView(backend);
await webView.InitializeAsync();
webView.RenderMode = NativeWebViewRenderMode.GpuSurface;
webView.RenderFramesPerSecond = 30;
webView.Navigate("https://example.com");
```

## Airspace Mitigation Modes

- `RenderMode = Embedded`: native child host mode (highest interaction fidelity).
- `RenderMode = GpuSurface`: draws web frames into a reusable Avalonia surface.
- `RenderMode = Offscreen`: draws web frames captured offscreen.
- Runtime checks:
  - `webView.SupportsRenderMode(mode)`
  - `webView.IsUsingSyntheticFrameSource`
  - `webView.RenderDiagnosticsMessage`
  - `webView.RenderStatistics` / `webView.GetRenderStatisticsSnapshot()`
  - `webView.ResetRenderStatistics()`
  - `var capturedFrame = await webView.CaptureRenderFrameAsync()`
  - `await webView.SaveRenderFrameAsync("artifacts/frame.png")`
  - `await webView.SaveRenderFrameWithMetadataAsync("artifacts/frame.png", "artifacts/frame.json")`
  - `var sidecar = await NativeWebViewRenderFrameMetadataSerializer.ReadFromFileAsync("artifacts/frame.json")`
  - `if (capturedFrame is not null) NativeWebViewRenderFrameMetadataSerializer.TryVerifyIntegrity(capturedFrame, sidecar, out var integrityError)`
  - frame metadata: `FrameId`, `CapturedAtUtc`, `RenderMode`, `Origin`
  - sidecar integrity metadata: `PixelDataLength`, `PixelDataSha256` (schema `FormatVersion = 2`, BGRA hash excludes row-stride padding bytes; verification requires matching `FormatVersion`)
  - capture statistics: attempts/success/failure/skip and frame-source breakdown (`SyntheticFrameCount`, `NativeFrameCount`)

## Desktop Feature Sample

Run the Avalonia desktop feature explorer (default mode):

```bash
dotnet run --project samples/NativeWebView.Sample.Desktop/NativeWebView.Sample.Desktop.csproj -c Debug
```

Run the deterministic smoke matrix mode used by CI:

```bash
dotnet run --project samples/NativeWebView.Sample.Desktop/NativeWebView.Sample.Desktop.csproj -c Debug -- --smoke
```

## Documentation

- [Quickstart](docs/quickstart.md)
- [NativeWebView API](docs/nativewebview.md)
- [NativeWebDialog API](docs/nativewebdialog.md)
- [WebAuthenticationBroker API](docs/webauthenticationbroker.md)
- [Environment Options](docs/interop/environment-options.md)
- [Native Handle Interop](docs/interop/native-browser-interop.md)
- [Platform Prerequisites and Diagnostics](docs/platform-prerequisites.md)
- [Platform Diagnostics Report](docs/platform-diagnostics-report.md)
- [Platform Notes: Windows](docs/platforms/windows.md)
- [Platform Notes: macOS](docs/platforms/macos.md)
- [Platform Notes: Linux](docs/platforms/linux.md)
- [Platform Notes: iOS](docs/platforms/ios.md)
- [Platform Notes: Android](docs/platforms/android.md)
- [Platform Notes: Browser](docs/platforms/browser.md)
- [CI and Release](docs/ci-and-release.md)

## Runtime Diagnostics

```csharp
NativeWebViewRuntime.EnsureCurrentPlatformRegistered();
var diagnostics = NativeWebViewRuntime.GetCurrentPlatformDiagnostics();

if (!diagnostics.IsReady)
{
    throw new InvalidOperationException(
        $"Platform prerequisites are not satisfied for {diagnostics.Platform}.");
}

NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics);
```

Generate a cross-platform diagnostics JSON artifact:

```bash
./scripts/run-platform-diagnostics-report.sh --configuration Release --platform all --output artifacts/diagnostics/platform-diagnostics-report.json --markdown-output artifacts/diagnostics/platform-diagnostics-report.md --blocking-baseline ci/baselines/blocking-issues-baseline.txt --comparison-markdown-output artifacts/diagnostics/blocking-regression.md --comparison-json-output artifacts/diagnostics/blocking-regression.json --comparison-evaluation-markdown-output artifacts/diagnostics/gate-evaluation.md --require-baseline-sync --allow-not-ready
./scripts/validate-diagnostics-exit-code-contract.sh --configuration Release --no-build --output-dir artifacts/diagnostics/exit-code-contract --baseline ci/baselines/blocking-issues-baseline.txt --fingerprint-baseline ci/baselines/diagnostics-fingerprint-baseline.txt
```

`blocking-regression.json` includes deterministic evaluation fingerprint metadata (`fingerprintVersion`, `fingerprint`) plus structured `gateFailures` (`kind`, `exitCode`, `message`, `recommendation`) for automation and triage tooling.
`validate-diagnostics-exit-code-contract.sh` also emits `fingerprint-current.txt` and, when `--fingerprint-baseline` is provided, drift summaries in `fingerprint-baseline-comparison.md` and `fingerprint-baseline-comparison.json`.
Conformance artifacts include `exit-code-contract-summary.json` with per-scenario expected/actual exit codes, pass/fail status, and fingerprints.

Refresh baseline when intentional blocking diagnostics changes are accepted:

```bash
./scripts/update-blocking-baseline.sh --configuration Release --platform all --output ci/baselines/blocking-issues-baseline.txt
./scripts/update-diagnostics-fingerprint-baseline.sh --configuration Release --output ci/baselines/diagnostics-fingerprint-baseline.txt
```

## Extended Validation

The repository includes `.github/workflows/extended-validation.yml` for scheduled/manual checks:

- Browser docs smoke checks using Playwright (`scripts/run-browser-playwright-smoke.sh`).
- iOS contract smoke with simulator boot (`scripts/run-ios-simulator-contract-smoke.sh`).
- Android contract smoke with emulator runner (`scripts/run-android-emulator-contract-smoke.sh`).

## Changelog Fragments and Release Notes

Releases use changelog fragments from `changelog/fragments` to generate release notes.

Validate fragments:

```bash
./scripts/validate-changelog-fragments.sh
```

Generate release notes preview:

```bash
./scripts/build-release-notes.sh --version 0.1.0 --output artifacts/release-notes.md
```

## Docs Site

The documentation site is built with MkDocs:

```bash
python -m pip install -r docs/requirements.txt
mkdocs build --strict
```

## Build and Validate

```bash
dotnet restore NativeWebView.sln
dotnet build NativeWebView.sln -c Debug
dotnet test NativeWebView.sln -c Debug
```

## License

MIT. See [LICENSE](LICENSE).
