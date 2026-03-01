# Phase 27 Render Frame Capture and Export Checklist

## Scope

- Add public render-frame capture API to `NativeWebView` so applications can snapshot current web content when using composited render modes.
- Add export API for writing captured frames to disk (`png`) for diagnostics and automation.
- Keep behavior deterministic on all platforms via native capture path where available and fallback capture source otherwise.

## Contract Additions

- Add feature flag:
  - `NativeWebViewFeature.RenderFrameCapture`
- Add `NativeWebView` methods:
  - `Task<NativeWebViewRenderFrame?> CaptureRenderFrameAsync(CancellationToken)`
  - `Task<bool> SaveRenderFrameAsync(string outputPath, CancellationToken)`

## Behavior Contract

- `CaptureRenderFrameAsync`:
  - returns `null` in `Embedded` mode.
  - returns latest captured frame model in `GpuSurface`/`Offscreen` modes.
  - updates render diagnostics when capture is unavailable.
- `SaveRenderFrameAsync`:
  - returns `false` if no frame is available.
  - writes png artifact to requested path when capture succeeds.
  - creates directory if needed.

## Implementation Checklist

- [x] Core/contracts
  - Add `RenderFrameCapture` feature flag.
  - Add feature support on all platform feature sets.
- [x] Control API
  - Implement frame capture/export methods in `NativeWebView`.
  - Reuse existing frame-copy pipeline and avoid duplicated conversion logic.
- [x] Sample
  - Add menu actions for capture and png export.
  - Add render capture details in state diagnostics output.
- [x] Tests
  - Add contract tests for capture flag and frame availability behavior in stub backends.
- [x] Docs
  - Update README and API docs with new methods and expected behavior.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`

## Exit Criteria

- Public capture/export methods available and documented.
- Desktop sample can capture and export a composited frame.
- All tests and smoke checks pass.
