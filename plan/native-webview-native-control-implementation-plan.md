# Native WebView Native Control Implementation Plan

## 1. Scope and Success Criteria

### Goal
Build a production-grade, native-webview-first control stack for Avalonia (no bundled Chromium engine) with:
- `NativeWebView` embedded control
- `NativeWebDialog` window/dialog-hosted browser
- `WebAuthenticationBroker`
- Platform interop handles
- Environment/options hooks
- End-to-end NuGet packaging, CI, docs, and README

### Target Platforms
- Windows
- macOS
- Linux
- iOS
- Android
- Browser (WASM)

### Success Criteria
- Public API parity with the baseline component surface (properties/events/methods/options/interop contracts).
- Functional parity for navigation, JS bridge, messaging, resource interception, popup handling, and print surface where platform allows.
- Clear and tested support matrix for every API on every platform (supported, emulated, unsupported with explicit exception/error contract).
- Releasable multi-package NuGet distribution with symbols, SourceLink, docs, and CI gates.

---

## 2. Parity Inventory (Implementation Contract)

## 2.1 `NativeWebView`

### Properties
- `Source`
- `CurrentUrl`
- `IsInitialized`
- `CanGoBack`
- `CanGoForward`
- `IsDevToolsEnabled`
- `IsContextMenuEnabled`
- `IsStatusBarEnabled`
- `IsZoomControlEnabled`
- `ZoomFactor`
- `HeaderString`
- `UserAgentString`

### Events
- `CoreWebView2Initialized` (keep name for compatibility even if non-Windows backend)
- `NavigationStarted`
- `NavigationCompleted`
- `WebMessageReceived`
- `OpenDevToolsRequested`
- `DestroyRequested`
- `RequestCustomChrome`
- `RequestParentWindowPosition`
- `BeginMoveDrag`
- `BeginResizeDrag`
- `NewWindowRequested`
- `WebResourceRequested`
- `ContextMenuRequested`
- `NavigationHistoryChanged`
- `CoreWebView2EnvironmentRequested`
- `CoreWebView2ControllerOptionsRequested`

### Methods
- `Navigate(...)`
- `Reload()`
- `Stop()`
- `GoBack()`
- `GoForward()`
- `ExecuteScriptAsync(...)`
- `PostWebMessageAsJson(...)`
- `PostWebMessageAsString(...)`
- `OpenDevToolsWindow()`
- `PrintAsync(...)`
- `ShowPrintUIAsync(...)`
- `SetZoomFactor(...)`
- `SetUserAgent(...)`
- `SetHeader(...)`
- `TryGetCommandManager(...)`
- `TryGetCookieManager(...)`
- `MoveFocus(...)`

---

## 2.2 `NativeWebDialog`

### Properties
- `IsVisible`
- `CurrentUrl`
- `CanGoBack`
- `CanGoForward`
- `IsDevToolsEnabled`
- `IsContextMenuEnabled`
- `IsStatusBarEnabled`
- `IsZoomControlEnabled`
- `ZoomFactor`
- `HeaderString`
- `UserAgentString`

### Events
- `Shown`
- `Closed`
- `NavigationStarted`
- `NavigationCompleted`
- `WebMessageReceived`
- `NewWindowRequested`
- `WebResourceRequested`
- `ContextMenuRequested`

### Methods
- `Show(...)`
- `Close()`
- `Move(...)`
- `Resize(...)`
- `Navigate(...)`
- `Reload()`
- `Stop()`
- `GoBack()`
- `GoForward()`
- `ExecuteScriptAsync(...)`
- `PostWebMessageAsJson(...)`
- `PostWebMessageAsString(...)`
- `OpenDevToolsWindow()`
- `PrintAsync(...)`
- `ShowPrintUIAsync(...)`
- `SetZoomFactor(...)`
- `SetUserAgent(...)`
- `SetHeader(...)`

---

## 2.3 `WebAuthenticationBroker`

### API
- `AuthenticateAsync(...)`

### Options
- `None`
- `SilentMode`
- `UseTitle`
- `UseHttpPost`
- `UseCorporateNetwork`
- `UseWebAuthenticationBroker`

