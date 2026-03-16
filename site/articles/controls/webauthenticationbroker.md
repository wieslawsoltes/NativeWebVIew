---
title: "WebAuthenticationBroker"
---

# WebAuthenticationBroker

`WebAuthenticationBroker` exposes a unified authentication flow surface across supported backends.

## Availability

- Windows, macOS, Linux: implemented through dedicated native dialog-hosted web sessions.
- iOS: implemented through a modal `WKWebView` flow when the iOS backend is built with the .NET 8 Apple workload.
- Android: implemented through a dedicated authentication activity when the Android backend is built with the .NET 8 Android workload.
- Browser: implemented through a popup-window flow on the browser runtime path.

## API

- `AuthenticateAsync(Uri requestUri, Uri callbackUri, WebAuthenticationOptions options, CancellationToken cancellationToken)`

## Options

- `None`
- `SilentMode`
- `UseTitle`
- `UseHttpPost`
- `UseCorporateNetwork`
- `UseWebAuthenticationBroker`

## Result

| Field | Meaning |
| --- | --- |
| `ResponseStatus` | `Success`, `UserCancel`, or `ErrorHttp`. |
| `ResponseData` | Callback payload when available. |
| `ResponseErrorDetail` | Backend-specific error code when the status is `ErrorHttp`. |

## Runtime Notes

- Authentication completes when navigation reaches the callback scheme, host, port, and path. Query-string and fragment values from the callback URL are preserved in `ResponseData`.
- `SilentMode` currently returns `UserCancel` across the runtime backends in this repo.
- `UseHttpPost` is currently unsupported on every runtime backend in this repo and returns `ErrorHttp`.
- Browser authentication requires popup support plus an inspectable `http` or `https` callback URL. Custom-scheme callbacks are not supported there.
- When a platform-specific interactive runtime cannot be launched on the current host or TFM, the broker returns `ErrorHttp` instead of reporting a false user cancel.
- Desktop runtimes use dedicated dialog-hosted sessions, while iOS and Android use workload-targeted modal or activity-hosted sessions rather than the default `net8.0` contract assemblies.

Use `WebAuthenticationBroker` when you need a callback-driven auth session. Use [`NativeWebDialog`](nativewebdialog.md) instead when you need a general-purpose desktop browser window.

## Security Validation

Controller-level guards validate that:

- request URI is absolute and uses `http` or `https`,
- callback URI is absolute,
- callback scheme is not `javascript`, `data`, `file`, `about`, or `blob`,
- request and callback URIs do not include `UserInfo`.

## Typical Usage Pattern

```csharp
using NativeWebView.Auth;
using NativeWebView.Core;

NativeWebViewRuntime.EnsureCurrentPlatformRegistered();

using var broker = new WebAuthenticationBroker();
var result = await broker.AuthenticateAsync(
    new Uri("https://example.com/auth"),
    new Uri("https://example.com/callback"),
    WebAuthenticationOptions.None,
    cancellationToken);
```

## Related

- [Platform Notes](../platforms/readme.md)
- [Platform Prerequisites](../diagnostics/platform-prerequisites.md)
