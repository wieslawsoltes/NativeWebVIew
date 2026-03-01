using NativeWebView.Core;
using NativeWebView.Interop;
using NativeWebView.Platform.Linux;
using NativeWebView.Platform.Windows;
using NativeWebView.Platform.macOS;

namespace NativeWebView.Sample.Desktop;

internal static class DesktopSampleSmokeRunner
{
    public static async Task<int> RunAsync()
    {
        var platform = NativeWebViewRuntime.CurrentPlatform;

        if (platform is not (NativeWebViewPlatform.Windows or NativeWebViewPlatform.MacOS or NativeWebViewPlatform.Linux))
        {
            Console.Error.WriteLine($"Desktop sample supports Windows/macOS/Linux. Current platform: {platform}.");
            return 2;
        }

        var factory = new NativeWebViewBackendFactory();
        RegisterDesktop(factory, platform);
        PrintDiagnostics(factory, platform);

        var checks = new List<(string Name, bool Passed, string? Details)>();

        await RunWebViewChecksAsync(factory, platform, checks);
        await RunDialogChecksAsync(factory, platform, checks);
        await RunAuthChecksAsync(factory, platform, checks);

        var failedCount = checks.Count(static c => !c.Passed);

        Console.WriteLine($"Desktop backend matrix for {platform}:");
        foreach (var check in checks)
        {
            var status = check.Passed ? "PASS" : "FAIL";
            var details = string.IsNullOrWhiteSpace(check.Details) ? string.Empty : $" ({check.Details})";
            Console.WriteLine($"[{status}] {check.Name}{details}");
        }

        Console.WriteLine($"Result: {checks.Count - failedCount}/{checks.Count} checks passed.");
        return failedCount == 0 ? 0 : 1;
    }

    private static void RegisterDesktop(NativeWebViewBackendFactory factory, NativeWebViewPlatform platform)
    {
        switch (platform)
        {
            case NativeWebViewPlatform.Windows:
                factory.UseNativeWebViewWindows();
                break;
            case NativeWebViewPlatform.MacOS:
                factory.UseNativeWebViewMacOS();
                break;
            case NativeWebViewPlatform.Linux:
                factory.UseNativeWebViewLinux();
                break;
        }
    }

    private static void PrintDiagnostics(NativeWebViewBackendFactory factory, NativeWebViewPlatform platform)
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

    private static bool GetBooleanEnvironmentVariable(string name)
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

    private static async Task RunWebViewChecksAsync(
        NativeWebViewBackendFactory factory,
        NativeWebViewPlatform platform,
        List<(string Name, bool Passed, string? Details)> checks)
    {
        var created = factory.TryCreateNativeWebViewBackend(platform, out var backend);
        checks.Add(("Create webview backend", created, created ? null : "fallback backend used"));

        if (!created)
        {
            return;
        }

        using (backend)
        {
            var environmentRequestedCount = 0;
            var controllerRequestedCount = 0;
            backend.CoreWebView2EnvironmentRequested += (_, _) => environmentRequestedCount++;
            backend.CoreWebView2ControllerOptionsRequested += (_, _) => controllerRequestedCount++;

            await ExecuteCheckAsync("Initialize webview", checks, async () =>
            {
                await backend.InitializeAsync();
            });

            ExecuteCheck("Environment options hook", checks, () =>
            {
                if (environmentRequestedCount == 0)
                {
                    throw new InvalidOperationException("Environment options hook was not raised.");
                }
            });

            ExecuteCheck("Controller options hook", checks, () =>
            {
                if (controllerRequestedCount == 0)
                {
                    throw new InvalidOperationException("Controller options hook was not raised.");
                }
            });

            ExecuteCheck("WebView platform handle", checks, () =>
            {
                var provider = backend as INativeWebViewPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebViewPlatformHandleProvider.");
                RequireHandle(provider.TryGetPlatformHandle(out var handle), handle, "platform");
            });

            ExecuteCheck("WebView view handle", checks, () =>
            {
                var provider = backend as INativeWebViewPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebViewPlatformHandleProvider.");
                RequireHandle(provider.TryGetViewHandle(out var handle), handle, "view");
            });

            ExecuteCheck("WebView controller handle", checks, () =>
            {
                var provider = backend as INativeWebViewPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebViewPlatformHandleProvider.");
                RequireHandle(provider.TryGetControllerHandle(out var handle), handle, "controller");
            });

            ExecuteCheck("Cookie manager", checks, () =>
            {
                if (!backend.TryGetCookieManager(out var cookieManager) || cookieManager is null)
                {
                    throw new InvalidOperationException("Cookie manager is unavailable.");
                }
            });

            ExecuteCheck("Command manager", checks, () =>
            {
                if (!backend.TryGetCommandManager(out var commandManager) || commandManager is null)
                {
                    throw new InvalidOperationException("Command manager is unavailable.");
                }
            });

            ExecuteCheck("Navigate", checks, () =>
            {
                backend.Navigate("https://example.com/");
            });

            await ExecuteCheckAsync("Execute script", checks, async () =>
            {
                _ = await backend.ExecuteScriptAsync("1 + 1");
            });

            await ExecuteCheckAsync("Post web message", checks, async () =>
            {
                await backend.PostWebMessageAsStringAsync("phase2-matrix");
            });

            await ExecuteCheckAsync("Print", checks, async () =>
            {
                _ = await backend.PrintAsync(new NativeWebViewPrintSettings { OutputPath = "matrix.pdf" });
            });

            await ExecuteCheckAsync("Show print UI", checks, async () =>
            {
                _ = await backend.ShowPrintUiAsync();
            });

            if (backend.Features.Supports(NativeWebViewFeature.DevTools))
            {
                ExecuteCheck("Open devtools", checks, backend.OpenDevToolsWindow);
            }
            else
            {
                checks.Add(("Open devtools", true, "not supported on this backend"));
            }
        }
    }