### Result/Status
- `ResponseData`
- `ResponseStatus` (`Success`, `UserCancel`, `ErrorHttp`)
- `ResponseErrorDetail`

---

## 2.4 Interop and Environment Contracts

### Interop Interfaces
- `IPlatformHandleProvider`
- `INativeWebViewPlatformHandleProvider`
- `INativeWebDialogPlatformHandleProvider`

### Environment Option Event Args
- Browser environment request args with:
  - `BrowserExecutableFolder`
  - `UserDataFolder`
  - `Language`
  - `AdditionalBrowserArguments`
  - `TargetCompatibleBrowserVersion`
  - `AllowSingleSignOnUsingOSPrimaryAccount`
- Controller/profile option args with:
  - `ProfileName`
  - `IsInPrivateModeEnabled`
  - `ScriptLocale`

---

## 3. Architecture Blueprint

## 3.1 Repository/Project Layout

```text
src/
  NativeWebView/
    NativeWebView.csproj                  // shared control API + abstractions
  NativeWebView.Core/
    NativeWebView.Core.csproj             // cross-platform backend contracts
  NativeWebView.Platform.Windows/
  NativeWebView.Platform.macOS/
  NativeWebView.Platform.Linux/
  NativeWebView.Platform.iOS/
  NativeWebView.Platform.Android/
  NativeWebView.Platform.Browser/
  NativeWebView.Dialog/
  NativeWebView.Auth/
  NativeWebView.Interop/
samples/
  Sample.Desktop/
  Sample.Mobile/
  Sample.Browser/
tests/
  NativeWebView.UnitTests/
  NativeWebView.IntegrationTests/
  NativeWebView.PlatformTests.*
docs/
  ...
```

## 3.2 Core Abstractions
- `INativeWebViewBackend`: lifecycle, navigation, script execution, message bridge, devtools, print, cookies, command API.
- `INativeWebDialogBackend`: show/close/move/resize + same browser operations.
- `IWebAuthenticationBrokerBackend`: auth flow orchestration and callback capture.
- `IWebViewPlatformFeatures`: runtime capability reporting used by API guards.
- `NativeWebViewBackendFactory`: resolves backend by platform at runtime.

## 3.3 API Compatibility Strategy
- Keep external API names intact.
- Normalize behavior with shared event arg types.
- For unsupported operations on a platform:
  - Expose capability flag.
  - Throw `PlatformNotSupportedException` only when invoked.
  - Document fallback/emulation behavior.

## 3.4 Threading/Lifecycle Rules
- All public control mutations on Avalonia UI thread.
- Backend calls marshal to native thread model (STA/main-thread/WebKit thread/Looper).
- Deterministic disposal:
  - Unhook native callbacks.
  - Tear down JS bridge safely.
  - Release platform handles.

---

## 4. Platform Implementation Plan

## 4.1 Windows
- Backend: Edge WebView2 runtime.
- Embedded control host with composition path for Avalonia.
- Implement environment/controller option mapping directly.
- Implement printing, devtools, resource interception, popup handling.
- Implement platform handles for host window and underlying WebView2 controller/view.

## 4.2 macOS
- Backend: `WKWebView` via native host view.
- Map navigation/message/resource hooks to shared event model.
- Implement dialog with native window sheet/popup host.
- Implement auth broker using `ASWebAuthenticationSession` (preferred) with fallback.
- Expose Cocoa handle interop.

## 4.3 Linux
- Backend: `WebKitGTK` (4.1+), GTK native embedding.
- Implement embedded host (not dialog-only).
- Integrate with Avalonia X11/Wayland host handles.
- Implement request interception, JS messaging, and popup policy.
- Printing support via GTK print operation where available.
- Handle distro/runtime dependencies with startup diagnostics.

## 4.4 iOS
- Backend: `WKWebView`.
- Control embedded in Avalonia iOS native view host.
- Auth broker with `ASWebAuthenticationSession`.
- Support navigation/message/script/cookies/headers/UA/zoom.
- Disable or emulate desktop-only APIs (`ShowPrintUIAsync`, devtools) with explicit capability map.

## 4.5 Android
- Backend: `android.webkit.WebView`.
- Configure `WebViewClient` + `WebChromeClient` for:
  - Navigation callbacks
  - JS dialogs
  - Popup/new window requests
  - Permission bridge for media APIs
