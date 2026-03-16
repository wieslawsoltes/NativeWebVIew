namespace NativeWebView.Core;

internal static class WebAuthenticationBrokerBackendSupport
{
    public const int NotImplementedError = unchecked((int)0x80004001);

    private const double DefaultDialogWidth = 480;
    private const double DefaultDialogHeight = 720;

    public static bool IsCallbackUri(Uri? navigationUri, Uri callbackUri)
    {
        ArgumentNullException.ThrowIfNull(callbackUri);

        if (navigationUri is null || !navigationUri.IsAbsoluteUri || !callbackUri.IsAbsoluteUri)
        {
            return false;
        }

        return Uri.Compare(
            navigationUri,
            callbackUri,
            UriComponents.SchemeAndServer | UriComponents.Path,
            UriFormat.SafeUnescaped,
            StringComparison.OrdinalIgnoreCase) == 0;
    }

    public static bool TryCreateImmediateSuccess(Uri requestUri, Uri callbackUri, out WebAuthenticationResult result)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(callbackUri);

        if (IsCallbackUri(requestUri, callbackUri))
        {
            result = WebAuthenticationResult.Success(ToResponseData(requestUri));
            return true;
        }

        result = null!;
        return false;
    }

    public static WebAuthenticationResult UnsupportedHttpPost()
    {
        return WebAuthenticationResult.Error(NotImplementedError);
    }

    public static WebAuthenticationResult RuntimeUnavailable()
    {
        return WebAuthenticationResult.Error(NotImplementedError);
    }

    public static string CreateInteractiveTitle(Uri requestUri, WebAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        if ((options & WebAuthenticationOptions.UseTitle) != 0 &&
            !string.IsNullOrWhiteSpace(requestUri.Host))
        {
            return requestUri.Host;
        }

        return "Authentication";
    }

    public static async Task<WebAuthenticationResult> AuthenticateWithDialogAsync(
        INativeWebDialogBackend dialogBackend,
        Uri requestUri,
        Uri callbackUri,
        WebAuthenticationOptions options = WebAuthenticationOptions.None,
        CancellationToken cancellationToken = default,
        bool supportsHttpPost = false)
    {
        ArgumentNullException.ThrowIfNull(dialogBackend);
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(callbackUri);
        cancellationToken.ThrowIfCancellationRequested();

        if (TryCreateImmediateSuccess(requestUri, callbackUri, out var immediateResult))
        {
            dialogBackend.Dispose();
            return immediateResult;
        }

        if ((options & WebAuthenticationOptions.SilentMode) != 0)
        {
            dialogBackend.Dispose();
            return WebAuthenticationResult.UserCancel();
        }

        if ((options & WebAuthenticationOptions.UseHttpPost) != 0 && !supportsHttpPost)
        {
            dialogBackend.Dispose();
            return UnsupportedHttpPost();
        }

        var completion = new TaskCompletionSource<WebAuthenticationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionState = 0;

        void TryCloseDialog()
        {
            try
            {
                if (dialogBackend.IsVisible)
                {
                    dialogBackend.Close();
                }
            }
            catch
            {
                // Best-effort shutdown for completion paths.
            }
        }

        void TryComplete(WebAuthenticationResult result, bool closeDialog)
        {
            if (Interlocked.CompareExchange(ref completionState, 1, 0) != 0)
            {
                return;
            }

            completion.TrySetResult(result);
            if (closeDialog)
            {
                TryCloseDialog();
            }
        }

        void TryCancel()
        {
            if (Interlocked.CompareExchange(ref completionState, 1, 0) != 0)
            {
                return;
            }

            completion.TrySetCanceled(cancellationToken);
            TryCloseDialog();
        }

        void OnClosed(object? sender, EventArgs e)
        {
            _ = sender;
            _ = e;
            TryComplete(WebAuthenticationResult.UserCancel(), closeDialog: false);
        }

        void OnNavigationStarted(object? sender, NativeWebViewNavigationStartedEventArgs e)
        {
            _ = sender;

            if (!IsCallbackUri(e.Uri, callbackUri))
            {
                return;
            }

            e.Cancel = true;
            TryComplete(WebAuthenticationResult.Success(ToResponseData(e.Uri!)), closeDialog: true);
        }

        void OnNewWindowRequested(object? sender, NativeWebViewNewWindowRequestedEventArgs e)
        {
            _ = sender;

            if (e.Uri is null)
            {
                return;
            }

            if (IsCallbackUri(e.Uri, callbackUri))
            {
                e.Handled = true;
                TryComplete(WebAuthenticationResult.Success(ToResponseData(e.Uri)), closeDialog: true);
                return;
            }

            e.Handled = true;

            try
            {
                dialogBackend.Navigate(e.Uri);
            }
            catch
            {
                // Leave the current flow running if popup redirection fails.
            }
        }

        dialogBackend.Closed += OnClosed;
        dialogBackend.NavigationStarted += OnNavigationStarted;
        dialogBackend.NewWindowRequested += OnNewWindowRequested;

        using var cancellationRegistration = cancellationToken.Register(static state =>
        {
            ((Action)state!).Invoke();
        }, (Action)TryCancel);

        try
        {
            dialogBackend.Show(new NativeWebDialogShowOptions
            {
                Title = CreateInteractiveTitle(requestUri, options),
                Width = DefaultDialogWidth,
                Height = DefaultDialogHeight,
                CenterOnParent = true,
            });

            // Force runtime initialization to complete or fail before the auth flow waits on navigation events.
            await dialogBackend.ExecuteScriptAsync("void 0", cancellationToken).ConfigureAwait(false);
            dialogBackend.Navigate(requestUri);
            return await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return RuntimeUnavailable();
        }
        finally
        {
            dialogBackend.Closed -= OnClosed;
            dialogBackend.NavigationStarted -= OnNavigationStarted;
            dialogBackend.NewWindowRequested -= OnNewWindowRequested;
            dialogBackend.Dispose();
        }
    }

    public static string ToResponseData(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();
    }
}
