# Phase 28 Render Frame Metadata and Events Checklist

## Scope

- Add explicit metadata to captured render frames so consumers can reason about sequence, time, source, and mode.
- Add a `NativeWebView` frame-captured event for push-based integrations (diagnostics, telemetry, automation).

## Contract Additions

- Add `NativeWebViewRenderFrameOrigin` enum.
- Extend `NativeWebViewRenderFrame` with:
  - `FrameId`
  - `CapturedAtUtc`
  - `RenderMode`
  - `Origin`
- Add `NativeWebViewRenderFrameCapturedEventArgs`.
- Add `NativeWebView.RenderFrameCaptured` event.

## Behavior Contract

- Synthetic fallback frames report:
  - `Origin = SyntheticFallback`
  - monotonic `FrameId`
  - requested `RenderMode`
- Native-captured frames report:
  - `Origin = NativeCapture`
  - monotonic `FrameId`
  - requested `RenderMode`
- `RenderFrameCaptured` fires for successful composited captures.

## Implementation Checklist

- [x] Core contracts
  - Add frame origin enum and metadata properties.
  - Add frame captured event args type.
- [x] Backends
  - Populate frame metadata in stub fallback source.
  - Populate frame metadata in macOS native capture source.
- [x] Control
  - Add frame captured event and raise on successful capture.
- [x] Sample
  - Surface metadata details in capture summary text.
- [x] Tests
  - Assert metadata semantics for fallback frames.
- [x] Docs
  - Document event and frame metadata usage.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`

## Exit Criteria

- Frame metadata and event APIs are public and documented.
- Tests assert frame metadata behavior.
- Build/tests/smoke remain green.