- JS bridge using `addJavascriptInterface` + postMessage normalization.
- Auth broker via Custom Tabs / WebView flow with redirect interception.

## 4.6 Browser (WASM)
- Backend: DOM `<iframe>` / popup strategy with JS interop.
- Implement navigation + history + messaging using `postMessage`.
- Script execution through JS interop wrapper.
- Resource interception limitations documented; provide emulation hooks where feasible.
- Auth broker via `window.open` + redirect/callback message channel.

---

## 5. Feature Matrix Target (All Platforms)

## 5.1 Mandatory Features
- Embedded `NativeWebView`: all 6 platforms.
- `WebAuthenticationBroker`: all 6 platforms.
- `NativeWebDialog`: full desktop support (Windows/macOS/Linux) and defined mobile/browser alternatives.
- JS -> .NET bridge and .NET -> JS execution.
- Navigation lifecycle events + history tracking.
- New-window interception and policy control.
- Web resource request interception (or explicit not-supported contract with capability flag).
- Cookie and command manager abstraction.

## 5.2 Platform-Specific Allowed Differences
- DevTools: unavailable on some mobile/browser targets.
- Print UI: desktop-first; mobile/browser may be limited.
- Low-level resource interception depth differs per engine.
- Drag-resize/chrome events only where desktop windowing exists.

---

## 6. Delivery Phases and Milestones

## Phase 0 - Foundation (Week 1)
- Create solution layout and project scaffolding.
- Define public API contracts and event args.
- Add capability model and backend factory.
- Exit criteria: builds on all target TFMs with stub backends.

## Phase 1 - Shared Core (Week 2)
- Implement shared control/dialog/auth orchestration.
- Implement message dispatch, navigation state, and disposal state machine.
- Add API-level unit tests.
- Exit criteria: API contract stable, test harness passes.

## Phase 2 - Desktop Backends (Weeks 3-4)
- Windows backend complete.
- macOS backend complete.
- Linux embedded backend complete (not dialog-only).
- Desktop dialog implementation complete.
- Exit criteria: sample desktop app validates full navigation/message/print/devtools matrix.

## Phase 3 - Mobile and Browser (Weeks 5-6)
- iOS backend + auth broker flow.
- Android backend + auth broker flow + permission callbacks.
- Browser backend + popup-based auth flow + postMessage bridge.
- Exit criteria: platform smoke tests passing on all three targets.

## Phase 4 - Advanced Parity and Interop (Week 7)
- Environment/options request events wired on all platforms (native or emulated).
- Platform handle providers exposed and tested.
- Cookie/command manager abstraction finalized.
- Exit criteria: parity checklist signed off.

## Phase 5 - Hardening and QA (Week 8)
- Reliability tests (dispose/recreate loops, navigation stress).
- Security review (origin checks, bridge hardening, auth callback validation).
- Performance baseline (startup, navigation latency, memory).
- Exit criteria: no P0/P1 defects; perf within defined thresholds.

## Phase 6 - Packaging, CI, Docs, Release (Week 9)
- NuGet packaging and symbol/source publishing.
- CI matrix complete with release workflow.
- README + docs site + samples finalized.
- Exit criteria: release candidate packages published from tagged build.

## Phase 7 - Release Notes and Docs Publishing Automation (Week 10)
- Changelog-fragment workflow and validation gates.
- Release notes generated from changelog fragments in CI/release.
- Docs site build + publish workflow for GitHub Pages.
- Exit criteria: tagged release publishes packages plus generated release notes and docs pipeline is green.

## Phase 8 - Extended Validation Automation (Week 11)
- Add browser-facing smoke validation with Playwright.
- Add dedicated extended-validation workflow for mobile/browser contract checks.
- Standardize sample smoke command wrappers for CI/release reuse.
- Exit criteria: scheduled/manual extended-validation workflow passes on supported runners.

## Phase 9 - Device Runner Validation (Week 12)
- Add Android-emulator-backed contract smoke execution in CI.
- Add iOS-simulator-backed contract smoke execution in CI.
- Consolidate browser Playwright validation into reusable scripts.
- Exit criteria: extended-validation workflow boots device runners and passes browser + mobile contract checks.

