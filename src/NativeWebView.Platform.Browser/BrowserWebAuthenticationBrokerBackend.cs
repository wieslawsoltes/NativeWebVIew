#if NATIVEWEBVIEW_BROWSER_RUNTIME
using System.Runtime.InteropServices.JavaScript;
#endif
using NativeWebView.Core;

namespace NativeWebView.Platform.Browser;

public sealed class BrowserWebAuthenticationBrokerBackend : IWebAuthenticationBrokerBackend
{
    public BrowserWebAuthenticationBrokerBackend()
    {
        Platform = NativeWebViewPlatform.Browser;
        Features = BrowserPlatformFeatures.Instance;
    }

    public NativeWebViewPlatform Platform { get; }

    public IWebViewPlatformFeatures Features { get; }

    public async Task<WebAuthenticationResult> AuthenticateAsync(
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
            return WebAuthenticationResult.Error(WebAuthenticationBrokerBackendSupport.NotImplementedError);
        }

        if (WebAuthenticationBrokerBackendSupport.TryCreateImmediateSuccess(requestUri, callbackUri, out var immediateResult))
        {
            return immediateResult;
        }

        if ((options & WebAuthenticationOptions.SilentMode) != 0)
        {
            return WebAuthenticationResult.UserCancel();
        }

        if ((options & WebAuthenticationOptions.UseHttpPost) != 0)
        {
            return WebAuthenticationBrokerBackendSupport.UnsupportedHttpPost();
        }

#if NATIVEWEBVIEW_BROWSER_RUNTIME
        if (OperatingSystem.IsBrowser())
        {
            BrowserNativeWebViewInterop.EnsureInstalled();
            var hostOrigin = TryGetHostOrigin();
            if (!IsInspectableHttpCallback(callbackUri, hostOrigin))
            {
                return WebAuthenticationBrokerBackendSupport.RuntimeUnavailable();
            }

            JSObject? popup = null;
            try
            {
                popup = BrowserNativeWebViewInterop.OpenPopup(
                    requestUri.AbsoluteUri,
                    WebAuthenticationBrokerBackendSupport.CreateInteractiveTitle(requestUri, options));

                if (popup is null)
                {
                    return WebAuthenticationBrokerBackendSupport.RuntimeUnavailable();
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (BrowserNativeWebViewInterop.IsPopupClosed(popup))
                    {
                        return WebAuthenticationResult.UserCancel();
                    }

                    var popupUrl = BrowserNativeWebViewInterop.GetPopupUrl(popup);
                    if (Uri.TryCreate(popupUrl, UriKind.Absolute, out var popupUri) &&
                        WebAuthenticationBrokerBackendSupport.IsCallbackUri(popupUri, callbackUri))
                    {
                        return WebAuthenticationResult.Success(
                            WebAuthenticationBrokerBackendSupport.ToResponseData(popupUri));
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (popup is not null)
                {
                    try
                    {
                        BrowserNativeWebViewInterop.ClosePopup(popup);
                    }
                    catch
                    {
                        // Popup teardown is best effort.
                    }

                    popup.Dispose();
                }
            }
        }
#endif

        if (!IsInspectableHttpCallback(callbackUri, inspectableOrigin: null))
        {
            return WebAuthenticationBrokerBackendSupport.UnsupportedHttpPost();
        }

        return WebAuthenticationBrokerBackendSupport.RuntimeUnavailable();
    }

    public void Dispose()
    {
    }

    internal static bool IsInspectableHttpCallback(Uri callbackUri, Uri? inspectableOrigin)
    {
        if (!callbackUri.IsAbsoluteUri ||
            (!string.Equals(callbackUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(callbackUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (inspectableOrigin is null)
        {
            return true;
        }

        return Uri.Compare(
            callbackUri,
            inspectableOrigin,
            UriComponents.SchemeAndServer,
            UriFormat.SafeUnescaped,
            StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static Uri? TryGetHostOrigin()
    {
#if NATIVEWEBVIEW_BROWSER_RUNTIME
        var origin = BrowserNativeWebViewInterop.GetHostOrigin();
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return uri;
        }
#endif

        return null;
    }
}
