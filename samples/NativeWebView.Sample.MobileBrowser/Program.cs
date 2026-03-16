using NativeWebView.Core;
using NativeWebView.Interop;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.iOS;

var checks = new List<(NativeWebViewPlatform Platform, string Name, bool Passed, string? Details)>();

await RunPlatformChecksAsync(NativeWebViewPlatform.IOS, factory => factory.UseNativeWebViewIOS(), checks);
await RunPlatformChecksAsync(NativeWebViewPlatform.Android, factory => factory.UseNativeWebViewAndroid(), checks);
await RunPlatformChecksAsync(NativeWebViewPlatform.Browser, factory => factory.UseNativeWebViewBrowser(), checks);

foreach (var platform in new[] { NativeWebViewPlatform.IOS, NativeWebViewPlatform.Android, NativeWebViewPlatform.Browser })
{
    Console.WriteLine($"Mobile/browser matrix for {platform}:");
    foreach (var check in checks.Where(c => c.Platform == platform))
    {
        var status = check.Passed ? "PASS" : "FAIL";
        var details = string.IsNullOrWhiteSpace(check.Details) ? string.Empty : $" ({check.Details})";
        Console.WriteLine($"[{status}] {check.Name}{details}");
    }

    Console.WriteLine();
}

var failed = checks.Count(c => !c.Passed);
Console.WriteLine($"Result: {checks.Count - failed}/{checks.Count} checks passed.");
return failed == 0 ? 0 : 1;

static async Task RunPlatformChecksAsync(
    NativeWebViewPlatform platform,
    Action<NativeWebViewBackendFactory> register,
    List<(NativeWebViewPlatform Platform, string Name, bool Passed, string? Details)> checks)
{
    var factory = new NativeWebViewBackendFactory();
    register(factory);

    PrintDiagnostics(factory, platform);

    var createdWebView = factory.TryCreateNativeWebViewBackend(platform, out var webViewBackend);
    checks.Add((platform, "Create webview backend", createdWebView, createdWebView ? null : "fallback backend used"));

    var createdAuth = factory.TryCreateWebAuthenticationBrokerBackend(platform, out var authBackend);
    checks.Add((platform, "Create auth backend", createdAuth, createdAuth ? null : "fallback backend used"));

    var createdDialog = factory.TryCreateNativeWebDialogBackend(platform, out var dialogBackend);
    checks.Add((platform, "Dialog unsupported", !createdDialog, createdDialog ? "unexpected dialog backend registration" : null));

    using (dialogBackend)
    {
        if (!createdDialog)
        {
            try
            {
                dialogBackend.Show();
                checks.Add((platform, "Dialog show guard", false, "expected PlatformNotSupportedException"));
            }
            catch (PlatformNotSupportedException)
            {
                checks.Add((platform, "Dialog show guard", true, null));
            }
            catch (Exception ex)
            {
                checks.Add((platform, "Dialog show guard", false, ex.GetType().Name));
            }
        }
    }

    if (createdWebView)
    {
        using (webViewBackend)
        {
            var environmentRequestedCount = 0;
            var controllerRequestedCount = 0;
            webViewBackend.CoreWebView2EnvironmentRequested += (_, _) => environmentRequestedCount++;
            webViewBackend.CoreWebView2ControllerOptionsRequested += (_, _) => controllerRequestedCount++;

            await ExecuteCheckAsync(platform, "Initialize webview", checks, async () => await webViewBackend.InitializeAsync());
            ExecuteCheck(platform, "Environment options hook", checks, () =>
            {
                if (environmentRequestedCount == 0)
                {
                    throw new InvalidOperationException("Environment options hook was not raised.");
                }
            });
            ExecuteCheck(platform, "Controller options hook", checks, () =>
            {
                if (controllerRequestedCount == 0)
                {
                    throw new InvalidOperationException("Controller options hook was not raised.");
                }
            });
            ExecuteCheck(platform, "WebView platform handle", checks, () =>
            {
                var provider = webViewBackend as INativeWebViewPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebViewPlatformHandleProvider.");
                RequireHandle(provider.TryGetPlatformHandle(out var handle), handle, "platform");
            });
            ExecuteCheck(platform, "WebView view handle", checks, () =>
            {
                var provider = webViewBackend as INativeWebViewPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebViewPlatformHandleProvider.");
                RequireHandle(provider.TryGetViewHandle(out var handle), handle, "view");
            });
            ExecuteCheck(platform, "WebView controller handle", checks, () =>
            {
                var provider = webViewBackend as INativeWebViewPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebViewPlatformHandleProvider.");
                RequireHandle(provider.TryGetControllerHandle(out var handle), handle, "controller");
            });
            ExecuteCheck(platform, "Cookie manager", checks, () =>
            {
                if (!webViewBackend.TryGetCookieManager(out var cookieManager) || cookieManager is null)
                {
                    throw new InvalidOperationException("Cookie manager is unavailable.");
                }
            });
            ExecuteCheck(platform, "Command manager", checks, () =>
            {
                if (!webViewBackend.TryGetCommandManager(out var commandManager) || commandManager is null)
                {
                    throw new InvalidOperationException("Command manager is unavailable.");
                }
            });
            ExecuteCheck(platform, "Navigate", checks, () => webViewBackend.Navigate("https://example.com/mobile-browser"));
            await ExecuteCheckAsync(platform, "Execute script", checks, async () => _ = await webViewBackend.ExecuteScriptAsync("1 + 2"));
            await ExecuteCheckAsync(platform, "Post string message", checks, async () => await webViewBackend.PostWebMessageAsStringAsync("mobile-message"));
            await ExecuteCheckAsync(platform, "Post json message", checks, async () => await webViewBackend.PostWebMessageAsJsonAsync("{\"type\":\"mobile\"}"));
        }
    }

    if (createdAuth)
    {
        using (authBackend)
        {
            await ExecuteCheckAsync(platform, "Authenticate interactive", checks, async () =>
            {
                var callbackUri = new Uri("https://example.com/callback");
                var result = await authBackend.AuthenticateAsync(
                    CreateImmediateSuccessRequestUri(platform, callbackUri),
                    callbackUri,
                    WebAuthenticationOptions.UseTitle);

                if (result.ResponseStatus != WebAuthenticationStatus.Success)
                {
                    throw new InvalidOperationException($"Unexpected auth status: {result.ResponseStatus}");
                }
            });

            await ExecuteCheckAsync(platform, "Authenticate silent", checks, async () =>
            {
                var result = await authBackend.AuthenticateAsync(
                    new Uri("https://example.com/auth"),
                    new Uri("https://example.com/callback"),
                    WebAuthenticationOptions.SilentMode);

                if (result.ResponseStatus != WebAuthenticationStatus.UserCancel)
                {
                    throw new InvalidOperationException($"Unexpected auth status: {result.ResponseStatus}");
                }
            });
        }
    }
}