## Phase 10 - Platform Prerequisite Diagnostics (Week 13)
- Add cross-platform diagnostics model for startup prerequisite validation.
- Register per-platform diagnostics providers in every platform module.
- Expose runtime/factory APIs for diagnostics retrieval.
- Add automated diagnostics coverage and docs.
- Exit criteria: each platform registration exposes diagnostics and startup can fail fast on blocking prerequisite issues.

## Phase 11 - Diagnostics Policy Enforcement and CI Gates (Week 14)
- Add strict diagnostics validator helpers for fail-fast startup policy.
- Wire diagnostics policy gating into sample smoke execution paths used by CI/release.
- Add validator-focused unit tests and docs for strict/warnings-as-errors modes.
- Exit criteria: CI smoke scripts fail on blocking diagnostics and strict diagnostics policy is documented and tested.

## Phase 12 - Diagnostics Reporting Artifacts and Publication (Week 15)
- Add reusable diagnostics report generation APIs and a CLI sample for JSON export.
- Add CI/release diagnostics-report execution with artifact upload and release attachment.
- Document diagnostics report commands, schema, and policy options.
- Exit criteria: CI/release publish diagnostics JSON artifacts and can gate on blocking prerequisite diagnostics.

## Phase 13 - Diagnostics Markdown Summaries and Release Surfacing (Week 16)
- Add diagnostics markdown formatter for human-readable platform status summaries.
- Emit markdown summaries from diagnostics sample/script alongside JSON reports.
- Publish diagnostics markdown in CI workflow summary and append to generated release notes.
- Exit criteria: CI and release runs expose human-readable diagnostics summary without downloading JSON artifacts.

## Phase 14 - Blocking Baseline Regression Gates (Week 17)
- Add blocking diagnostics baseline parsing/serialization/comparison APIs.
- Extend diagnostics sample/script to compare current blocking issues against baseline allowlist.
- Publish regression comparison markdown artifacts and enforce baseline gate in CI/release workflows.
- Exit criteria: CI/release fail on newly introduced blocking diagnostics issues compared to repository baseline.

## Phase 15 - Baseline Sync Hygiene and Refresh Automation (Week 18)
- Add regression model flags for stale baseline detection and baseline update requirement.
- Extend diagnostics sample/script with baseline-sync gate option (`--require-baseline-sync`).
- Add baseline refresh helper script for regenerating repository blocking baseline artifacts.
- Enforce stale-baseline hygiene gate in CI/release and document baseline refresh workflow.
- Exit criteria: CI/release fail when baseline contains resolved/stale entries and repository provides a deterministic baseline refresh command.

## Phase 16 - Regression Evaluation JSON and Gate Auditability (Week 19)
- Add regression evaluation API that captures gate policy inputs and computed gate outcomes.
- Extend diagnostics sample/script with comparison evaluation JSON output.
- Publish regression evaluation JSON artifacts in CI/release alongside markdown comparison outputs.
- Document regression evaluation JSON schema/usage and update local command recipes.
- Exit criteria: CI/release artifacts include machine-readable gate outcomes (`effectiveExitCode` + per-gate flags) for baseline comparisons.

## Phase 17 - Gate-Specific Exit Code Contract (Week 20)
- Add gate failure classification model (`require-ready`, `regression`, `baseline-sync`).
- Emit deterministic gate-specific exit codes from diagnostics evaluation and CLI.
- Preserve combined failure reporting when multiple gates fail in one run.
- Document exit code contract for CI/local tooling integrations.
- Exit criteria: diagnostics CLI returns stable gate-specific exit codes (`10`, `11`, `12`, `13`) and tests cover single/multi-gate failures.

## Phase 18 - Exit Code Contract Conformance Automation (Week 21)
- Add dedicated script that exercises diagnostics gate scenarios and validates expected exit codes.
- Verify generated regression evaluation JSON outputs for each scenario.
- Publish conformance summary/log artifacts in CI/release.
- Document local execution for exit code contract validation.
- Exit criteria: CI/release run automated conformance script and fail if any gate contract exit code deviates from expected mapping.

