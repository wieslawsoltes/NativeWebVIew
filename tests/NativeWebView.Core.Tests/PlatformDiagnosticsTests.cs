using NativeWebView.Core;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.Linux;
using NativeWebView.Platform.Windows;
using NativeWebView.Platform.iOS;
using NativeWebView.Platform.macOS;

namespace NativeWebView.Core.Tests;

public sealed class PlatformDiagnosticsTests
{
    [Fact]
    public void RegisteredPlatforms_ExposeDiagnosticsProviders()
    {
        var platforms = new (NativeWebViewPlatform Platform, Action<NativeWebViewBackendFactory> Register)[]
        {
            (NativeWebViewPlatform.Windows, static factory => factory.UseNativeWebViewWindows()),
            (NativeWebViewPlatform.MacOS, static factory => factory.UseNativeWebViewMacOS()),
            (NativeWebViewPlatform.Linux, static factory => factory.UseNativeWebViewLinux()),
            (NativeWebViewPlatform.IOS, static factory => factory.UseNativeWebViewIOS()),
            (NativeWebViewPlatform.Android, static factory => factory.UseNativeWebViewAndroid()),
            (NativeWebViewPlatform.Browser, static factory => factory.UseNativeWebViewBrowser()),
        };

        foreach (var (platform, register) in platforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryGetPlatformDiagnostics(platform, out var diagnostics));
            Assert.Equal(platform, diagnostics.Platform);
            Assert.False(string.IsNullOrWhiteSpace(diagnostics.ProviderName));
            Assert.NotEmpty(diagnostics.Issues);
        }
    }

    [Fact]
    public void UnregisteredPlatform_ReturnsErrorDiagnostics()
    {
        var factory = new NativeWebViewBackendFactory();

        var found = factory.TryGetPlatformDiagnostics(NativeWebViewPlatform.Windows, out var diagnostics);

        Assert.False(found);
        Assert.Equal(NativeWebViewPlatform.Windows, diagnostics.Platform);
        var issue = Assert.Single(diagnostics.Issues);
        Assert.Equal("platform.unregistered", issue.Code);
        Assert.Equal(NativeWebViewDiagnosticSeverity.Error, issue.Severity);
    }

    [Fact]
    public void GetPlatformDiagnosticsOrDefault_Unregistered_ReturnsBlockingDiagnostics()
    {
        var factory = new NativeWebViewBackendFactory();

        var diagnostics = factory.GetPlatformDiagnosticsOrDefault(NativeWebViewPlatform.Windows);

        Assert.Equal(NativeWebViewPlatform.Windows, diagnostics.Platform);
        Assert.False(diagnostics.IsReady);
        var issue = Assert.Single(diagnostics.Issues);
        Assert.Equal("platform.unregistered", issue.Code);
    }

    [Fact]
    public void ThrowingProvider_ReturnsProviderFailureDiagnostic()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.RegisterPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            static () => throw new InvalidOperationException("boom"));

        var found = factory.TryGetPlatformDiagnostics(NativeWebViewPlatform.Windows, out var diagnostics);

        Assert.True(found);
        Assert.Equal(NativeWebViewPlatform.Windows, diagnostics.Platform);
        var issue = Assert.Single(diagnostics.Issues);
        Assert.Equal("diagnostics.provider.failure", issue.Code);
        Assert.Equal(NativeWebViewDiagnosticSeverity.Error, issue.Severity);
        Assert.Equal("boom", issue.Recommendation);
    }

    [Fact]
    public void Runtime_CurrentPlatformDiagnostics_AreAvailable()
    {
        var diagnostics = NativeWebViewRuntime.GetCurrentPlatformDiagnostics();

        Assert.Equal(NativeWebViewRuntime.CurrentPlatform, diagnostics.Platform);
        Assert.NotEmpty(diagnostics.Issues);
    }

    [Fact]
    public void NoImplementedEmbeddedPlatforms_ReportContractOnlyWarnings()
    {
        var platforms = new (NativeWebViewPlatform Platform, Action<NativeWebViewBackendFactory> Register, string IssueCode)[]
        {
        };

        foreach (var (platform, register, issueCode) in platforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryGetPlatformDiagnostics(platform, out var diagnostics));
            Assert.Contains(diagnostics.Issues, issue => issue.Code == issueCode);
        }
    }

    [Fact]
    public void EmbeddedControlContractOnlyWarning_MatchesImplementationStatus()
    {
        var platforms = new (NativeWebViewPlatform Platform, Action<NativeWebViewBackendFactory> Register, string IssueCode)[]
        {
            (NativeWebViewPlatform.Windows, static factory => factory.UseNativeWebViewWindows(), "windows.control.contract_only"),
            (NativeWebViewPlatform.Linux, static factory => factory.UseNativeWebViewLinux(), "linux.control.contract_only"),
            (NativeWebViewPlatform.MacOS, static factory => factory.UseNativeWebViewMacOS(), "macos.control.contract_only"),
            (NativeWebViewPlatform.IOS, static factory => factory.UseNativeWebViewIOS(), "ios.control.contract_only"),
            (NativeWebViewPlatform.Android, static factory => factory.UseNativeWebViewAndroid(), "android.control.contract_only"),
            (NativeWebViewPlatform.Browser, static factory => factory.UseNativeWebViewBrowser(), "browser.control.contract_only"),
        };

        foreach (var (platform, register, issueCode) in platforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryGetPlatformDiagnostics(platform, out var diagnostics));
            var shouldWarn = NativeWebViewPlatformImplementationStatusMatrix.Get(platform).EmbeddedControl !=
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented;

            if (shouldWarn)
            {
                Assert.Contains(diagnostics.Issues, issue => issue.Code == issueCode);
            }
            else
            {
                Assert.DoesNotContain(diagnostics.Issues, issue => issue.Code == issueCode);
            }
        }
    }
}
