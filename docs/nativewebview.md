# NativeWebView

`NativeWebView` is the embedded browser facade surface.

## Core capabilities

- Navigation lifecycle and history state.
- Script execution and message posting.
- DevTools/context menu/status bar/zoom toggles where supported.
- Print and print UI support where supported.
- Cookie and command manager access when available.
- Native handle interop when backend provides it.
- Airspace-mitigation render modes (`Embedded`, `GpuSurface`, `Offscreen`).

## Main properties

- `Source`, `CurrentUrl`, `IsInitialized`
- `CanGoBack`, `CanGoForward`
- `IsDevToolsEnabled`, `IsContextMenuEnabled`, `IsStatusBarEnabled`, `IsZoomControlEnabled`
- `ZoomFactor`, `HeaderString`, `UserAgentString`
- `RenderMode`, `RenderFramesPerSecond`
- `IsUsingSyntheticFrameSource`, `RenderDiagnosticsMessage`, `RenderStatistics`

## Main methods

- `InitializeAsync`
- `Navigate`, `Reload`, `Stop`, `GoBack`, `GoForward`
- `ExecuteScriptAsync`
- `PostWebMessageAsJsonAsync`, `PostWebMessageAsStringAsync`
- `OpenDevToolsWindow`
- `PrintAsync`, `ShowPrintUiAsync`
- `SetZoomFactor`, `SetUserAgent`, `SetHeader`
- `TryGetCommandManager`, `TryGetCookieManager`
- `MoveFocus`
- `SupportsRenderMode`
- `CaptureRenderFrameAsync`, `SaveRenderFrameAsync`, `SaveRenderFrameWithMetadataAsync`
- `GetRenderStatisticsSnapshot`, `ResetRenderStatistics`

## Main events

- Initialization and options: `CoreWebView2Initialized`, `CoreWebView2EnvironmentRequested`, `CoreWebView2ControllerOptionsRequested`
- Navigation and state: `NavigationStarted`, `NavigationCompleted`, `NavigationHistoryChanged`
- Messaging and extensibility: `WebMessageReceived`, `NewWindowRequested`, `WebResourceRequested`, `ContextMenuRequested`
- Windowing hooks: `RequestCustomChrome`, `RequestParentWindowPosition`, `BeginMoveDrag`, `BeginResizeDrag`, `DestroyRequested`
- Render pipeline: `RenderFrameCaptured`

## Unsupported operations

When a backend does not support an operation, the capability flags expose it up front and method calls throw `PlatformNotSupportedException` on invocation.

## Render modes

- `Embedded`: native child host mode (best interaction fidelity, subject to native-airspace overlap behavior).
- `GpuSurface`: rendered frame mode with reusable GPU-uploaded surface in Avalonia.
- `Offscreen`: rendered frame mode using offscreen frame capture composited by Avalonia.
- For non-native capture paths, fallback frame generation can be used; inspect `IsUsingSyntheticFrameSource` and `RenderDiagnosticsMessage` at runtime.
- Use `CaptureRenderFrameAsync` to fetch the latest composited frame and `SaveRenderFrameAsync` to export png diagnostics artifacts.
- Use `SaveRenderFrameWithMetadataAsync` to export png and a JSON metadata sidecar for automation/diagnostics ingestion.
- Captured frames expose metadata (`FrameId`, `CapturedAtUtc`, `RenderMode`, `Origin`) for sequencing and telemetry correlation.
- `RenderStatistics` and `GetRenderStatisticsSnapshot` expose capture counters and last-frame metadata (attempt/success/failure/skip counts, source breakdown, latest frame metadata, last failure details).
- Sidecar metadata includes deterministic integrity fields (`PixelDataLength`, `PixelDataSha256`) in schema version `2`; for BGRA frames hashing uses visible pixel bytes and ignores row-stride padding.
- Use `NativeWebViewRenderFrameMetadataSerializer.ReadFromFileAsync` to load sidecar JSON and `TryVerifyIntegrity` to validate a frame against stored integrity fields (`FormatVersion` must match the current serializer schema).
- Use `ResetRenderStatistics` to clear capture telemetry when starting a fresh diagnostics run.