## Phase 19 - Gate Evaluation Markdown Surfacing and Triage Signals (Week 22)
- Add gate evaluation markdown formatter that mirrors regression evaluation JSON in human-readable form.
- Extend diagnostics sample and wrapper script with gate evaluation markdown output option.
- Validate gate evaluation markdown generation in exit-code contract conformance automation.
- Publish gate evaluation markdown artifacts in CI workflow summary and release notes.
- Exit criteria: every CI/release diagnostics run emits both machine-readable evaluation JSON and human-readable gate evaluation markdown with matching effective exit code.

## Phase 20 - Structured Gate Failure Metadata and Remediation Contract (Week 23)
- Add structured gate failure metadata model (`kind`, `exitCode`, `message`, `recommendation`) to regression evaluation output.
- Reuse structured gate failure metadata in diagnostics CLI stderr and gate evaluation markdown formatting.
- Extend script-level validations and conformance automation to enforce `gateFailures` metadata contract.
- Document structured gate failure metadata in diagnostics report docs for CI/local tooling consumers.
- Exit criteria: evaluation JSON always includes deterministic `gateFailures[]` entries for failing gates and conformance automation validates gate-specific recommendations.

## Phase 21 - Deterministic Evaluation Fingerprint Contract (Week 24)
- Add deterministic regression evaluation fingerprint field that excludes run timestamp noise.
- Surface fingerprint in both evaluation JSON and gate evaluation markdown outputs.
- Enforce fingerprint format/presence in diagnostics wrapper and exit-code conformance scripts.
- Add unit tests validating fingerprint stability and gate-outcome sensitivity.
- Exit criteria: diagnostics evaluation artifacts include stable `fingerprint` values (64-char lowercase SHA-256 hex) and conformance automation validates fingerprint contract.

## Phase 22 - Fingerprint Parity and Schema Version Enforcement (Week 25)
- Add explicit fingerprint schema version field (`fingerprintVersion`) to evaluation outputs.
- Enforce JSON and markdown fingerprint parity in diagnostics wrapper and conformance scripts.
- Surface per-scenario fingerprints in conformance markdown summary for quick triage.
- Extend tests and docs to lock fingerprint version and parity expectations.
- Exit criteria: diagnostics artifacts include `fingerprintVersion=1`, and automation fails when JSON/markdown fingerprints diverge.

## Phase 23 - Fingerprint Baseline Contract Gate and Refresh Workflow (Week 26)
- Add repository-managed diagnostics fingerprint baseline for conformance scenarios.
- Extend conformance automation with optional fingerprint baseline verification gate.
- Emit current fingerprint contract artifact (`fingerprint-current.txt`) for baseline refresh diffs.
- Add helper script to regenerate fingerprint baseline deterministically.
- Exit criteria: CI/release fail when conformance fingerprints diverge from repository baseline unless baseline is intentionally refreshed.

## Phase 24 - Fingerprint Baseline Drift Comparison Artifacts and Triage Surfacing (Week 27)
- Add structured fingerprint baseline comparison artifact with expected/actual values for every conformance scenario.
- Add human-readable fingerprint baseline comparison markdown for triage and workflow summaries.
- Update conformance validation to report all mismatched scenarios in one pass and fail only after writing comparison artifacts.
- Publish fingerprint baseline comparison markdown in CI summary and append release-time conformance summary sections.
- Exit criteria: every fingerprint baseline drift failure provides complete comparison artifacts (`fingerprint-baseline-comparison.json` + `.md`) for all scenarios.

## Phase 25 - Machine-Readable Conformance Summary Contract (Week 28)
- Add machine-readable exit-code conformance summary JSON artifact with per-scenario expected/actual outcomes.
- Include per-scenario evaluation fingerprint values in conformance summary JSON for automation consumers.
- Include fingerprint baseline gate metadata (`enabled`, match/mismatch counts, comparison artifact paths) in conformance summary JSON.
- Enforce conformance summary JSON contract fields in automation validation.
- Exit criteria: every conformance run emits deterministic `exit-code-contract-summary.json` with complete case outcomes and fingerprint-baseline gate context.

---

## 7. Test Strategy

## 7.1 Automated
- Unit tests for state transitions, API guards, capability flags.
- Integration tests for navigation events, message bridge, script execution.
- Platform tests:
  - Windows: WebView2 integration tests.
  - macOS/iOS: WKWebView-driven tests.
  - Linux: WebKitGTK integration tests in CI container/runner.
  - Android: instrumentation tests.
  - Browser: Playwright-based WASM tests.

