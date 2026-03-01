# iOS

## Backend

- Package: `NativeWebView.Platform.iOS`
- Platform enum: `NativeWebViewPlatform.IOS`

## Supported areas

- Embedded view
- Authentication broker
- Context menu and zoom
- New window interception
- Environment/controller options
- Native handles
- Cookie and command manager

## Unsupported in this phase

- Dialog backend
- Desktop-only print UI and devtools behaviors

## Registration

```csharp
factory.UseNativeWebViewIOS();
```
