# Phase 4 Parity Checklist

## Scope
- Environment/options request hooks wired for all platform webview backends.
- Native platform handle providers exposed through backends and facades.
- Cookie and command manager abstractions available across all platform webview backends.

## Completed
- Added `EnvironmentOptions` and `ControllerOptions` capability flags to macOS, Linux, iOS, Android, and Browser platform features.
- Added `NativePlatformHandle` and `CommandManager` capability flags to iOS, Android, and Browser platform features.
- Added `CookieManager` and `CommandManager` capability flags to Browser platform features.
- Implemented `INativeWebViewPlatformHandleProvider` on webview backends for:
  - Windows, macOS, Linux, iOS, Android, Browser.
- Implemented `INativeWebDialogPlatformHandleProvider` on dialog backends for:
  - Windows, macOS, Linux.
- Exposed handle-provider access in facades:
  - `NativeWebView.TryGetPlatformHandle(...)`
  - `NativeWebView.TryGetViewHandle(...)`
  - `NativeWebView.TryGetControllerHandle(...)`
  - `NativeWebDialog.TryGetPlatformHandle(...)`
  - `NativeWebDialog.TryGetDialogHandle(...)`
  - `NativeWebDialog.TryGetHostWindowHandle(...)`

## Validation
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug`
- `dotnet run --project samples/NativeWebView.Sample.Desktop/NativeWebView.Sample.Desktop.csproj -c Debug`
- `dotnet run --project samples/NativeWebView.Sample.MobileBrowser/NativeWebView.Sample.MobileBrowser.csproj -c Debug`
