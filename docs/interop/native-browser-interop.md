# Native Handle Interop

`NativeWebView.Interop` exposes strongly-typed native handle access points.

## Interfaces

- `INativeWebViewPlatformHandleProvider`
- `INativeWebDialogPlatformHandleProvider`

## Handle access from facades

- `NativeWebView.TryGetPlatformHandle`
- `NativeWebView.TryGetViewHandle`
- `NativeWebView.TryGetControllerHandle`
- `NativeWebDialog.TryGetPlatformHandle`
- `NativeWebDialog.TryGetDialogHandle`
- `NativeWebDialog.TryGetHostWindowHandle`

## Usage pattern

```csharp
if (webView.TryGetViewHandle(out var handle))
{
    Console.WriteLine($"Native handle: {handle.Handle} ({handle.HandleDescriptor})");
}
```

## Contract

- Return `false` when the handle is unavailable.
- Return non-zero handle values for supported providers.
- Keep descriptor strings stable for diagnostics.
