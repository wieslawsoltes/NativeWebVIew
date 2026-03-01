using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsValidatorTests
{
    [Fact]
    public void IsReady_DefaultMode_OnlyErrorsBlock()
    {
        var diagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            providerName: "test",
            issues:
            [
                new NativeWebViewDiagnosticIssue("i", NativeWebViewDiagnosticSeverity.Info, "info"),
                new NativeWebViewDiagnosticIssue("w", NativeWebViewDiagnosticSeverity.Warning, "warning"),
            ]);

        Assert.True(NativeWebViewDiagnosticsValidator.IsReady(diagnostics));
        NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics);
    }

    [Fact]
    public void IsReady_WarningsAsErrors_WarningsBlock()
    {
        var diagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            providerName: "test",
            issues:
            [
                new NativeWebViewDiagnosticIssue("w", NativeWebViewDiagnosticSeverity.Warning, "warning")
            ]);

        Assert.False(NativeWebViewDiagnosticsValidator.IsReady(diagnostics, warningsAsErrors: true));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics, warningsAsErrors: true));
        Assert.Contains("warningsAsErrors=True", ex.Message, StringComparison.Ordinal);
        Assert.Contains("w: warning", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureReady_ErrorsAlwaysBlock()
    {
        var diagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Linux,
            providerName: "test",
            issues:
            [
                new NativeWebViewDiagnosticIssue(
                    "linux.webkitgtk.version.too_low",
                    NativeWebViewDiagnosticSeverity.Error,
                    "Version is too low.",
                    recommendation: "Upgrade runtime.")
            ]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            NativeWebViewDiagnosticsValidator.EnsureReady(diagnostics));
        Assert.Contains("linux.webkitgtk.version.too_low", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Upgrade runtime.", ex.Message, StringComparison.Ordinal);
    }
}
