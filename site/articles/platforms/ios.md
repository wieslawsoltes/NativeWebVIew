---
title: "iOS"
---

# iOS

## Backend

- Package: `NativeWebView.Platform.iOS`
- Platform enum: `NativeWebViewPlatform.IOS`
- Native engine: `WKWebView`

## Current Repo Implementation Status

- `NativeWebView`: implemented when `NativeWebView.Platform.iOS` is built with the .NET 8 Apple workload. The runtime path uses a backend-owned `UIView` attachment plus `WKWebView`.
- Minimum runtime version for the current backend package: `iOS 17+`.
- `NativeWebDialog`: unsupported in the current implementation.
- `WebAuthenticationBroker`: implemented when `NativeWebView.Platform.iOS` is built with the .NET 8 Apple workload. The runtime path uses a modal `WKWebView` controller.
- Check `NativeWebViewPlatformImplementationStatusMatrix.Get(NativeWebViewPlatform.IOS)` in code when you need the honest current repo status.

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
- `Proxy.AutoConfigUrl`
- `WebAuthenticationBroker.UseHttpPost`

## Registration

```csharp
factory.UseNativeWebViewIOS();
```

## Proxy Notes

- `WKWebsiteDataStore.proxyConfigurations` is available on `iOS 17+`.
- The current repo runtime applies explicit `http`, `https`, and `socks5` per-instance proxy settings on `iOS 17+`.
- Proxy credentials and bypass domains are applied on the runtime path.
- `Proxy.AutoConfigUrl` remains unsupported in the current iOS runtime path.
- Storage-path and profile-name values contribute to a dedicated `WKWebsiteDataStore` identity instead of mapping directly to physical directories.

## Authentication Notes

- The current iOS `WebAuthenticationBroker` implementation presents a modal `WKWebView` flow and completes when navigation reaches the callback scheme/host/path.
- `UseHttpPost` is not currently implemented on the iOS runtime path.
