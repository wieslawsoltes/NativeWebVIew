using System.Diagnostics;
using System.Runtime.CompilerServices;
using NativeWebView.Core;
using NativeWebView.Platform.Windows;

namespace NativeWebView.Core.Tests;

public sealed class Phase5HardeningTests
{
    private const int NavigationStressIterations = 2_000;
    private const int ControllerLoopIterations = 500;
    private const long LocalNavigationStressThresholdMs = 5_000;
    private const long LocalControllerLoopThresholdMs = 6_000;
    private const long CiNavigationStressThresholdMs = 15_000;
    private const long CiControllerLoopThresholdMs = 18_000;

    [Fact]
    public void NativeWebViewController_DisposeRecreateLoop_DoesNotLeak()
    {
        var references = new List<WeakReference>();
        for (var i = 0; i < 64; i++)
        {
            references.Add(CreateDisposedWebViewControllerReference());
        }

        ForceFullGc();
        Assert.All(references, static reference => Assert.False(reference.IsAlive));
    }

    [Fact]
    public void NativeWebDialogController_DisposeRecreateLoop_DoesNotLeak()
    {
        var references = new List<WeakReference>();
        for (var i = 0; i < 64; i++)
        {
            references.Add(CreateDisposedDialogControllerReference());
        }

        ForceFullGc();
        Assert.All(references, static reference => Assert.False(reference.IsAlive));
    }

    [Fact]
    public void WebAuthenticationBrokerController_DisposeRecreateLoop_DoesNotLeak()
    {
        var references = new List<WeakReference>();
        for (var i = 0; i < 64; i++)
        {
            references.Add(CreateDisposedAuthControllerReference());
        }

        ForceFullGc();
        Assert.All(references, static reference => Assert.False(reference.IsAlive));
    }

    [Fact]
    public async Task WindowsWebViewBackend_NavigationStress_CompletesWithinBaseline()
    {
        using var backend = new WindowsNativeWebViewBackend();
        await backend.InitializeAsync();

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < NavigationStressIterations; i++)
        {
            backend.Navigate($"https://example.com/stress/{i}");
            _ = await backend.ExecuteScriptAsync("1 + 1");
        }

        stopwatch.Stop();
        var threshold = GetThreshold(
            localThresholdMs: LocalNavigationStressThresholdMs,
            ciThresholdMs: CiNavigationStressThresholdMs);
        Assert.True(
            stopwatch.ElapsedMilliseconds < threshold,
            $"Navigation stress exceeded baseline: {stopwatch.ElapsedMilliseconds} ms (threshold: {threshold} ms) for {NavigationStressIterations} iterations.");
    }

    [Fact]
    public async Task WebViewController_CreateInitializeDisposeLoop_CompletesWithinBaseline()
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < ControllerLoopIterations; i++)
        {
            using var backend = new WindowsNativeWebViewBackend();
            using var controller = new NativeWebViewController(backend);
            await controller.InitializeAsync();
            controller.Navigate($"https://example.com/perf/{i}");
        }

        stopwatch.Stop();
        var threshold = GetThreshold(
            localThresholdMs: LocalControllerLoopThresholdMs,
            ciThresholdMs: CiControllerLoopThresholdMs);
        Assert.True(
            stopwatch.ElapsedMilliseconds < threshold,
            $"Create/initialize/dispose loop exceeded baseline: {stopwatch.ElapsedMilliseconds} ms (threshold: {threshold} ms) for {ControllerLoopIterations} iterations.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedWebViewControllerReference()
    {
        var backend = new WindowsNativeWebViewBackend();
        var controller = new NativeWebViewController(backend);
        controller.InitializeAsync().AsTask().GetAwaiter().GetResult();
        controller.Navigate("https://example.com/leak-webview");
        controller.Dispose();
        return new WeakReference(controller);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedDialogControllerReference()
    {
        var backend = new WindowsNativeWebDialogBackend();
        var controller = new NativeWebDialogController(backend);
        controller.Show(new NativeWebDialogShowOptions { Title = "Leak Dialog" });
        controller.Navigate("https://example.com/leak-dialog");
        controller.Close();
        controller.Dispose();
        return new WeakReference(controller);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedAuthControllerReference()
    {
        var backend = new WindowsWebAuthenticationBrokerBackend();
        var controller = new WebAuthenticationBrokerController(backend);
        controller.Dispose();
        return new WeakReference(controller);
    }

    private static void ForceFullGc()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static long GetThreshold(long localThresholdMs, long ciThresholdMs)
    {
        return IsCiEnvironment() ? ciThresholdMs : localThresholdMs;
    }

    private static bool IsCiEnvironment()
    {
        if (IsTruthyEnvironmentVariable("CI"))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TF_BUILD")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JENKINS_URL")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APPVEYOR"));
    }

    private static bool IsTruthyEnvironmentVariable(string name)
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
}
