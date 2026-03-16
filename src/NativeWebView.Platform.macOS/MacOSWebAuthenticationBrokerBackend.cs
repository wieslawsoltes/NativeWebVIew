using NativeWebView.Core;

namespace NativeWebView.Platform.macOS;

public sealed class MacOSWebAuthenticationBrokerBackend : IWebAuthenticationBrokerBackend
{
    public MacOSWebAuthenticationBrokerBackend()
    {
        Platform = NativeWebViewPlatform.MacOS;
        Features = MacOSPlatformFeatures.Instance;
    }

    public NativeWebViewPlatform Platform { get; }

    public IWebViewPlatformFeatures Features { get; }

    public Task<WebAuthenticationResult> AuthenticateAsync(
        Uri requestUri,
        Uri callbackUri,
        WebAuthenticationOptions options = WebAuthenticationOptions.None,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(callbackUri);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Features.Supports(NativeWebViewFeature.AuthenticationBroker))
        {
            return Task.FromResult(WebAuthenticationResult.Error(WebAuthenticationBrokerBackendSupport.NotImplementedError));
        }

        if (WebAuthenticationBrokerBackendSupport.TryCreateImmediateSuccess(requestUri, callbackUri, out var immediateResult))
        {
            return Task.FromResult(immediateResult);
        }

        if ((options & WebAuthenticationOptions.SilentMode) != 0)
        {
            return Task.FromResult(WebAuthenticationResult.UserCancel());
        }

        if ((options & WebAuthenticationOptions.UseHttpPost) != 0)
        {
            return Task.FromResult(WebAuthenticationBrokerBackendSupport.UnsupportedHttpPost());
        }

        if (!OperatingSystem.IsMacOS())
        {
            return Task.FromResult(WebAuthenticationBrokerBackendSupport.RuntimeUnavailable());
        }

        return WebAuthenticationBrokerBackendSupport.AuthenticateWithDialogAsync(
            new MacOSNativeWebDialogBackend(),
            requestUri,
            callbackUri,
            options,
            cancellationToken);
    }

    public void Dispose()
    {
    }
}
