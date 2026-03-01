using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsRegressionMarkdownFormatterTests
{
    [Fact]
    public void Format_IncludesCountsAndIssueLists()
    {
        var result = new NativeWebViewDiagnosticsRegressionResult(
            baselineBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "w.error")
            ],
            currentBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "w.error"),
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Android, "a.error")
            ],
            newBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Android, "a.error")
            ],
            resolvedBlockingIssues: []);

        var markdown = NativeWebViewDiagnosticsRegressionMarkdownFormatter.Format(result);

        Assert.Contains("## Blocking Diagnostics Regression Comparison", markdown, StringComparison.Ordinal);
        Assert.Contains("Baseline Blocking Issues: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Current Blocking Issues: 2", markdown, StringComparison.Ordinal);
        Assert.Contains("New Blocking Issues: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Resolved Blocking Issues: 0", markdown, StringComparison.Ordinal);
        Assert.Contains("Has Regression: True", markdown, StringComparison.Ordinal);
        Assert.Contains("Has Stale Baseline: False", markdown, StringComparison.Ordinal);
        Assert.Contains("Requires Baseline Update: True", markdown, StringComparison.Ordinal);
        Assert.Contains("`Android|a.error`", markdown, StringComparison.Ordinal);
        Assert.Contains("- None", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_NullResult_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NativeWebViewDiagnosticsRegressionMarkdownFormatter.Format(result: null!));
    }

    [Fact]
    public void Format_SanitizesTitleAndIssueCodeLineBreaks()
    {
        var result = new NativeWebViewDiagnosticsRegressionResult(
            baselineBlockingIssues: [],
            currentBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Android, "line1\nline`2")
            ],
            newBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Android, "line1\nline`2")
            ],
            resolvedBlockingIssues: []);

        var markdown = NativeWebViewDiagnosticsRegressionMarkdownFormatter.Format(
            result,
            title: "A title\nwith line break");

        Assert.Contains("## A title with line break", markdown, StringComparison.Ordinal);
        Assert.Contains("`Android|line1 line'2`", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ResolvedOnly_ReportsStaleBaseline()
    {
        var result = new NativeWebViewDiagnosticsRegressionResult(
            baselineBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "linux.error")
            ],
            currentBlockingIssues: [],
            newBlockingIssues: [],
            resolvedBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "linux.error")
            ]);

        var markdown = NativeWebViewDiagnosticsRegressionMarkdownFormatter.Format(result);

        Assert.Contains("Has Regression: False", markdown, StringComparison.Ordinal);
        Assert.Contains("Has Stale Baseline: True", markdown, StringComparison.Ordinal);
        Assert.Contains("Requires Baseline Update: True", markdown, StringComparison.Ordinal);
        Assert.Contains("`Linux|linux.error`", markdown, StringComparison.Ordinal);
    }
}