static void PrintDiagnostics(NativeWebViewBackendFactory factory, NativeWebViewPlatform platform)
{
    var diagnostics = factory.GetPlatformDiagnosticsOrDefault(platform);

    Console.WriteLine($"Diagnostics for {platform} ({diagnostics.ProviderName}, ready={diagnostics.IsReady}):");
    foreach (var issue in diagnostics.Issues)
    {
        var recommendation = string.IsNullOrWhiteSpace(issue.Recommendation)
            ? string.Empty
            : $" Recommendation: {issue.Recommendation}";
        Console.WriteLine($"- [{issue.Severity}] {issue.Code}: {issue.Message}{recommendation}");
    }

    Console.WriteLine();

    if (GetBooleanEnvironmentVariable("NATIVEWEBVIEW_DIAGNOSTICS_REQUIRE_READY"))
    {
        var warningsAsErrors = GetBooleanEnvironmentVariable("NATIVEWEBVIEW_DIAGNOSTICS_WARNINGS_AS_ERRORS");
        NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics, warningsAsErrors);
    }
}

static bool GetBooleanEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
        return false;
    }

    return bool.TryParse(value, out var parsed)
        ? parsed
        : string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}

static void ExecuteCheck(
    NativeWebViewPlatform platform,
    string name,
    List<(NativeWebViewPlatform Platform, string Name, bool Passed, string? Details)> checks,
    Action action)
{
    try
    {
        action();
        checks.Add((platform, name, true, null));
    }
    catch (Exception ex)
    {
        checks.Add((platform, name, false, ex.GetType().Name));
    }
}

static async Task ExecuteCheckAsync(
    NativeWebViewPlatform platform,
    string name,
    List<(NativeWebViewPlatform Platform, string Name, bool Passed, string? Details)> checks,
    Func<Task> action)
{
    try
    {
        await action();
        checks.Add((platform, name, true, null));
    }
    catch (Exception ex)
    {
        checks.Add((platform, name, false, ex.GetType().Name));
    }
}

static void RequireHandle(bool available, NativePlatformHandle handle, string scope)
{
    if (!available)
    {
        throw new InvalidOperationException($"Missing {scope} handle.");
    }

    if (handle.Handle == 0)
    {
        throw new InvalidOperationException($"{scope} handle value is zero.");
    }

    if (string.IsNullOrWhiteSpace(handle.HandleDescriptor))
    {
        throw new InvalidOperationException($"{scope} handle descriptor is empty.");
    }
}

static Uri CreateImmediateSuccessRequestUri(NativeWebViewPlatform platform, Uri callbackUri)
{
    ArgumentNullException.ThrowIfNull(callbackUri);

    var platformName = platform.ToString().ToLowerInvariant();
    return new Uri($"{callbackUri}?platform={platformName}#result=success");
}
