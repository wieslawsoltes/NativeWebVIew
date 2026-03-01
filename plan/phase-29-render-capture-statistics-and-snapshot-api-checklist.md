# Phase 29 Render Capture Statistics and Snapshot API Checklist

## Scope

- Add structured render-capture statistics so apps can inspect capture reliability and source behavior at runtime.
- Expose snapshot and reset APIs on `NativeWebView`.
- Surface statistics in the desktop sample diagnostics/state output.

## Contract Additions

- Add `NativeWebViewRenderStatistics` model.
- Add `NativeWebViewRenderStatisticsTracker` accumulator.
- Add `NativeWebView` members:
  - `RenderStatistics`
  - `GetRenderStatisticsSnapshot()`
  - `ResetRenderStatistics()`

## Behavior Contract

- Statistics track:
  - capture attempts/successes/failures/skips.
  - synthetic/native frame counts.
  - last captured frame metadata (`FrameId`, timestamp, mode, origin).
  - last failure message and timestamp.
- Successful capture clears stale failure state.
- Reset clears counters and last-frame/last-failure fields.

## Implementation Checklist

- [x] Core contracts
  - Add render statistics snapshot model.
  - Add thread-safe statistics tracker.
- [x] Control
  - Track capture lifecycle outcomes in `NativeWebView`.
  - Expose snapshot and reset APIs.
- [x] Sample
  - Surface render statistics in the state summary panel.
  - Add menu action to reset statistics.
- [x] Tests
  - Add tracker unit tests covering success/failure/skip and reset semantics.
- [x] Docs
  - Update README and API docs with statistics APIs.
- [x] Validation
  - `dotnet build NativeWebView.sln -c Debug`
  - `dotnet test NativeWebView.sln -c Debug --no-build`
  - `./scripts/run-desktop-sample-smoke.sh --configuration Debug --no-build`
  - `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --no-build --platform all`

## Exit Criteria

- Render statistics API is public and documented.
- Statistics behavior is test-covered.
- Build/tests/smoke checks are green.
