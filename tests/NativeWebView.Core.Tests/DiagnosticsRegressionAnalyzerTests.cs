using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsRegressionAnalyzerTests
{
    [Fact]
    public void GetBlockingIssues_DefaultPolicy_ReturnsErrorIssuesOnly()
    {
        var report = CreateReport(
            warningsAsErrors: false,
            (NativeWebViewPlatform.Windows, "windows.warning", NativeWebViewDiagnosticSeverity.Warning),
            (NativeWebViewPlatform.Windows, "windows.error", NativeWebViewDiagnosticSeverity.Error),
            (NativeWebViewPlatform.Linux, "linux.info", NativeWebViewDiagnosticSeverity.Info));

        var blocking = NativeWebViewDiagnosticsRegressionAnalyzer.GetBlockingIssues(report);

        var issue = Assert.Single(blocking);
        Assert.Equal(NativeWebViewPlatform.Windows, issue.Platform);
        Assert.Equal("windows.error", issue.Code);
    }

    [Fact]
    public void GetBlockingIssues_WarningsAsErrors_IncludesWarnings()
    {
        var report = CreateReport(
            warningsAsErrors: false,
            (NativeWebViewPlatform.Android, "android.warning", NativeWebViewDiagnosticSeverity.Warning));

        var blocking = NativeWebViewDiagnosticsRegressionAnalyzer.GetBlockingIssues(
            report,
            warningsAsErrors: true);

        var issue = Assert.Single(blocking);
        Assert.Equal(NativeWebViewPlatform.Android, issue.Platform);
        Assert.Equal("android.warning", issue.Code);
    }

    [Fact]
    public void CompareBlockingIssues_DetectsNewAndResolved()
    {
        var baseline = new[]
        {
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "a"),
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "b"),
        };
        var current = new[]
        {
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "a"),
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Android, "c"),
        };

        var result = NativeWebViewDiagnosticsRegressionAnalyzer.CompareBlockingIssues(baseline, current);

        Assert.True(result.HasRegression);
        Assert.True(result.HasStaleBaseline);
        Assert.True(result.RequiresBaselineUpdate);
        var added = Assert.Single(result.NewBlockingIssues);
        Assert.Equal(NativeWebViewPlatform.Android, added.Platform);
        Assert.Equal("c", added.Code);

        var resolved = Assert.Single(result.ResolvedBlockingIssues);
        Assert.Equal(NativeWebViewPlatform.Linux, resolved.Platform);
        Assert.Equal("b", resolved.Code);
    }

    [Fact]
    public void CompareBlockingIssues_MatchingSets_DoesNotRequireBaselineUpdate()
    {
        var baseline = new[]
        {
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "a"),
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "b"),
        };
        var current = new[]
        {
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "a"),
            new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "b"),
        };

        var result = NativeWebViewDiagnosticsRegressionAnalyzer.CompareBlockingIssues(baseline, current);

        Assert.False(result.HasRegression);
        Assert.False(result.HasStaleBaseline);
        Assert.False(result.RequiresBaselineUpdate);
        Assert.Empty(result.NewBlockingIssues);
        Assert.Empty(result.ResolvedBlockingIssues);
    }

    [Fact]
    public void ParseBaselineLines_SkipsCommentsAndWhitespace()
    {
        var parsed = NativeWebViewDiagnosticsRegressionAnalyzer.ParseBaselineLines(
            [
                "# comment",
                "  ",
                "Windows|windows.error",
                "linux|linux.error",
            ]);

        Assert.Equal(2, parsed.Count);
        Assert.Contains(parsed, i => i.Platform == NativeWebViewPlatform.Windows && i.Code == "windows.error");
        Assert.Contains(parsed, i => i.Platform == NativeWebViewPlatform.Linux && i.Code == "linux.error");
    }

    [Fact]
    public void ParseBaselineLines_InvalidLine_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() =>
            NativeWebViewDiagnosticsRegressionAnalyzer.ParseBaselineLines(["invalid"]));

        Assert.Contains("Expected '<Platform>|<Code>'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeBaselineLines_WritesSortedLines()
    {
        var text = NativeWebViewDiagnosticsRegressionAnalyzer.SerializeBaselineLines(
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Browser, "b"),
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Android, "a"),
            ]);

        Assert.Contains("Android|a", text, StringComparison.Ordinal);
        Assert.Contains("Browser|b", text, StringComparison.Ordinal);
        Assert.True(text.IndexOf("Android|a", StringComparison.Ordinal) < text.IndexOf("Browser|b", StringComparison.Ordinal));
    }

    private static NativeWebViewDiagnosticsReport CreateReport(
        bool warningsAsErrors,
        params (NativeWebViewPlatform Platform, string Code, NativeWebViewDiagnosticSeverity Severity)[] issues)
    {
        var grouped = issues
            .GroupBy(static issue => issue.Platform)
            .Select(group =>
            {
                var diagnostics = new NativeWebViewPlatformDiagnostics(
                    group.Key,
                    providerName: $"{group.Key}-provider",
                    group.Select(issue => new NativeWebViewDiagnosticIssue(issue.Code, issue.Severity, issue.Code)).ToArray());
                return new NativeWebViewPlatformDiagnosticsReportEntry(diagnostics, providerRegistered: true, warningsAsErrors);
            })
            .ToArray();

        return new NativeWebViewDiagnosticsReport(DateTimeOffset.UtcNow, warningsAsErrors, grouped);
    }
}
