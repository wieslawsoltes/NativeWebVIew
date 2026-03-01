# Environment and Controller Options

`NativeWebView` exposes two option-request events during initialization.

## Environment options event

Event: `CoreWebView2EnvironmentRequested`

Option model:

- `BrowserExecutableFolder`
- `UserDataFolder`
- `Language`
- `AdditionalBrowserArguments`
- `TargetCompatibleBrowserVersion`
- `AllowSingleSignOnUsingOSPrimaryAccount`

Use this event to mutate defaults before backend initialization completes.

## Controller options event

Event: `CoreWebView2ControllerOptionsRequested`

Option model:

- `ProfileName`
- `IsInPrivateModeEnabled`
- `ScriptLocale`

Use this event to configure profile/incognito/script-locale behavior where the backend supports it.

## Notes

- Events are raised once per backend initialization in the current implementation.
- Platforms that cannot apply some values can still expose the event for compatibility.
