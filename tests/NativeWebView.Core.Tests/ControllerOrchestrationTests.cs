using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

#pragma warning disable CS0067
public sealed class ControllerOrchestrationTests
{
    [Fact]
    public async Task NativeWebViewController_InitializeAsync_IsIdempotent()
    {
        var backend = new TestWebViewBackend();
        using var controller = new NativeWebViewController(backend);

        await Task.WhenAll(
            controller.InitializeAsync().AsTask(),
            controller.InitializeAsync().AsTask(),
            controller.InitializeAsync().AsTask());

        Assert.Equal(1, backend.InitializeCallCount);
        Assert.Equal(NativeWebComponentState.Ready, controller.State);
        Assert.True(controller.IsInitialized);
    }

    [Fact]
    public void NativeWebViewController_TracksNavigationSnapshot()
    {
        var backend = new TestWebViewBackend();
        using var controller = new NativeWebViewController(backend);

        controller.Navigate("https://example.com/first");
        controller.Navigate("https://example.com/second");

        Assert.Equal(new Uri("https://example.com/second"), controller.CurrentUrl);
        Assert.True(controller.CanGoBack);
        Assert.False(controller.CanGoForward);

        controller.GoBack();

        Assert.Equal(new Uri("https://example.com/first"), controller.CurrentUrl);
        Assert.False(controller.CanGoBack);
        Assert.True(controller.CanGoForward);
    }

    [Fact]
    public void NativeWebViewController_DoesNotUpdateSnapshot_WhenNavigationIsCancelled()
    {
        var backend = new TestWebViewBackend();
        using var controller = new NativeWebViewController(backend);

        controller.Navigate("https://example.com/first");
        Assert.Equal(new Uri("https://example.com/first"), controller.CurrentUrl);

        controller.NavigationStarted += (_, e) => e.Cancel = true;
        controller.Navigate("https://example.com/cancelled");

        Assert.Equal(new Uri("https://example.com/first"), controller.CurrentUrl);
    }

    [Fact]
    public void NativeWebViewController_StopsEventDispatch_AfterDispose()
    {
        var backend = new TestWebViewBackend();
        var controller = new NativeWebViewController(backend);

        var messageCount = 0;
        controller.WebMessageReceived += (_, _) => messageCount++;

        backend.EmitWebMessage("first");
        Assert.Equal(1, messageCount);

        controller.Dispose();
        Assert.Equal(NativeWebComponentState.Disposed, controller.State);

        backend.EmitWebMessage("second");
        Assert.Equal(1, messageCount);

        Assert.Throws<ObjectDisposedException>(() => controller.Navigate("https://example.com/disposed"));
    }

    [Fact]
    public async Task NativeWebViewController_DisposeDuringInitialization_StaysDisposed()
    {
        var backend = new TestWebViewBackend(
            initializeDelayMilliseconds: 100,
            allowInitializeAfterDispose: true);
        var controller = new NativeWebViewController(backend);

        var initializeTask = controller.InitializeAsync().AsTask();
        await backend.WaitForInitializationStartAsync();

        controller.Dispose();
        await initializeTask;

        Assert.Equal(NativeWebComponentState.Disposed, controller.State);
        Assert.Throws<ObjectDisposedException>(() => controller.Navigate("https://example.com/disposed"));
    }

    [Fact]
    public void NativeWebDialogController_TracksVisibility_AndDisposal()
    {
        var backend = new TestDialogBackend();
        var controller = new NativeWebDialogController(backend);

        Assert.False(controller.IsVisible);

        controller.Show();
        Assert.True(controller.IsVisible);

        controller.Close();
        Assert.False(controller.IsVisible);

        controller.Dispose();
        Assert.Equal(NativeWebComponentState.Disposed, controller.State);

        Assert.Throws<ObjectDisposedException>(() => controller.Show());
    }

    [Fact]
    public async Task WebAuthenticationBrokerController_SerializesRequests()
    {
        var backend = new TestAuthenticationBackend(delayMilliseconds: 50);
        using var controller = new WebAuthenticationBrokerController(backend);

        var requestUri = new Uri("https://example.com/auth");
        var callbackUri = new Uri("https://example.com/callback");

        var first = controller.AuthenticateAsync(requestUri, callbackUri);
        var second = controller.AuthenticateAsync(requestUri, callbackUri);

        await Task.WhenAll(first, second);

        Assert.Equal(2, backend.CallCount);
        Assert.Equal(1, backend.MaxConcurrentRequests);
        Assert.Equal(WebAuthenticationBrokerState.Ready, controller.State);
    }