## 7.2 Manual Validation Checklist
- OAuth login flow with redirect capture.
- Popup/new window policies.
- File upload/download and clipboard interactions.
- Audio/video permission prompts.
- Destroy/recreate control in same window.
- Multiple controls in one visual tree.

## 7.3 Regression Gates
- No leaked native handles after dispose cycles.
- No cross-thread UI access violations.
- Stable behavior after app suspend/resume (mobile/browser lifecycle).

---

## 8. CI/CD Plan

## 8.1 PR Workflow
- `dotnet restore`, `build`, `format`/analyzers, unit tests.
- OS matrix:
  - `windows-latest`
  - `macos-latest`
  - `ubuntu-latest` (with WebKitGTK dev/runtime deps)
- Artifact upload: test logs + screenshots + crash dumps + diagnostics report JSON/Markdown + blocking regression summary + gate evaluation markdown summary.
- Artifact upload includes machine-readable regression evaluation JSON for gate auditing.
- Conformance automation emits machine-readable `exit-code-contract-summary.json` for case-by-case exit-code contract auditing.
- Gate-specific exit codes can be consumed by automation for failure classification.
- Automated exit-code conformance script validates gate contract on every CI/release run.
- Baseline sync gate: fail when diagnostics baseline contains resolved/stale entries.
- Conformance artifact bundle includes fingerprint baseline drift comparison JSON/Markdown when fingerprint baseline gate is enabled.

## 8.2 Extended Validation
- Android emulator job (instrumentation smoke).
- iOS simulator job (macOS runner).
- Browser WASM + Playwright job.

## 8.3 Release Workflow
- Trigger: semver tag.
- Produce signed NuGet packages (`.nupkg`, `.snupkg`).
- Publish release notes generated from changelog fragments with appended diagnostics/regression summaries.
- Enforce baseline sync gate alongside regression detection in release pipeline.
- Publish diagnostics report JSON/Markdown artifacts plus blocking baseline comparison outputs.
- Publish regression evaluation JSON artifact with policy/gate outcomes for release diagnostics runs.
- Publish gate evaluation markdown artifact for human-readable release triage and notes append.
- Regression evaluation JSON contract includes structured `gateFailures[]` remediation metadata for automation and triage tooling.
- Regression evaluation JSON contract includes deterministic `fingerprint` field for cross-run artifact correlation.
- Diagnostics automation validates JSON/markdown fingerprint parity and expected fingerprint schema version.
- Diagnostics automation can enforce repository fingerprint baseline drift gate and publish current fingerprint contract artifacts.
- Release diagnostics notes can include conformance and fingerprint baseline drift comparison markdown for faster post-failure triage.
- Release diagnostics artifacts include machine-readable conformance summary JSON for downstream release automation.
- Optional package signing/notarization per platform binaries.

---

## 9. NuGet Packaging Plan

## 9.1 Package Split
- `NativeWebView` (meta package)
- `NativeWebView.Core`
- `NativeWebView.Platform.Windows`
- `NativeWebView.Platform.macOS`
- `NativeWebView.Platform.Linux`
- `NativeWebView.Platform.iOS`
- `NativeWebView.Platform.Android`
- `NativeWebView.Platform.Browser`
- `NativeWebView.Auth`
- `NativeWebView.Dialog`
- `NativeWebView.Interop`

## 9.2 Packaging Standards
- `RepositoryUrl`, `RepositoryCommit`, `PackageReadmeFile`, license metadata.
- SourceLink + deterministic builds + symbols.
- Trimming/AOT annotations and analyzer warnings documented.
- Semantic versioning with compatibility policy:
  - Major: API breaks
  - Minor: additive features
  - Patch: fixes only

---

## 10. Documentation and README Plan

## 10.1 README Sections
- What this library provides
- Platform support and prerequisites
- Install matrix by platform
- 5-minute quick start sample
- JS bridge sample
- Auth broker sample
- Interop handle sample
- Known limitations by platform

