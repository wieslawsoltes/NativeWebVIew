#if NATIVEWEBVIEW_IOS_RUNTIME
using System.Linq;
using Foundation;
using UIKit;
using WebKit;
#endif
using NativeWebView.Core;

namespace NativeWebView.Platform.iOS;

public sealed class IOSWebAuthenticationBrokerBackend : IWebAuthenticationBrokerBackend
{
    public IOSWebAuthenticationBrokerBackend()
    {
        Platform = NativeWebViewPlatform.IOS;
        Features = IOSPlatformFeatures.Instance;
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

#if NATIVEWEBVIEW_IOS_RUNTIME
        if (OperatingSystem.IsIOS())
        {
            return AuthenticateRuntimeAsync(requestUri, callbackUri, options, cancellationToken);
        }
#endif

        return Task.FromResult(WebAuthenticationBrokerBackendSupport.RuntimeUnavailable());
    }

    public void Dispose()
    {
    }

#if NATIVEWEBVIEW_IOS_RUNTIME
    private static Task<WebAuthenticationResult> AuthenticateRuntimeAsync(
        Uri requestUri,
        Uri callbackUri,
        WebAuthenticationOptions options,
        CancellationToken cancellationToken)
    {
        var session = new IOSAuthenticationSession(requestUri, callbackUri, options, cancellationToken);
        return session.RunAsync();
    }

    private sealed class IOSAuthenticationSession : NSObject
    {
        private readonly Uri _requestUri;
        private readonly Uri _callbackUri;
        private readonly WebAuthenticationOptions _options;
        private readonly CancellationToken _cancellationToken;
        private readonly TaskCompletionSource<WebAuthenticationResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private CancellationTokenRegistration _cancellationRegistration;
        private UINavigationController? _navigationController;
        private UIViewController? _contentController;
        private WKWebView? _webView;
        private IOSNavigationDelegate? _navigationDelegate;
        private IOSUiDelegate? _uiDelegate;

        private int _completionState;

        public IOSAuthenticationSession(
            Uri requestUri,
            Uri callbackUri,
            WebAuthenticationOptions options,
            CancellationToken cancellationToken)
        {
            _requestUri = requestUri;
            _callbackUri = callbackUri;
            _options = options;
            _cancellationToken = cancellationToken;
        }

        public async Task<WebAuthenticationResult> RunAsync()
        {
            _cancellationRegistration = _cancellationToken.Register(static state =>
            {
                ((IOSAuthenticationSession)state!).CancelFromToken();
            }, this);

            try
            {
                await InvokeOnMainThreadAsync(ShowAsync, _cancellationToken).ConfigureAwait(false);
                return await _completion.Task.ConfigureAwait(false);
            }
            finally
            {
                _cancellationRegistration.Dispose();
                await InvokeOnMainThreadAsync(DismissAsync, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private Task ShowAsync()
        {
            var presenter = GetTopViewController();
            if (presenter is null)
            {
                TryComplete(WebAuthenticationResult.UserCancel());
                return Task.CompletedTask;
            }

            _contentController = new UIViewController
            {
                Title = WebAuthenticationBrokerBackendSupport.CreateInteractiveTitle(_requestUri, _options),
            };
            _contentController.View!.BackgroundColor = UIColor.SystemBackground;

            _navigationController = new UINavigationController(_contentController)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FullScreen,
            };

            _contentController.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(
                UIBarButtonSystemItem.Cancel,
                (_, _) => TryComplete(WebAuthenticationResult.UserCancel()));

            var configuration = new WKWebViewConfiguration
            {
                WebsiteDataStore = WKWebsiteDataStore.NonPersistentDataStore,
                Preferences = new WKPreferences
                {
                    JavaScriptEnabled = true,
                },
            };

            _webView = new WKWebView(_contentController.View.Bounds, configuration)
            {
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            };

            _navigationDelegate = new IOSNavigationDelegate(this);
            _uiDelegate = new IOSUiDelegate(this);

            _webView.NavigationDelegate = _navigationDelegate;
            _webView.UIDelegate = _uiDelegate;

            _contentController.View.AddSubview(_webView);
            presenter.PresentViewController(_navigationController, true, null);
            _webView.LoadRequest(new NSUrlRequest(new NSUrl(_requestUri.AbsoluteUri)));
            return Task.CompletedTask;
        }

        private Task DismissAsync()
        {
            if (_webView is not null)
            {
                _webView.NavigationDelegate = null;
                _webView.UIDelegate = null;
                _webView.StopLoading();
                _webView.RemoveFromSuperview();
                _webView.Dispose();
                _webView = null;
            }

            _navigationDelegate?.Dispose();
            _navigationDelegate = null;
            _uiDelegate?.Dispose();
            _uiDelegate = null;
            _contentController = null;

            if (_navigationController is not null)
            {
                if (_navigationController.PresentingViewController is not null)
                {
                    _navigationController.DismissViewController(true, null);
                }

                _navigationController.Dispose();
                _navigationController = null;
            }

            return Task.CompletedTask;
        }

        public void HandleNavigationAction(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            var requestUrl = navigationAction.Request.Url?.AbsoluteString;
            if (Uri.TryCreate(requestUrl, UriKind.Absolute, out var navigationUri) &&
                WebAuthenticationBrokerBackendSupport.IsCallbackUri(navigationUri, _callbackUri))
            {
                decisionHandler(WKNavigationActionPolicy.Cancel);
                TryComplete(WebAuthenticationResult.Success(
                    WebAuthenticationBrokerBackendSupport.ToResponseData(navigationUri)));
                return;
            }

            decisionHandler(WKNavigationActionPolicy.Allow);
        }

        public void HandlePopupRequest(WKNavigationAction navigationAction)
        {
            var requestUrl = navigationAction.Request.Url?.AbsoluteString;
            if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var navigationUri))
            {
                return;
            }

            if (WebAuthenticationBrokerBackendSupport.IsCallbackUri(navigationUri, _callbackUri))
            {
                TryComplete(WebAuthenticationResult.Success(
                    WebAuthenticationBrokerBackendSupport.ToResponseData(navigationUri)));
                return;
            }

            _webView?.LoadRequest(navigationAction.Request);
        }

