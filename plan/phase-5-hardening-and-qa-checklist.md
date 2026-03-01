# Phase 5 Hardening and QA Checklist

## Scope
- Reliability tests for dispose/recreate loops and stress scenarios.
- Security hardening checks for authentication URI validation.
- Performance baselines for startup/navigation/create-dispose paths.

## Implemented
- Added authentication URI security validation in `WebAuthenticationBrokerController`:
  - Request URI must be absolute and use `http`/`https`.
  - Callback URI must be absolute and cannot use unsafe schemes (`javascript`, `data`, `file`, `about`, `blob`).
  - Request/callback URIs must not include embedded user-info.
- Added security regression tests in `ControllerOrchestrationTests` for:
  - Non-http request scheme rejection.
  - Unsafe callback scheme rejection.
  - Relative callback rejection.
  - User-info rejection.
- Added reliability and performance hardening suite in `Phase5HardeningTests`:
  - Leak checks with `WeakReference` and forced GC for:
    - `NativeWebViewController`
    - `NativeWebDialogController`
    - `WebAuthenticationBrokerController`
  - Navigation stress baseline (`WindowsNativeWebViewBackend`).
  - Create/initialize/dispose loop baseline (`NativeWebViewController`).

## Validation
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug`
- `dotnet run --project samples/NativeWebView.Sample.Desktop/NativeWebView.Sample.Desktop.csproj -c Debug`
- `dotnet run --project samples/NativeWebView.Sample.MobileBrowser/NativeWebView.Sample.MobileBrowser.csproj -c Debug`
