# NativeWebView Dialog And Auth Follow-Up Plan

## Goal

Complete the two remaining follow-up areas after the embedded-control bring-up:

- implement real `NativeWebDialog` runtime support on Windows and Linux,
- replace `WebAuthenticationBroker` stubs with real platform implementations.

## Status Review Before Implementation

### Dialog

- Windows exposed the `Dialog` feature contract, but `WindowsNativeWebDialogBackend` was still a placeholder handle provider.
- Linux exposed the `Dialog` feature contract, but `LinuxNativeWebDialogBackend` was still a placeholder handle provider.
- macOS already had a real dialog runtime.
- iOS, Android, and Browser intentionally did not register dialog backends.

### Authentication broker

- Windows, macOS, and Linux broker backends were direct `WebAuthenticationBrokerStubBase` implementations.
- iOS, Android, and Browser returned fabricated success URLs instead of running a real interactive auth session.
- Shared docs and status APIs still described broker support as contract-only across platforms.

## Implementation Plan

### Phase 1: Desktop dialog runtimes

- Rebuild `WindowsNativeWebDialogBackend` on top of the real `WindowsNativeWebViewBackend` so the dialog shell only owns the top-level HWND and delegates browser behavior to the existing WebView2 runtime host.
- Rebuild `LinuxNativeWebDialogBackend` on top of the real `LinuxNativeWebViewBackend` so the dialog shell only owns the top-level GTK window and delegates browser behavior to the existing WebKitGTK runtime host.
- Keep `NativeWebDialog` instance configuration forwarding intact so dialog proxy/storage settings flow into the existing Windows/Linux runtime option pipelines.

### Phase 2: Shared broker flow support

- Add a shared callback-matching and dialog-runner helper in `NativeWebView.Core`.
- Preserve controller validation in `WebAuthenticationBrokerController`; only backend execution changes.
- Support deterministic “request already equals callback” completion for tests and non-interactive bootstrap cases.

### Phase 3: Desktop broker runtimes

- Implement Windows, macOS, and Linux broker backends by running the shared dialog-backed auth flow against their real dialog runtimes.
- Treat `SilentMode` as `UserCancel` and keep `UseHttpPost` explicitly unsupported for now.

### Phase 4: Mobile and browser broker runtimes

- iOS: present a modal `WKWebView` controller and complete on callback navigation.
- Android: launch a dedicated auth activity hosting `android.webkit.WebView` and complete through a shared session coordinator.
- Browser: open a popup window, poll the callback URL when it becomes observable, and close on completion/cancel.

### Phase 5: Status, docs, and tests

- Flip the implementation-status matrix from contract-only to runtime-implemented wherever the new runtime paths now exist.
- Update platform/control docs and README support tables.
- Replace tests that depended on fabricated auth URLs with deterministic callback-matching and cancellation coverage.

## Delivered

- Windows and Linux now have real `NativeWebDialog` runtime paths backed by their existing WebView2/WebKitGTK runtime engines.
- `WebAuthenticationBroker` now has real platform implementations on Windows, macOS, Linux, iOS, Android, and Browser.
- Browser auth is popup-based and therefore requires popup support plus an inspectable `http` or `https` callback URL.
- `UseHttpPost` remains unsupported on all current broker runtimes and is reported as `ErrorHttp`.

## Residual Limits

- `NativeWebDialog` remains intentionally unsupported on iOS, Android, and Browser.
- Browser auth cannot observe custom-scheme callbacks on the current runtime path.
- Android per-instance proxy configuration remains contract-only because the official platform API is app-wide.
- Linux dialog/auth runtime support is still X11-only.
