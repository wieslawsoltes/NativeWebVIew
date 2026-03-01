# Phase 32 Render Metadata Round-Trip and Integrity Verification Checklist

## Scope

- Add typed sidecar metadata read API for local tooling/automation.
- Add integrity verification helper to validate a frame against sidecar metadata.
- Surface read/verify usage in sample/docs.

## Contract Additions

- `NativeWebViewRenderFrameMetadataSerializer.ReadFromFileAsync(...)`
- `NativeWebViewRenderFrameMetadataSerializer.TryVerifyIntegrity(...)`

## Behavior Contract

- Sidecar metadata can be loaded from JSON to typed DTOs.
- Integrity verification checks both:
  - `PixelDataLength`
  - `PixelDataSha256`
- Verification returns deterministic mismatch reason text.

## Implementation Checklist

- [x] Core contracts
  - Add metadata read API.
  - Add integrity verification API.
- [x] Sample
  - Add menu action to load last saved metadata and display summary.
- [x] Tests
  - Add round-trip read/write coverage.
  - Add integrity verification success/failure coverage.
- [x] Docs
  - Update README/nativewebview/quickstart with read+verify usage.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug --no-build`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
  - `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`

## Exit Criteria

- Sidecar metadata can be round-tripped via public APIs.
- Integrity verification is deterministic and test-covered.
- Build/tests/smoke checks are green.
