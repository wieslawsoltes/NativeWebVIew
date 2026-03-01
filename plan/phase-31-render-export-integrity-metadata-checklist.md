# Phase 31 Render Export Integrity Metadata Checklist

## Scope

- Add deterministic integrity metadata to render sidecar JSON exports.
- Ensure integrity fields are stable and test-covered.
- Surface integrity capability in docs/sample messaging.

## Contract Additions

- Extend `NativeWebViewRenderFrameExportMetadata`:
  - `PixelDataLength`
  - `PixelDataSha256`

## Behavior Contract

- `PixelDataLength` is derived from the exported visible pixel-byte span used for hashing.
- `PixelDataSha256` is the SHA-256 hash of the exact byte span used for `PixelDataLength`.
- For BGRA frames, hashing excludes row-stride padding bytes and uses visible pixel bytes per row.
- Integrity fields are serialized for every `SaveRenderFrameWithMetadataAsync(...)` export.

## Implementation Checklist

- [x] Core contracts
  - Extend sidecar metadata DTO with integrity fields.
  - Compute deterministic hash and length in serializer helper.
- [x] Control
  - Reuse serializer path so integrity metadata is emitted in export API.
- [x] Sample
  - Update export log text to indicate integrity metadata presence.
- [x] Tests
  - Assert integrity fields in serializer object and written JSON payload.
- [x] Docs
  - Document integrity fields in README/API docs/quickstart.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug --no-build`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
  - `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`

## Exit Criteria

- Sidecar metadata includes deterministic integrity fields.
- Tests verify integrity field semantics.
- Build/tests/smoke checks are green.
