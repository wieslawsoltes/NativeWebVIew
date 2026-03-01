# Quickstart

## 1. Add packages

```bash
dotnet add package NativeWebView
dotnet add package NativeWebView.Core
dotnet add package NativeWebView.Platform.Windows
```

Replace the platform package with the one you target.

## 2. Register platform backends

```csharp
using NativeWebView.Core;
using NativeWebView.Platform.Windows;

var factory = new NativeWebViewBackendFactory();
factory.UseNativeWebViewWindows();
```

## 3. Create and initialize the control facade

```csharp
using NativeWebView.Controls;
using NativeWebView.Core;

if (!factory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Windows, out var backend))
{
    throw new InvalidOperationException("Backend not registered.");
}

using var webView = new NativeWebView(backend);
await webView.InitializeAsync();
webView.RenderMode = NativeWebViewRenderMode.GpuSurface;
webView.RenderFramesPerSecond = 30;

if (!webView.SupportsRenderMode(webView.RenderMode))
{
    webView.RenderMode = NativeWebViewRenderMode.Embedded;
}

webView.Navigate("https://example.com");

if (webView.RenderMode != NativeWebViewRenderMode.Embedded)
{
    var frame = await webView.CaptureRenderFrameAsync();
    Console.WriteLine($"Frame: id={frame?.FrameId}, origin={frame?.Origin}, mode={frame?.RenderMode}");
    var stats = webView.GetRenderStatisticsSnapshot();
    Console.WriteLine($"Capture stats: attempts={stats.CaptureAttemptCount}, success={stats.CaptureSuccessCount}, failures={stats.CaptureFailureCount}");
    await webView.SaveRenderFrameAsync("artifacts/nativewebview-frame.png");
    await webView.SaveRenderFrameWithMetadataAsync(
        "artifacts/nativewebview-frame-with-metadata.png",
        "artifacts/nativewebview-frame-with-metadata.json");
    // Sidecar (FormatVersion=2) includes integrity fields: PixelDataLength + PixelDataSha256.
    if (frame is not null)
    {
        var sidecar = await NativeWebViewRenderFrameMetadataSerializer.ReadFromFileAsync(
            "artifacts/nativewebview-frame-with-metadata.json");
        var integrityOk = NativeWebViewRenderFrameMetadataSerializer.TryVerifyIntegrity(frame, sidecar, out var integrityError);
        Console.WriteLine($"Integrity ok: {integrityOk}, error={integrityError ?? "<null>"}");
    }
    webView.ResetRenderStatistics();
}
```

## 4. Validate platform prerequisites (recommended)

```csharp
var diagnostics = factory.GetPlatformDiagnosticsOrDefault(NativeWebViewPlatform.Windows);
if (!diagnostics.IsReady)
{
    throw new InvalidOperationException("Platform prerequisites are not satisfied.");
}
```

Or enforce using the built-in validator:

```csharp
var diagnostics = factory.GetPlatformDiagnosticsOrDefault(NativeWebViewPlatform.Windows);
NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics);
```

## 5. Optional diagnostics report for CI/local gates

```bash
./scripts/run-platform-diagnostics-report.sh \
  --configuration Release \
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

Conformance outputs include `exit-code-contract-summary.json` for machine-readable per-scenario results.
When fingerprint baseline gating is enabled, conformance outputs also include `fingerprint-baseline-comparison.md` and `fingerprint-baseline-comparison.json`.

## 6. Optional runtime auto-registration

If you use default constructors, runtime registration can load the current platform module automatically:

```csharp
NativeWebViewRuntime.EnsureCurrentPlatformRegistered();
```

## 7. Desktop sample app

Launch the Avalonia desktop feature explorer sample:

```bash
dotnet run --project samples/NativeWebView.Sample.Desktop/NativeWebView.Sample.Desktop.csproj -c Debug
```

For deterministic backend-contract smoke mode:

```bash
dotnet run --project samples/NativeWebView.Sample.Desktop/NativeWebView.Sample.Desktop.csproj -c Debug -- --smoke
```
