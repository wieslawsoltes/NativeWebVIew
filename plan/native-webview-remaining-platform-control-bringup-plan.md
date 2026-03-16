# NativeWebView Remaining Platform Control Bring-Up Plan

## Goal

Review the actual `NativeWebView` control implementation status for every non-macOS target, define the one-by-one implementation order, and make the current repo status explicit so package/docs no longer overstate runtime support.

## Current Status Review

### Repo facts

- `src/NativeWebView/NativeWebView.cs` now has four real embedded control paths:
  - macOS through `MacOSNativeWebViewHost`
  - Windows through backend-owned native parent attachment and WebView2 child hosting
  - Linux through backend-owned native parent attachment and GTK3/WebKitGTK child hosting on X11
  - iOS through backend-owned `UIView` attachment and `WKWebView` hosting when the iOS backend is built with the .NET 8 Apple workload
- Android through backend-owned child `View` attachment and `android.webkit.WebView` hosting when the Android backend is built with the .NET 8 Android workload
- Browser now ships a real browser-targeted `NativeWebView` runtime backed by Avalonia Browser native control hosting plus a DOM `iframe` bridge.
- Windows, macOS, and Linux now also ship real `NativeWebDialog` runtime paths; mobile/browser dialog backends remain intentionally unregistered.
- `WebAuthenticationBroker` now has runtime implementations across all supported platforms, with platform-specific limits documented separately.
- The shared control project still targets `net8.0`, but backend-owned native attachment is now sufficient for Windows, Linux, iOS, and Android without adding more control-assembly target forks.

### Actual current repo runtime status

| Platform | `NativeWebView` control | `NativeWebDialog` | `WebAuthenticationBroker` | Notes |
| --- | --- | --- | --- | --- |
| Windows | Implemented | Implemented | Implemented | Embedded control is backed by a real child HWND + WebView2 runtime path; dialog/auth use real native windows and WebView2 sessions. |
| Linux | Implemented | Implemented | Implemented | Embedded control is backed by a real GTK3/WebKitGTK child host on X11; dialog/auth use real GTK/WebKitGTK windows and sessions on X11. |
| iOS | Implemented | Unsupported | Implemented | Embedded control is backed by a real `UIView` + `WKWebView` runtime path when the iOS backend is built with the .NET 8 Apple workload; auth uses a modal `WKWebView`. |
| Android | Implemented | Unsupported | Implemented | Embedded control is backed by a real child `View` + `android.webkit.WebView` runtime path when the Android backend is built with the .NET 8 Android workload; auth uses a dedicated activity-hosted `WebView`. |
| Browser | Implemented | Unsupported | Implemented | Embedded control is backed by a real Avalonia Browser native host + DOM `iframe` runtime path; auth uses popup/browser APIs and inherits browser popup/origin limits. |
| macOS | Implemented | Implemented | Implemented | Current real embedded/dialog runtime path plus dialog-backed auth. |

## Recommended Bring-Up Order

No remaining embedded-control bring-up phases are pending in this plan.

## Why This Order

- All listed embedded-control bring-up phases are now complete. Browser remains the most web-platform-constrained runtime because it must host through an `iframe` and cannot bypass normal browser same-origin or frame-embedding rules.

## Shared Prerequisites Before Per-Platform Bring-Up

- Preserve the new backend-owned native attachment primitive (`INativeWebViewNativeControlAttachment`) so Linux and later platforms can plug real control hosts into `NativeWebView` without adding more platform-specific control branches.
- Keep two status concepts explicit everywhere:
  - platform capability contract
  - current repo runtime implementation status
- Add integration tests that distinguish stub-contract validation from real runtime-host validation.
- Preserve the current instance-configuration and proxy helper surface so new hosts consume shared configuration instead of inventing per-platform option parsing.

## Platform Plan

### Completed: Windows

#### Delivered

- Real embedded Windows control hosting through a backend-owned child HWND and WebView2 controller.
- Shared environment/controller option application, including per-instance proxy argument merging.
- Real HWND / `ICoreWebView2` / `ICoreWebView2Controller` handles once attached.
- Runtime delegation for navigation, script execution, web messaging, zoom, print, print UI, and DevTools on the embedded control path.
- Honest implementation/proxy status reporting and Windows documentation updates.

### Completed: Linux

#### Delivered

- Real embedded Linux control hosting through a backend-owned GTK3/X11 child window and WebKitGTK web view.
- Shared environment/controller option application, including per-instance proxy settings through `WebsiteDataManager`.
- Real X11 / `WebKitWebView` / `WebKitSettings` handles once attached.
- Runtime delegation for navigation, script execution, host/page messaging, zoom, and native print dispatch on the embedded control path.
- Honest implementation/proxy status reporting and Linux documentation updates.

### Completed: iOS

#### Delivered

- Real embedded iOS control hosting through a backend-owned `UIView` attachment and `WKWebView`.
- Shared environment/controller option application on the embedded runtime path.
- Runtime delegation for navigation, JS execution, host/page messaging, zoom, and new-window interception.
- Per-instance proxy application on iOS 17+ through `WKWebsiteDataStore` proxy configuration with explicit `http`, `https`, and `socks5` servers, credentials, and bypass domains.
- Honest implementation/proxy status reporting and iOS documentation updates.

### Completed: Android

#### Delivered

- Real embedded Android control hosting through a backend-owned child `View` attachment and `android.webkit.WebView`.
- Shared environment/controller option flow on the embedded runtime path, with explicit rejection of unsupported per-instance proxy configuration.
- Runtime delegation for navigation, JS execution, host/page messaging, zoom control toggles, new-window interception, synthetic composited frame capture, and native handle exposure.
- Honest implementation/proxy status reporting and Android documentation updates.

### Completed: Browser

#### Delivered

- Real embedded Browser control hosting through Avalonia Browser native hosting and a backend-owned DOM `iframe`.
- Browser-targeted `NativeWebView.Platform.Browser` build with `JSObjectControlHandle` support and a managed DOM/JS bridge.
- Runtime delegation for navigation, history, script execution, host/page messaging, new-window interception, and synthetic render-frame capture within browser security constraints.
- Honest implementation/proxy status reporting and Browser documentation updates.

#### Verification

- Browser-target build of `NativeWebView.Platform.Browser` for `net8.0-browser1.0`.
- Shared repository test/build validation for status, diagnostics, and control-host integration.

#### Exit criteria

- `NativeWebViewPlatformImplementationStatusMatrix.Get(Browser).EmbeddedControl == RuntimeImplemented`
- Browser docs clearly describe the real host model and its security/runtime constraints.

## Definition of Done For This Branch

- Add a public implementation-status matrix API so consumers can inspect the honest current repo status in code.
- Add diagnostics warnings for platforms where the embedded control is still contract-only.
- Update README/docs/reference pages so they distinguish real runtime implementation from platform capability contracts.
- Write this bring-up plan to `plan/` so the remaining platforms can be implemented one-by-one without redoing the status audit.
