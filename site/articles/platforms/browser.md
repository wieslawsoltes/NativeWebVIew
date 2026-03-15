---
title: "Browser"
---

# Browser

## Backend

- Package: `NativeWebView.Platform.Browser`
- Platform enum: `NativeWebViewPlatform.Browser`
- Native engine: browser-host integration for WebAssembly/browser targets

## Current Repo Implementation Status

- `NativeWebView`: implemented. The package now ships a browser-targeted embedded control runtime backed by Avalonia Browser native hosting plus a real DOM `iframe`.
- `NativeWebDialog`: unsupported in the current implementation.
- `WebAuthenticationBroker`: implemented through a popup-window flow that monitors the callback URL from browser runtime code.
- Check `NativeWebViewPlatformImplementationStatusMatrix.Get(NativeWebViewPlatform.Browser)` in code when you need the honest current repo status.

## Platform Engine Capability

- Embedded view
- GPU surface rendering
- Offscreen rendering
- Authentication broker
- New window interception
- Environment and controller options
- Native handles
- Cookie manager and command manager

## Runtime Notes

- Dialog backend: unsupported in the current browser runtime.
- Desktop windowing features: unsupported on the browser runtime path.
- Per-instance proxy configuration: unsupported on the browser runtime path.
- Arbitrary browser frame restrictions still apply. Pages that block framing with `X-Frame-Options` or `Content-Security-Policy: frame-ancestors` will not host successfully inside the embedded browser runtime.
- Script execution, `window.chrome.webview` emulation, and `NewWindowRequested` interception work when the hosted page is same-origin with the app or when the page explicitly cooperates via `postMessage`. Cross-origin navigation still works, but cross-origin script access does not.
- `HeaderString` and `UserAgentString` remain compatibility properties only on the browser runtime path; the host browser does not expose per-iframe request-header or user-agent overrides here.
- Browser authentication requires popup support plus an inspectable `http` or `https` callback URL that returns control to an origin the app can observe.
- If popup startup is unavailable, the browser auth runtime reports `ErrorHttp` instead of `UserCancel`.
- Custom-scheme callbacks and `UseHttpPost` are not currently implemented on the browser runtime path.

## Registration

```csharp
factory.UseNativeWebViewBrowser();
```

## Diagnostics Notes

Set `NATIVEWEBVIEW_BROWSER_POPUP_SUPPORT=false` or `0` to force the popup-support warning path during diagnostics testing.

## Proxy Notes

- Browser targets run inside the host browser’s networking stack.
- The current implementation does not expose per-instance proxy control for browser-hosted `NativeWebView` instances.
