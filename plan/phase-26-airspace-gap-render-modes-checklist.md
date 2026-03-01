# Phase 26 Airspace Gap Render Modes Checklist

## Scope

- Add first-class render mode selection to `NativeWebView` so airspace-sensitive layouts can avoid native child overlay issues.
- Implement two new render paths in addition to embedded mode:
  - `GpuSurface`: reusable GPU-uploaded bitmap surface fed by frame capture.
  - `Offscreen`: frame-by-frame offscreen capture composited by Avalonia.
- Ensure mode switching works at runtime and remains API-compatible with existing navigation/script/dialog/auth surface.
- Wire capabilities and diagnostics so each platform reports render-mode support consistently.
- Update desktop sample, docs, and validation scripts to demonstrate and verify both modes.

## Rendering Contract

- Add `NativeWebViewRenderMode` enum:
  - `Embedded`
  - `GpuSurface`
  - `Offscreen`
- Add render feature flags:
  - `NativeWebViewFeature.GpuSurfaceRendering`
  - `NativeWebViewFeature.OffscreenRendering`
- Add frame contract in `NativeWebView.Core`:
  - `NativeWebViewRenderPixelFormat`
  - `NativeWebViewRenderFrame`
  - `INativeWebViewFrameSource`
- Frame-source contract must support:
  - capability probe per mode
  - frame capture with requested pixel size
  - deterministic fallback behavior when native capture is unavailable

## Control Behavior Contract

- `NativeWebView` exposes:
  - `RenderMode` (default `Embedded`)
  - `RenderFramesPerSecond` (default 30)
  - `SupportsRenderMode(...)`
- `Embedded` mode:
  - preserves current native host behavior
  - native control host remains interactive
- `GpuSurface` and `Offscreen` modes:
  - disable direct embedded airspace rendering
  - run periodic frame pump
  - render captured frames into Avalonia visual tree
  - keep navigation/script/message APIs operational
- Mode switch behavior:
  - hot-switch without recreating control instance
  - clear stale surfaces and refresh summaries/diagnostics

## Platform Matrix (Phase 26)

- Windows: render modes exposed via shared frame-source fallback provider.
- macOS: render modes exposed with native `WKWebView` frame capture path.
- Linux: render modes exposed via shared frame-source fallback provider.
- iOS: render modes exposed via shared frame-source fallback provider.
- Android: render modes exposed via shared frame-source fallback provider.
- Browser: render modes exposed via shared frame-source fallback provider.

## Implementation Checklist

- [x] Core abstractions
  - Add render mode enum and frame contracts.
  - Add frame-source interface and capability hooks.
  - Extend feature flags for new modes.
- [x] Backend support
  - Implement deterministic fallback frame-source in stub base backend.
  - Ensure all platform backend stubs inherit render-source support.
- [x] macOS native host support
  - Add frame capture helpers from `WKWebView`/`NSView` backing surface.
  - Add mode-aware native host visibility/placement logic.
- [x] Avalonia control
  - Add mode properties and runtime switching.
  - Add frame pump lifecycle (`Attached`/`Detached`/`Dispose`).
  - Add GPU-surface and offscreen drawing paths.
- [x] Sample app
  - Add render mode controls into top-right menu/flyout.
  - Surface active mode and support matrix in diagnostics panel.
  - Ensure startup path keeps correct sizing and first-frame behavior.
- [x] Docs
  - Update `docs/nativewebview.md` and `README.md` with mode usage and platform behavior.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`

## Exit Criteria

- Render mode API available in public control surface and documented.
- Desktop sample can switch between `Embedded`, `GpuSurface`, and `Offscreen` at runtime.
- `GpuSurface` and `Offscreen` render without native airspace overlap in sample host layout.
- Solution builds and tests pass with no regressions in existing matrix tests.
