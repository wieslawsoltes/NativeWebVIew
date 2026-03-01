# Phase 30 Render Frame Sidecar Metadata Export Checklist

## Scope

- Add a public API to export a captured frame together with a JSON metadata sidecar.
- Keep export deterministic for diagnostics and automation pipelines.
- Surface the new export action in the desktop sample.

## Contract Additions

- Add `NativeWebViewRenderFrameExportMetadata`.
- Add `NativeWebViewRenderFrameMetadataSerializer`.
- Add `NativeWebView.SaveRenderFrameWithMetadataAsync(...)`.

## Behavior Contract

- PNG and metadata JSON export is available in composited render modes when frame capture is available.
- Metadata captures:
  - frame metadata (`FrameId`, timestamps, dimensions, format, origin/synthetic).
  - runtime render state (`Platform`, `RenderMode`, fps, URL, diagnostics message).
  - current render statistics snapshot.
- If metadata output path is omitted, default to `<image-output-path>.json`.

## Implementation Checklist

- [x] Core contracts
  - Add metadata DTO and serializer helper.
- [x] Control API
  - Implement `SaveRenderFrameWithMetadataAsync`.
  - Reuse shared png export pipeline.
- [x] Sample
  - Add menu action to export frame + metadata.
  - Surface last saved metadata path in state summary.
- [x] Tests
  - Add coverage for export behavior and generated metadata fields.
- [x] Docs
  - Update README and docs with sidecar export method.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug --no-build`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
  - `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`

## Exit Criteria

- New sidecar export API is public and documented.
- Metadata export behavior is test-covered.
- Build/tests/smoke checks are green.
