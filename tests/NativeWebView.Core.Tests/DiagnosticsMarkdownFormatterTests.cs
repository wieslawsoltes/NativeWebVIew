using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsMarkdownFormatterTests
{
    [Fact]
    public void FormatReport_IncludesSummaryTableAndIssueSections()
    {
        var report = CreateReport();

        var markdown = NativeWebViewDiagnosticsMarkdownFormatter.FormatReport(report);

        Assert.Contains("## Platform Diagnostics Summary", markdown, StringComparison.Ordinal);
        Assert.Contains("| Platform | Ready | Provider | Registered | Warnings | Errors | Blocking |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Windows | True | windows-provider | True | 1 | 0 | 0 |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Linux | False | linux-provider | True | 0 | 1 | 1 |", markdown, StringComparison.Ordinal);
        Assert.Contains("### Windows", markdown, StringComparison.Ordinal);
        Assert.Contains("### Linux", markdown, StringComparison.Ordinal);
        Assert.Contains("[Warning] `windows.warning`", markdown, StringComparison.Ordinal);
        Assert.Contains("[Error] `linux.error`", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatReport_EscapesPipeCharactersInTableCells()
    {
        var diagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            providerName: "provider|name",
            issues:
            [
                new NativeWebViewDiagnosticIssue(
                    code: "code.with|pipe",
                    severity: NativeWebViewDiagnosticSeverity.Warning,
                    message: "Message with | pipe")
            ]);
        var entry = new NativeWebViewPlatformDiagnosticsReportEntry(diagnostics, providerRegistered: true, warningsAsErrors: false);
        var report = new NativeWebViewDiagnosticsReport(DateTimeOffset.UtcNow, warningsAsErrors: false, [entry]);

        var markdown = NativeWebViewDiagnosticsMarkdownFormatter.FormatReport(report, title: "Custom|Title");

        Assert.Contains("## Custom|Title", markdown, StringComparison.Ordinal);
        Assert.Contains("| Windows | True | provider\\|name | True | 1 | 0 | 0 |", markdown, StringComparison.Ordinal);
        Assert.Contains("`code.with|pipe`", markdown, StringComparison.Ordinal);
        Assert.Contains("Message with | pipe", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatReport_SanitizesTitleAndCodeLineBreaks()
    {
        var diagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Browser,
            providerName: "browser-provider",
            issues:
            [
                new NativeWebViewDiagnosticIssue(
                    code: "line1\nline`2",
                    severity: NativeWebViewDiagnosticSeverity.Warning,
                    message: "Message")
            ]);
        var entry = new NativeWebViewPlatformDiagnosticsReportEntry(diagnostics, providerRegistered: true, warningsAsErrors: false);
        var report = new NativeWebViewDiagnosticsReport(DateTimeOffset.UtcNow, warningsAsErrors: false, [entry]);

        var markdown = NativeWebViewDiagnosticsMarkdownFormatter.FormatReport(report, title: "A title\nwith line break");

        Assert.Contains("## A title with line break", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("## A title\nwith line break", markdown, StringComparison.Ordinal);
        Assert.Contains("`line1 line'2`", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatReport_NullReport_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NativeWebViewDiagnosticsMarkdownFormatter.FormatReport(report: null!));
    }

    private static NativeWebViewDiagnosticsReport CreateReport()
    {
        var windowsDiagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            providerName: "windows-provider",
            issues:
            [
                new NativeWebViewDiagnosticIssue(
                    code: "windows.warning",
                    severity: NativeWebViewDiagnosticSeverity.Warning,
                    message: "Windows warning")
            ]);
        var linuxDiagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Linux,
            providerName: "linux-provider",
            issues:
            [
                new NativeWebViewDiagnosticIssue(
                    code: "linux.error",
                    severity: NativeWebViewDiagnosticSeverity.Error,
                    message: "Linux error",
                    recommendation: "Fix Linux setup")
            ]);

        var windowsEntry = new NativeWebViewPlatformDiagnosticsReportEntry(
            windowsDiagnostics,
            providerRegistered: true,
            warningsAsErrors: false);
        var linuxEntry = new NativeWebViewPlatformDiagnosticsReportEntry(
            linuxDiagnostics,
            providerRegistered: true,
            warningsAsErrors: false);

        return new NativeWebViewDiagnosticsReport(
            DateTimeOffset.UtcNow,
            warningsAsErrors: false,
            [windowsEntry, linuxEntry]);
    }
}
