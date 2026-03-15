---
title: "Android"
---

# Android

## Backend

- Package: `NativeWebView.Platform.Android`
- Platform enum: `NativeWebViewPlatform.Android`
- Native engine: `android.webkit.WebView`

## Current Repo Implementation Status

- `NativeWebView`: implemented when `NativeWebView.Platform.Android` is built with the .NET 8 Android workload. The runtime path uses a backend-owned child `View` attachment plus `android.webkit.WebView`.
- Minimum runtime version for the current backend package: `Android 7.0 / API 24+`.
- `NativeWebDialog`: unsupported in the current implementation.
- `WebAuthenticationBroker`: implemented when `NativeWebView.Platform.Android` is built with the .NET 8 Android workload. The runtime path uses a dedicated authentication activity and `android.webkit.WebView`.
- Check `NativeWebViewPlatformImplementationStatusMatrix.Get(NativeWebViewPlatform.Android)` in code when you need the honest current repo status.

## Platform Engine Capability

- Embedded view
- GPU surface rendering
- Offscreen rendering
- Authentication broker
- Context menu and zoom
- New window interception
- Environment and controller options
- Native handles
- Cookie manager and command manager

## Unsupported in the Current Implementation

- Dialog backend
- Desktop-only print UI and DevTools behaviors
- Per-instance proxy application
- `WebAuthenticationBroker.UseHttpPost`

## Registration

```csharp
factory.UseNativeWebViewAndroid();
```

## Diagnostics Notes

Diagnostics use `ANDROID_API_LEVEL` for minimum API-level enforcement (`24+`).
The real runtime path is compiled from the Android-targeted backend assembly; the default `net8.0` build remains a contract/stub asset for non-Android hosts.

## Proxy Notes

- AndroidX `ProxyController` applies proxy overrides process-wide for the app, not per `WebView` instance.
- The current embedded Android `NativeWebView` runtime does not integrate that app-wide override, so per-instance proxy configuration remains unsupported.

## Authentication Notes

- The current Android `WebAuthenticationBroker` implementation launches a dedicated activity-hosted `WebView` session and completes when navigation reaches the callback scheme/host/path.
- Popup windows are redirected back into the active auth `WebView` session.
- `UseHttpPost` is not currently implemented on the Android runtime path.