## 10.2 Docs Set
- `docs/quickstart.md`
- `docs/nativewebview.md`
- `docs/nativewebdialog.md`
- `docs/webauthenticationbroker.md`
- `docs/interop/environment-options.md`
- `docs/interop/native-browser-interop.md`
- `docs/platforms/windows.md`
- `docs/platforms/macos.md`
- `docs/platforms/linux.md`
- `docs/platforms/ios.md`
- `docs/platforms/android.md`
- `docs/platforms/browser.md`
- `docs/platform-prerequisites.md`
- `docs/platform-diagnostics-report.md`
- `docs/ci-and-release.md`

## 10.3 Samples
- Desktop sample with tabs/dialog/auth.
- Mobile sample with auth and JS messaging.
- Browser sample with iframe bridge and popup auth.

---

## 11. Risk Register and Mitigation

- Linux embedding complexity (GTK/WebKit host interop).
  - Mitigation: prototype Linux backend first in Phase 2 and validate on X11 + Wayland early.
- Browser limitations for request interception and print.
  - Mitigation: expose explicit capability flags and emulated behavior contracts.
- Mobile auth/session differences.
  - Mitigation: shared auth abstraction with per-platform adapter and contract tests.
- Native dependency drift (WebView runtime/library versions).
  - Mitigation: startup diagnostics + version checks + documented prerequisites.
- Resource leaks from native callbacks/web process.
  - Mitigation: stress dispose tests and leak checks in CI.

---

## 12. Immediate Execution Backlog (First 2 Weeks)

1. Create solution and package skeleton with all target projects.
2. Define complete shared API surface and event args.
3. Implement backend interfaces and capability registry.
4. Wire control + dialog + auth orchestration with stub backends.
5. Build sample host app with dependency injection for backend selection.
6. Add initial Windows + macOS backend slices (navigation + script + messaging).
7. Add CI PR pipeline for desktop build/test artifacts.
8. Publish initial README with install and architecture overview.

---

## 13. Airspace Mitigation Render Modes (Phase 26)

### Goal
- Provide native-airspace mitigation options without changing the existing browser API:
  - `Embedded` (current behavior),
  - `GpuSurface`,
  - `Offscreen`.

### Contract Additions
- New `NativeWebViewRenderMode` enum.
- New feature flags:
  - `GpuSurfaceRendering`
  - `OffscreenRendering`
- New frame-source contract:
  - `INativeWebViewFrameSource`
  - `NativeWebViewRenderFrameRequest`
  - `NativeWebViewRenderFrame`

### Control Behavior
- `NativeWebView` properties:
  - `RenderMode`
  - `RenderFramesPerSecond`
  - `IsUsingSyntheticFrameSource`
  - `RenderDiagnosticsMessage`
- `SupportsRenderMode(...)` runtime probe.
- Frame-pump lifecycle tied to visual attach/detach and mode selection.

### Platform Rollout
- macOS: native frame capture path from hosted `WKWebView`.
- Windows/Linux/iOS/Android/Browser: deterministic frame-source fallback path, preserving mode API and diagnostics contracts while deeper native capture backends are staged.

### Validation
- Build and unit-test regression gates remain required.
- Desktop sample must allow live mode switching and show render diagnostics/state.

---

## 14. Render Frame Capture and Export (Phase 27)

### Goal
- Provide public APIs to capture and persist composited web frames for diagnostics, tooling, and UI automation use cases.

### Contract Additions
- New feature flag:
  - `RenderFrameCapture`
- New `NativeWebView` methods:
  - `CaptureRenderFrameAsync(...)`
  - `SaveRenderFrameAsync(...)`

### Behavior
- Capture/export is available in composited render modes (`GpuSurface`, `Offscreen`).
- `Embedded` mode returns no frame.
- Output persistence writes png artifacts and auto-creates output directories.

### Platform Rollout
- macOS: capture uses the native host frame path.
- Windows/Linux/iOS/Android/Browser: capture uses deterministic fallback frame-source contracts until native capture backends are staged.

### Validation
- Build + tests + desktop and mobile/browser smoke checks pass.

---

## 15. Render Frame Metadata and Capture Events (Phase 28)

### Goal
- Attach deterministic metadata to captured frames and expose event-based capture notifications for diagnostics and automation consumers.