        private void CancelFromToken()
        {
            if (Interlocked.CompareExchange(ref _completionState, 1, 0) != 0)
            {
                return;
            }

            _completion.TrySetCanceled(_cancellationToken);
        }

        private void TryComplete(WebAuthenticationResult result)
        {
            if (Interlocked.CompareExchange(ref _completionState, 1, 0) != 0)
            {
                return;
            }

            _completion.TrySetResult(result);
        }

        private static UIViewController? GetTopViewController()
        {
            UIWindow? hostWindow = null;

            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is UIWindowScene windowScene &&
                    windowScene.ActivationState == UISceneActivationState.ForegroundActive)
                {
                    hostWindow = windowScene.Windows.FirstOrDefault(static window => window.IsKeyWindow) ??
                        windowScene.Windows.FirstOrDefault();
                    if (hostWindow is not null)
                    {
                        break;
                    }
                }
            }

            var controller = hostWindow?.RootViewController;
            while (controller?.PresentedViewController is not null)
            {
                controller = controller.PresentedViewController;
            }

            return controller;
        }
    }

    private sealed class IOSNavigationDelegate : WKNavigationDelegate
    {
        private readonly WeakReference<IOSAuthenticationSession> _owner;

        public IOSNavigationDelegate(IOSAuthenticationSession owner)
        {
            _owner = new WeakReference<IOSAuthenticationSession>(owner);
        }

        public override void DecidePolicy(
            WKWebView webView,
            WKNavigationAction navigationAction,
            Action<WKNavigationActionPolicy> decisionHandler)
        {
            if (_owner.TryGetTarget(out var owner))
            {
                owner.HandleNavigationAction(webView, navigationAction, decisionHandler);
                return;
            }

            decisionHandler(WKNavigationActionPolicy.Cancel);
        }
    }

    private sealed class IOSUiDelegate : WKUIDelegate
    {
        private readonly WeakReference<IOSAuthenticationSession> _owner;

        public IOSUiDelegate(IOSAuthenticationSession owner)
        {
            _owner = new WeakReference<IOSAuthenticationSession>(owner);
        }

        public override WKWebView? CreateWebView(
            WKWebView webView,
            WKWebViewConfiguration configuration,
            WKNavigationAction navigationAction,
            WKWindowFeatures windowFeatures)
        {
            _ = configuration;
            _ = windowFeatures;

            if (_owner.TryGetTarget(out var owner))
            {
                owner.HandlePopupRequest(navigationAction);
            }

            return null;
        }
    }

    private static Task InvokeOnMainThreadAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        if (NSThread.Current.IsMainThread)
        {
            return action();
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(static state =>
        {
            ((TaskCompletionSource<bool>)state!).TrySetCanceled();
        }, completion);

        UIApplication.SharedApplication.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
                completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }
#endif
}
