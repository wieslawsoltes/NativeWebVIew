using NativeWebView.Core;

namespace NativeWebView.Platform.Windows;

public sealed class WindowsWebAuthenticationBrokerBackend : IWebAuthenticationBrokerBackend
{
    public WindowsWebAuthenticationBrokerBackend()
    {
        Platform = NativeWebViewPlatform.Windows;
        Features = WindowsPlatformFeatures.Instance;
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

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(WebAuthenticationBrokerBackendSupport.RuntimeUnavailable());
        }

        return WebAuthenticationBrokerBackendSupport.AuthenticateWithDialogAsync(
            new WindowsNativeWebDialogBackend(),
            requestUri,
            callbackUri,
            options,
            cancellationToken);
    }

    public void Dispose()
    {
    }
}
