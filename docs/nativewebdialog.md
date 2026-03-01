# NativeWebDialog

`NativeWebDialog` is the dialog/window-hosted browser facade.

## Availability

- Desktop platforms: supported.
- iOS, Android, Browser: not supported by design in this phase; creating dialog backend returns an unsupported backend contract.

## Main properties

- `IsVisible`, `CurrentUrl`
- `CanGoBack`, `CanGoForward`
- `IsDevToolsEnabled`, `IsContextMenuEnabled`, `IsStatusBarEnabled`, `IsZoomControlEnabled`
- `ZoomFactor`, `HeaderString`, `UserAgentString`

## Main methods

- `Show`, `Close`, `Move`, `Resize`
- `Navigate`, `Reload`, `Stop`, `GoBack`, `GoForward`
- `ExecuteScriptAsync`
- `PostWebMessageAsJsonAsync`, `PostWebMessageAsStringAsync`
- `OpenDevToolsWindow`
- `PrintAsync`, `ShowPrintUiAsync`
- `SetZoomFactor`, `SetUserAgent`, `SetHeader`

## Main events

- Visibility: `Shown`, `Closed`
- Navigation: `NavigationStarted`, `NavigationCompleted`
- Messaging/interception: `WebMessageReceived`, `NewWindowRequested`, `WebResourceRequested`, `ContextMenuRequested`
