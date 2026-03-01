# WebAuthenticationBroker

`WebAuthenticationBroker` exposes a unified auth flow surface.

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

- `ResponseStatus`: `Success`, `UserCancel`, `ErrorHttp`
- `ResponseData`: callback payload when available
- `ResponseErrorDetail`: backend-specific error code when status is `ErrorHttp`

## Security validation

Controller-level guards validate:

- Request URI is absolute and uses `http` or `https`.
- Callback URI is absolute.
- Callback scheme is not one of `javascript`, `data`, `file`, `about`, `blob`.
- Request and callback URIs do not include `UserInfo`.
