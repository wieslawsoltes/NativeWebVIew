using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsReporterTests
{
    [Fact]
    public void CreateReport_DefaultPolicy_ComputesAggregateAndPerPlatformCounts()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.RegisterPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            static () => CreateDiagnostics(
                NativeWebViewPlatform.Windows,
                "windows-provider",
                [
                    new NativeWebViewDiagnosticIssue("windows.info", NativeWebViewDiagnosticSeverity.Info, "Info"),
                    new NativeWebViewDiagnosticIssue("windows.warning", NativeWebViewDiagnosticSeverity.Warning, "Warning")
                ]));
        factory.RegisterPlatformDiagnostics(
            NativeWebViewPlatform.MacOS,
            static () => CreateDiagnostics(
                NativeWebViewPlatform.MacOS,
                "macos-provider",
                [
                    new NativeWebViewDiagnosticIssue("macos.error", NativeWebViewDiagnosticSeverity.Error, "Error")
                ]));

        var report = NativeWebViewDiagnosticsReporter.CreateReport(
            factory,
            [NativeWebViewPlatform.Windows, NativeWebViewPlatform.MacOS]);

        Assert.False(report.IsReady);
        Assert.Equal(3, report.IssueCount);
        Assert.Equal(1, report.WarningCount);
        Assert.Equal(1, report.ErrorCount);
        Assert.Equal(1, report.BlockingIssueCount);

        var windows = Assert.Single(
            report.Platforms,
            static platform => platform.Platform == NativeWebViewPlatform.Windows);
        Assert.True(windows.ProviderRegistered);
        Assert.True(windows.IsReady);
        Assert.Equal(2, windows.IssueCount);
        Assert.Equal(1, windows.WarningCount);
        Assert.Equal(0, windows.ErrorCount);
        Assert.Equal(0, windows.BlockingIssueCount);

        var macos = Assert.Single(
            report.Platforms,
            static platform => platform.Platform == NativeWebViewPlatform.MacOS);
        Assert.True(macos.ProviderRegistered);
        Assert.False(macos.IsReady);
        Assert.Equal(1, macos.IssueCount);
        Assert.Equal(0, macos.WarningCount);
        Assert.Equal(1, macos.ErrorCount);
        Assert.Equal(1, macos.BlockingIssueCount);
    }

    [Fact]
    public void CreateReport_WarningsAsErrors_ConvertsWarningsToBlockingIssues()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.RegisterPlatformDiagnostics(
            NativeWebViewPlatform.Linux,
            static () => CreateDiagnostics(
                NativeWebViewPlatform.Linux,
                "linux-provider",
                [
                    new NativeWebViewDiagnosticIssue("linux.warning", NativeWebViewDiagnosticSeverity.Warning, "Warning")
                ]));

        var defaultPolicyReport = NativeWebViewDiagnosticsReporter.CreateReport(
            factory,
            [NativeWebViewPlatform.Linux],
            warningsAsErrors: false);
        var strictPolicyReport = NativeWebViewDiagnosticsReporter.CreateReport(
            factory,
            [NativeWebViewPlatform.Linux],
            warningsAsErrors: true);

        Assert.True(defaultPolicyReport.IsReady);
        Assert.Equal(0, defaultPolicyReport.BlockingIssueCount);

        Assert.False(strictPolicyReport.IsReady);
        Assert.Equal(1, strictPolicyReport.BlockingIssueCount);

        var strictLinux = Assert.Single(strictPolicyReport.Platforms);
        Assert.False(strictLinux.IsReady);
        Assert.Equal(1, strictLinux.BlockingIssueCount);
    }

    [Fact]
    public void CreateReport_DeduplicatesPlatformList()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.RegisterPlatformDiagnostics(
            NativeWebViewPlatform.Browser,
            static () => CreateDiagnostics(
                NativeWebViewPlatform.Browser,
                "browser-provider",
                [new NativeWebViewDiagnosticIssue("browser.info", NativeWebViewDiagnosticSeverity.Info, "Info")]));

        var report = NativeWebViewDiagnosticsReporter.CreateReport(
            factory,
            [
                NativeWebViewPlatform.Browser,
                NativeWebViewPlatform.Browser,
                NativeWebViewPlatform.Browser,
            ]);

        Assert.Single(report.Platforms);
    }

    [Fact]
    public void CreateReport_UnregisteredPlatform_IsMarkedAsNotRegisteredAndBlocked()
    {
        var report = NativeWebViewDiagnosticsReporter.CreateReport(
            new NativeWebViewBackendFactory(),
            [NativeWebViewPlatform.Android]);

        var android = Assert.Single(report.Platforms);
        Assert.False(android.ProviderRegistered);
        Assert.False(android.IsReady);
        Assert.Equal(1, android.ErrorCount);
        Assert.Equal(1, android.BlockingIssueCount);

        var issue = Assert.Single(android.Issues);
        Assert.Equal("platform.unregistered", issue.Code);
        Assert.Equal(NativeWebViewDiagnosticSeverity.Error, issue.Severity);
    }

    [Fact]
    public void CreateReport_EmptyPlatformList_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            NativeWebViewDiagnosticsReporter.CreateReport(
                new NativeWebViewBackendFactory(),
                Array.Empty<NativeWebViewPlatform>()));

        Assert.Contains("At least one platform must be provided.", ex.Message, StringComparison.Ordinal);
        Assert.Equal("platforms", ex.ParamName);
    }

    private static NativeWebViewPlatformDiagnostics CreateDiagnostics(
        NativeWebViewPlatform platform,
        string providerName,
        IReadOnlyList<NativeWebViewDiagnosticIssue> issues)
    {
        return new NativeWebViewPlatformDiagnostics(
            platform,
            providerName,
            issues);
    }
}