### Contract Additions
- New `NativeWebViewRenderFrameOrigin` enum.
- Extended `NativeWebViewRenderFrame` metadata:
  - `FrameId`
  - `CapturedAtUtc`
  - `RenderMode`
  - `Origin`
- New event args:
  - `NativeWebViewRenderFrameCapturedEventArgs`
- New `NativeWebView` event:
  - `RenderFrameCaptured`

### Behavior
- Synthetic fallback captures emit `Origin = SyntheticFallback` with monotonic frame IDs.
- Native captures emit `Origin = NativeCapture` with monotonic frame IDs.
- `RenderFrameCaptured` is raised for successful composited captures.

### Validation
- Metadata semantics covered by tests.
- Build + tests + smoke checks pass.

---

## 16. Render Capture Statistics and Snapshot API (Phase 29)

### Goal
- Provide deterministic, queryable render-capture statistics so applications can inspect capture health and source breakdown at runtime without parsing event logs.

### Contract Additions
- New render diagnostics model:
  - `NativeWebViewRenderStatistics`
- New `NativeWebView` API:
  - `RenderStatistics`
  - `GetRenderStatisticsSnapshot()`
  - `ResetRenderStatistics()`

### Behavior
- Capture telemetry tracks:
  - attempts, successes, failures, and skipped captures.
  - synthetic-vs-native frame counts.
  - last captured frame metadata (`FrameId`, timestamp, mode, origin).
  - last capture failure message/timestamp.
- Successful capture clears stale failure state.
- `ResetRenderStatistics()` clears all counters and last-frame/last-failure state.

### Validation
- Tracker semantics covered by unit tests.
- Build + tests + smoke checks pass.

---

## 17. Render Frame Sidecar Metadata Export (Phase 30)

### Goal
- Export render captures with a machine-readable JSON sidecar so diagnostics tooling can consume frame metadata, runtime state, and capture statistics without parsing logs.

### Contract Additions
- New metadata model:
  - `NativeWebViewRenderFrameExportMetadata`
- New serializer helper:
  - `NativeWebViewRenderFrameMetadataSerializer`
- New `NativeWebView` method:
  - `SaveRenderFrameWithMetadataAsync(...)`

### Behavior
- Capture in composited modes and export png artifact plus metadata JSON.
- Metadata includes:
  - frame metadata (`FrameId`, `CapturedAtUtc`, dimensions, pixel format, origin/synthetic).
  - runtime render state (platform, mode, fps, current URL, diagnostics message).
  - render capture statistics snapshot.
- Export method supports explicit metadata path and deterministic default (`<png-path>.json`).

### Validation
- Unit tests verify metadata serializer output and control export semantics.
- Build + tests + smoke checks pass.

---

## 18. Render Export Integrity Metadata (Phase 31)

### Goal
- Add deterministic integrity fields to render sidecar metadata so exported frames can be verified and deduplicated in automation pipelines.

### Contract Additions
- Extend `NativeWebViewRenderFrameExportMetadata` with:
  - `PixelDataLength`
  - `PixelDataSha256`

### Behavior
- Sidecar metadata computes SHA-256 from captured pixel data used for export.
- For BGRA frames, integrity hashing uses visible pixel bytes per row and excludes row-stride padding.
- Schema version is incremented to reflect integrity metadata contract evolution.
- Integrity fields are emitted for every sidecar export path.

### Validation
- Serializer tests assert integrity field values and JSON output.
- Build + tests + smoke checks pass.

---

## 19. Render Metadata Round-Trip and Integrity Verification API (Phase 32)

### Goal
- Provide first-class APIs to read sidecar metadata and verify exported frame integrity in-process for diagnostics tooling and automation workflows.

### Contract Additions
- Extend `NativeWebViewRenderFrameMetadataSerializer` with:
  - `ReadFromFileAsync(...)`
  - `TryVerifyIntegrity(...)`

### Behavior
- Sidecar JSON can be deserialized back into typed metadata payloads.
- Integrity verification compares metadata length/hash fields against a provided frame buffer.
- Verification returns deterministic mismatch messages for triage.

### Validation
- Unit tests cover round-trip read/write and integrity match/mismatch semantics.
- Build + tests + smoke checks pass.