    private static async Task RunDialogChecksAsync(
        NativeWebViewBackendFactory factory,
        NativeWebViewPlatform platform,
        List<(string Name, bool Passed, string? Details)> checks)
    {
        var created = factory.TryCreateNativeWebDialogBackend(platform, out var backend);
        checks.Add(("Create dialog backend", created, created ? null : "dialog backend not registered"));

        if (!created)
        {
            return;
        }

        using (backend)
        {
            ExecuteCheck("Dialog platform handle", checks, () =>
            {
                var provider = backend as INativeWebDialogPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebDialogPlatformHandleProvider.");
                RequireHandle(provider.TryGetPlatformHandle(out var handle), handle, "platform");
            });

            ExecuteCheck("Dialog view handle", checks, () =>
            {
                var provider = backend as INativeWebDialogPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebDialogPlatformHandleProvider.");
                RequireHandle(provider.TryGetDialogHandle(out var handle), handle, "dialog");
            });

            ExecuteCheck("Dialog host handle", checks, () =>
            {
                var provider = backend as INativeWebDialogPlatformHandleProvider
                    ?? throw new InvalidOperationException("Missing INativeWebDialogPlatformHandleProvider.");
                RequireHandle(provider.TryGetHostWindowHandle(out var handle), handle, "host");
            });

            ExecuteCheck("Show dialog", checks, () => backend.Show(new NativeWebDialogShowOptions { Title = "Matrix" }));
            ExecuteCheck("Dialog navigate", checks, () => backend.Navigate("https://example.com/dialog"));

            await ExecuteCheckAsync("Dialog execute script", checks, async () =>
            {
                _ = await backend.ExecuteScriptAsync("window.location.href");
            });

            await ExecuteCheckAsync("Dialog post message", checks, async () =>
            {
                await backend.PostWebMessageAsJsonAsync("{\"message\":\"phase2\"}");
            });

            if (backend.Features.Supports(NativeWebViewFeature.DevTools))
            {
                ExecuteCheck("Dialog open devtools", checks, backend.OpenDevToolsWindow);
            }
            else
            {
                checks.Add(("Dialog open devtools", true, "not supported on this backend"));
            }

            ExecuteCheck("Close dialog", checks, backend.Close);
        }
    }

    private static async Task RunAuthChecksAsync(
        NativeWebViewBackendFactory factory,
        NativeWebViewPlatform platform,
        List<(string Name, bool Passed, string? Details)> checks)
    {
        var created = factory.TryCreateWebAuthenticationBrokerBackend(platform, out var backend);
        checks.Add(("Create auth backend", created, created ? null : "auth backend not registered"));

        if (!created)
        {
            return;
        }

        var request = new Uri("https://example.com/auth");
        var callback = new Uri("https://example.com/callback");

        await ExecuteCheckAsync("Authenticate", checks, async () =>
        {
            var result = await backend.AuthenticateAsync(request, callback);

            if (result.ResponseStatus is not (WebAuthenticationStatus.Success or WebAuthenticationStatus.UserCancel))
            {
                throw new InvalidOperationException($"Unexpected auth status: {result.ResponseStatus}");
            }
        });

        backend.Dispose();
    }

    private static void ExecuteCheck(string name, List<(string Name, bool Passed, string? Details)> checks, Action action)
    {
        try
        {
            action();
            checks.Add((name, true, null));
        }
        catch (Exception ex)
        {
            checks.Add((name, false, ex.GetType().Name));
        }
    }

    private static async Task ExecuteCheckAsync(string name, List<(string Name, bool Passed, string? Details)> checks, Func<Task> action)
    {
        try
        {
            await action();
            checks.Add((name, true, null));
        }
        catch (Exception ex)
        {
            checks.Add((name, false, ex.GetType().Name));
        }
    }

    private static void RequireHandle(bool available, NativePlatformHandle handle, string scope)
    {
        if (!available)
        {
            throw new InvalidOperationException($"{scope} handle was not available.");
        }

        if (handle.Handle == nint.Zero || string.IsNullOrWhiteSpace(handle.HandleDescriptor))
        {
            throw new InvalidOperationException($"{scope} handle was invalid.");
        }
    }
}