    [Fact]
    public void WebAuthenticationBrokerController_DisposesDisposableBackend()
    {
        var backend = new TestAuthenticationBackend(delayMilliseconds: 1);
        var controller = new WebAuthenticationBrokerController(backend);

        controller.Dispose();

        Assert.True(backend.IsDisposed);
    }

    [Fact]
    public async Task WebAuthenticationBrokerController_RejectsNonHttpRequestUri()
    {
        var backend = new TestAuthenticationBackend(delayMilliseconds: 1);
        using var controller = new WebAuthenticationBrokerController(backend);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.AuthenticateAsync(
            new Uri("ftp://example.com/auth"),
            new Uri("https://example.com/callback")));

        Assert.Equal("requestUri", exception.ParamName);
        Assert.Equal(0, backend.CallCount);
    }

    [Fact]
    public async Task WebAuthenticationBrokerController_RejectsUnsafeCallbackUriScheme()
    {
        var backend = new TestAuthenticationBackend(delayMilliseconds: 1);
        using var controller = new WebAuthenticationBrokerController(backend);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.AuthenticateAsync(
            new Uri("https://example.com/auth"),
            new Uri("javascript:alert(1)")));

        Assert.Equal("callbackUri", exception.ParamName);
        Assert.Equal(0, backend.CallCount);
    }

    [Fact]
    public async Task WebAuthenticationBrokerController_RejectsRelativeCallbackUri()
    {
        var backend = new TestAuthenticationBackend(delayMilliseconds: 1);
        using var controller = new WebAuthenticationBrokerController(backend);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.AuthenticateAsync(
            new Uri("https://example.com/auth"),
            new Uri("/callback", UriKind.Relative)));

        Assert.Equal("callbackUri", exception.ParamName);
        Assert.Equal(0, backend.CallCount);
    }

    [Fact]
    public async Task WebAuthenticationBrokerController_RejectsUserInfoInUris()
    {
        var backend = new TestAuthenticationBackend(delayMilliseconds: 1);
        using var controller = new WebAuthenticationBrokerController(backend);

        var requestException = await Assert.ThrowsAsync<ArgumentException>(() => controller.AuthenticateAsync(
            new Uri("https://user:pass@example.com/auth"),
            new Uri("https://example.com/callback")));
        Assert.Equal("requestUri", requestException.ParamName);
        Assert.Equal(0, backend.CallCount);

        var callbackException = await Assert.ThrowsAsync<ArgumentException>(() => controller.AuthenticateAsync(
            new Uri("https://example.com/auth"),
            new Uri("myapp://user:pass@callback/path")));
        Assert.Equal("callbackUri", callbackException.ParamName);
        Assert.Equal(0, backend.CallCount);
    }

    private sealed class TestWebViewBackend : INativeWebViewBackend
    {
        private readonly List<Uri> _history = [];
        private readonly int _initializeDelayMilliseconds;
        private readonly bool _allowInitializeAfterDispose;
        private readonly TaskCompletionSource<bool> _initializationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _historyIndex = -1;
        private bool _disposed;

        public TestWebViewBackend(
            int initializeDelayMilliseconds = 0,
            bool allowInitializeAfterDispose = false)
        {
            _initializeDelayMilliseconds = initializeDelayMilliseconds;
            _allowInitializeAfterDispose = allowInitializeAfterDispose;
        }

        public NativeWebViewPlatform Platform => NativeWebViewPlatform.Windows;

        public IWebViewPlatformFeatures Features { get; } = new WebViewPlatformFeatures(
            NativeWebViewPlatform.Windows,
            NativeWebViewFeature.EmbeddedView |
            NativeWebViewFeature.DevTools |
            NativeWebViewFeature.ContextMenu |
            NativeWebViewFeature.StatusBar |
            NativeWebViewFeature.ZoomControl |
            NativeWebViewFeature.Printing |
            NativeWebViewFeature.PrintUi |
            NativeWebViewFeature.WebMessageChannel |
            NativeWebViewFeature.ScriptExecution |
            NativeWebViewFeature.NewWindowRequestInterception |
            NativeWebViewFeature.WebResourceRequestInterception |
            NativeWebViewFeature.EnvironmentOptions |
            NativeWebViewFeature.ControllerOptions);

        public Uri? CurrentUrl { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool CanGoBack => _historyIndex > 0;

        public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

        public bool IsDevToolsEnabled { get; set; } = true;

        public bool IsContextMenuEnabled { get; set; } = true;

        public bool IsStatusBarEnabled { get; set; } = true;

        public bool IsZoomControlEnabled { get; set; } = true;

        public double ZoomFactor { get; private set; } = 1.0;

        public string? HeaderString { get; private set; }

        public string? UserAgentString { get; private set; }

        public int InitializeCallCount { get; private set; }

        public event EventHandler<CoreWebViewInitializedEventArgs>? CoreWebView2Initialized;

        public event EventHandler<NativeWebViewNavigationStartedEventArgs>? NavigationStarted;

        public event EventHandler<NativeWebViewNavigationCompletedEventArgs>? NavigationCompleted;

        public event EventHandler<NativeWebViewMessageReceivedEventArgs>? WebMessageReceived;

        public event EventHandler<NativeWebViewOpenDevToolsRequestedEventArgs>? OpenDevToolsRequested;

        public event EventHandler<NativeWebViewDestroyRequestedEventArgs>? DestroyRequested;

        public event EventHandler<NativeWebViewRequestCustomChromeEventArgs>? RequestCustomChrome;

        public event EventHandler<NativeWebViewRequestParentWindowPositionEventArgs>? RequestParentWindowPosition;

        public event EventHandler<NativeWebViewBeginMoveDragEventArgs>? BeginMoveDrag;

        public event EventHandler<NativeWebViewBeginResizeDragEventArgs>? BeginResizeDrag;

        public event EventHandler<NativeWebViewNewWindowRequestedEventArgs>? NewWindowRequested;

        public event EventHandler<NativeWebViewResourceRequestedEventArgs>? WebResourceRequested;

        public event EventHandler<NativeWebViewContextMenuRequestedEventArgs>? ContextMenuRequested;

        public event EventHandler<NativeWebViewNavigationHistoryChangedEventArgs>? NavigationHistoryChanged;

        public event EventHandler<CoreWebViewEnvironmentRequestedEventArgs>? CoreWebView2EnvironmentRequested;

        public event EventHandler<CoreWebViewControllerOptionsRequestedEventArgs>? CoreWebView2ControllerOptionsRequested;

        public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (!_allowInitializeAfterDispose)
            {
                EnsureNotDisposed();
            }

            cancellationToken.ThrowIfCancellationRequested();
            _initializationStarted.TrySetResult(true);

            if (_initializeDelayMilliseconds > 0)
            {
                await Task.Delay(_initializeDelayMilliseconds, cancellationToken);
            }

            if (!_allowInitializeAfterDispose)
            {
                EnsureNotDisposed();
            }

            InitializeCallCount++;
            if (!IsInitialized)
            {
                IsInitialized = true;
                CoreWebView2EnvironmentRequested?.Invoke(this, new CoreWebViewEnvironmentRequestedEventArgs(new NativeWebViewEnvironmentOptions()));
                CoreWebView2ControllerOptionsRequested?.Invoke(this, new CoreWebViewControllerOptionsRequestedEventArgs(new NativeWebViewControllerOptions()));
                CoreWebView2Initialized?.Invoke(this, new CoreWebViewInitializedEventArgs(isSuccess: true));
            }

        }

        public Task WaitForInitializationStartAsync()
        {
            return _initializationStarted.Task;
        }

        public void Navigate(string url)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("Invalid URL.", nameof(url));
            }

            Navigate(uri);
        }

        public void Navigate(Uri uri)
        {
            EnsureNotDisposed();
            ArgumentNullException.ThrowIfNull(uri);

            var started = new NativeWebViewNavigationStartedEventArgs(uri, isRedirected: false);
            NavigationStarted?.Invoke(this, started);

            if (started.Cancel)
            {
                return;
            }

            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            _history.Add(uri);
            _historyIndex = _history.Count - 1;
            CurrentUrl = uri;

            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(uri, isSuccess: true, httpStatusCode: 200));
            NavigationHistoryChanged?.Invoke(this, new NativeWebViewNavigationHistoryChangedEventArgs(CanGoBack, CanGoForward));
        }

        public void Reload()
        {
            EnsureNotDisposed();

            if (CurrentUrl is null)
            {
                return;
            }

            NavigationStarted?.Invoke(this, new NativeWebViewNavigationStartedEventArgs(CurrentUrl, isRedirected: false));
            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(CurrentUrl, isSuccess: true, httpStatusCode: 200));
        }

        public void Stop()
        {
            EnsureNotDisposed();
        }

        public void GoBack()
        {
            EnsureNotDisposed();

            if (!CanGoBack)
            {
                return;
            }

            _historyIndex--;
            CurrentUrl = _history[_historyIndex];
            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(CurrentUrl, isSuccess: true, httpStatusCode: 200));
            NavigationHistoryChanged?.Invoke(this, new NativeWebViewNavigationHistoryChangedEventArgs(CanGoBack, CanGoForward));
        }

        public void GoForward()
        {
            EnsureNotDisposed();

            if (!CanGoForward)
            {
                return;
            }

            _historyIndex++;
            CurrentUrl = _history[_historyIndex];
            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(CurrentUrl, isSuccess: true, httpStatusCode: 200));
            NavigationHistoryChanged?.Invoke(this, new NativeWebViewNavigationHistoryChangedEventArgs(CanGoBack, CanGoForward));
        }

        public Task<string?> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>("ok");
        }

        public Task PostWebMessageAsJsonAsync(string message, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            WebMessageReceived?.Invoke(this, new NativeWebViewMessageReceivedEventArgs(message: null, json: message));
            return Task.CompletedTask;
        }

        public Task PostWebMessageAsStringAsync(string message, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            WebMessageReceived?.Invoke(this, new NativeWebViewMessageReceivedEventArgs(message, json: null));
            return Task.CompletedTask;
        }

        public void OpenDevToolsWindow()
        {
            EnsureNotDisposed();
            OpenDevToolsRequested?.Invoke(this, new NativeWebViewOpenDevToolsRequestedEventArgs());
        }

        public Task<NativeWebViewPrintResult> PrintAsync(NativeWebViewPrintSettings? settings = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            _ = settings;
            return Task.FromResult(new NativeWebViewPrintResult(NativeWebViewPrintStatus.Success));
        }

        public Task<bool> ShowPrintUiAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public void SetZoomFactor(double zoomFactor)
        {
            EnsureNotDisposed();
            ZoomFactor = zoomFactor;
        }

        public void SetUserAgent(string? userAgent)
        {
            EnsureNotDisposed();
            UserAgentString = userAgent;
        }

        public void SetHeader(string? header)
        {
            EnsureNotDisposed();
            HeaderString = header;
        }

        public bool TryGetCommandManager(out INativeWebViewCommandManager? commandManager)
        {
            commandManager = null;
            return false;
        }

        public bool TryGetCookieManager(out INativeWebViewCookieManager? cookieManager)
        {
            cookieManager = null;
            return false;
        }

        public void MoveFocus(NativeWebViewFocusMoveDirection direction)
        {
            EnsureNotDisposed();
            _ = direction;
        }

        public void EmitWebMessage(string message)
        {
            WebMessageReceived?.Invoke(this, new NativeWebViewMessageReceivedEventArgs(message, json: null));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DestroyRequested?.Invoke(this, new NativeWebViewDestroyRequestedEventArgs("Disposed"));
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    private sealed class TestDialogBackend : INativeWebDialogBackend
    {
        private bool _disposed;
        private readonly List<Uri> _history = [];
        private int _historyIndex = -1;

        public NativeWebViewPlatform Platform => NativeWebViewPlatform.Windows;

        public IWebViewPlatformFeatures Features { get; } = new WebViewPlatformFeatures(
            NativeWebViewPlatform.Windows,
            NativeWebViewFeature.Dialog |
            NativeWebViewFeature.ScriptExecution |
            NativeWebViewFeature.WebMessageChannel);

        public bool IsVisible { get; private set; }

        public Uri? CurrentUrl { get; private set; }

        public bool CanGoBack => _historyIndex > 0;

        public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

        public bool IsDevToolsEnabled { get; set; }

        public bool IsContextMenuEnabled { get; set; }

        public bool IsStatusBarEnabled { get; set; }

        public bool IsZoomControlEnabled { get; set; }

        public double ZoomFactor { get; private set; } = 1.0;

        public string? HeaderString { get; private set; }

        public string? UserAgentString { get; private set; }

        public event EventHandler<EventArgs>? Shown;

        public event EventHandler<EventArgs>? Closed;

        public event EventHandler<NativeWebViewNavigationStartedEventArgs>? NavigationStarted;

        public event EventHandler<NativeWebViewNavigationCompletedEventArgs>? NavigationCompleted;

        public event EventHandler<NativeWebViewMessageReceivedEventArgs>? WebMessageReceived;

        public event EventHandler<NativeWebViewNewWindowRequestedEventArgs>? NewWindowRequested;

        public event EventHandler<NativeWebViewResourceRequestedEventArgs>? WebResourceRequested;

        public event EventHandler<NativeWebViewContextMenuRequestedEventArgs>? ContextMenuRequested;

        public void Show(NativeWebDialogShowOptions? options = null)
        {
            EnsureNotDisposed();
            _ = options;
            IsVisible = true;
            Shown?.Invoke(this, EventArgs.Empty);
        }

        public void Close()
        {
            EnsureNotDisposed();
            IsVisible = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        public void Move(double left, double top)
        {
            EnsureNotDisposed();
            _ = left;
            _ = top;
        }

        public void Resize(double width, double height)
        {
            EnsureNotDisposed();
            _ = width;
            _ = height;
        }

        public void Navigate(string url)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            Navigate(new Uri(url));
        }

        public void Navigate(Uri uri)
        {
            EnsureNotDisposed();
            ArgumentNullException.ThrowIfNull(uri);

            NavigationStarted?.Invoke(this, new NativeWebViewNavigationStartedEventArgs(uri, isRedirected: false));

            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            _history.Add(uri);
            _historyIndex = _history.Count - 1;
            CurrentUrl = uri;

            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(uri, isSuccess: true, httpStatusCode: 200));
        }

        public void Reload()
        {
            EnsureNotDisposed();
        }

        public void Stop()
        {
            EnsureNotDisposed();
        }

        public void GoBack()
        {
            EnsureNotDisposed();

            if (!CanGoBack)
            {
                return;
            }

            _historyIndex--;
            CurrentUrl = _history[_historyIndex];
            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(CurrentUrl, isSuccess: true, httpStatusCode: 200));
        }

        public void GoForward()
        {
            EnsureNotDisposed();

            if (!CanGoForward)
            {
                return;
            }

            _historyIndex++;
            CurrentUrl = _history[_historyIndex];
            NavigationCompleted?.Invoke(this, new NativeWebViewNavigationCompletedEventArgs(CurrentUrl, isSuccess: true, httpStatusCode: 200));
        }

        public Task<string?> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>("ok");
        }

        public Task PostWebMessageAsJsonAsync(string message, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            WebMessageReceived?.Invoke(this, new NativeWebViewMessageReceivedEventArgs(message: null, json: message));
            return Task.CompletedTask;
        }

        public Task PostWebMessageAsStringAsync(string message, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            WebMessageReceived?.Invoke(this, new NativeWebViewMessageReceivedEventArgs(message, json: null));
            return Task.CompletedTask;
        }

        public void OpenDevToolsWindow()
        {
            EnsureNotDisposed();
        }

        public Task<NativeWebViewPrintResult> PrintAsync(NativeWebViewPrintSettings? settings = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            _ = settings;
            return Task.FromResult(new NativeWebViewPrintResult(NativeWebViewPrintStatus.Success));
        }

        public Task<bool> ShowPrintUiAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public void SetZoomFactor(double zoomFactor)
        {
            EnsureNotDisposed();
            ZoomFactor = zoomFactor;
        }

        public void SetUserAgent(string? userAgent)
        {
            EnsureNotDisposed();
            UserAgentString = userAgent;
        }

        public void SetHeader(string? header)
        {
            EnsureNotDisposed();
            HeaderString = header;
        }

        public void Dispose()
        {
            _disposed = true;
            IsVisible = false;
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    private sealed class TestAuthenticationBackend : IWebAuthenticationBrokerBackend, IDisposable
    {
        private readonly int _delayMilliseconds;

        private int _inFlight;

        public TestAuthenticationBackend(int delayMilliseconds)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public NativeWebViewPlatform Platform => NativeWebViewPlatform.Windows;

        public IWebViewPlatformFeatures Features { get; } = new WebViewPlatformFeatures(
            NativeWebViewPlatform.Windows,
            NativeWebViewFeature.AuthenticationBroker);

        public int CallCount { get; private set; }

        public int MaxConcurrentRequests { get; private set; }

        public bool IsDisposed { get; private set; }

        public async Task<WebAuthenticationResult> AuthenticateAsync(
            Uri requestUri,
            Uri callbackUri,
            WebAuthenticationOptions options = WebAuthenticationOptions.None,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requestUri);
            ArgumentNullException.ThrowIfNull(callbackUri);
            _ = options;

            CallCount++;
            var inFlight = Interlocked.Increment(ref _inFlight);
            MaxConcurrentRequests = Math.Max(MaxConcurrentRequests, inFlight);

            try
            {
                await Task.Delay(_delayMilliseconds, cancellationToken).ConfigureAwait(false);
                return WebAuthenticationResult.Success(callbackUri.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
#pragma warning restore CS0067
