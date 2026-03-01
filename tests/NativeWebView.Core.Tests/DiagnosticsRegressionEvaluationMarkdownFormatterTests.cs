using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsRegressionEvaluationMarkdownFormatterTests
{
    [Fact]
    public void Format_NoFailingGates_IncludesSummaryAndNoneGateList()
    {
        var report = CreateReport(isReady: true, warningsAsErrors: false);
        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison: null,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: false);

        var markdown = NativeWebViewDiagnosticsRegressionEvaluationMarkdownFormatter.Format(evaluation);

        Assert.Contains("## Blocking Diagnostics Gate Evaluation", markdown, StringComparison.Ordinal);
        Assert.Contains("Require Ready: True", markdown, StringComparison.Ordinal);
        Assert.Contains("Is Ready: True", markdown, StringComparison.Ordinal);
        Assert.Contains("Effective Exit Code: 0", markdown, StringComparison.Ordinal);
        Assert.Contains("Fingerprint Version: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Fingerprint: ", markdown, StringComparison.Ordinal);
        Assert.Contains("Primary Failing Gate: None", markdown, StringComparison.Ordinal);
        Assert.Contains("### Failing Gates", markdown, StringComparison.Ordinal);
        Assert.Contains("- None", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_MultipleFailingGates_IncludesGateDescriptionsAndComparisonSnapshot()
    {
        var report = CreateReport(isReady: false, warningsAsErrors: false);
        var comparison = new NativeWebViewDiagnosticsRegressionResult(
            baselineBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "linux.error")
            ],
            currentBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "windows.error")
            ],
            newBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "windows.error")
            ],
            resolvedBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Linux, "linux.error")
            ]);
        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: true);

        var markdown = NativeWebViewDiagnosticsRegressionEvaluationMarkdownFormatter.Format(evaluation);

        Assert.Contains("Effective Exit Code: 13", markdown, StringComparison.Ordinal);
        Assert.Contains("Fingerprint: ", markdown, StringComparison.Ordinal);
        Assert.Contains("Has Multiple Gate Failures: True", markdown, StringComparison.Ordinal);
        Assert.Contains("Primary Failing Gate: None", markdown, StringComparison.Ordinal);
        Assert.Contains("### Comparison Snapshot", markdown, StringComparison.Ordinal);
        Assert.Contains("Baseline Blocking Issues: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Current Blocking Issues: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("New Blocking Issues: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Resolved Blocking Issues: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Fingerprint Version: 1", markdown, StringComparison.Ordinal);
        Assert.Contains("- `RequireReady` (10): Blocking diagnostics issues were found and --require-ready is enabled. Recommendation: Fix blocking diagnostics issues or run with --allow-not-ready when collecting non-gating reports.", markdown, StringComparison.Ordinal);
        Assert.Contains("- `Regression` (11): Blocking diagnostics regressions were found and regression gating is enabled. Recommendation: Resolve newly introduced blocking issues or run with --allow-regression when triaging intentional changes.", markdown, StringComparison.Ordinal);
        Assert.Contains("- `BaselineSync` (12): Blocking diagnostics baseline contains resolved entries and --require-baseline-sync is enabled. Recommendation: Refresh baseline using ./scripts/update-blocking-baseline.sh when resolved entries are intentional.", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_CustomTitle_SanitizesLineBreaks()
    {
        var report = CreateReport(isReady: false, warningsAsErrors: false);
        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison: null,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: false);

        var markdown = NativeWebViewDiagnosticsRegressionEvaluationMarkdownFormatter.Format(
            evaluation,
            title: "Gate evaluation\nsummary");

        Assert.Contains("## Gate evaluation summary", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_NullEvaluation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NativeWebViewDiagnosticsRegressionEvaluationMarkdownFormatter.Format(evaluation: null!));
    }

    private static NativeWebViewDiagnosticsReport CreateReport(bool isReady, bool warningsAsErrors)
    {
        var issueSeverity = isReady
            ? NativeWebViewDiagnosticSeverity.Info
            : NativeWebViewDiagnosticSeverity.Error;
        var diagnostics = new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            providerName: "windows-provider",
            [new NativeWebViewDiagnosticIssue("windows.issue", issueSeverity, "Issue")]);
        var entry = new NativeWebViewPlatformDiagnosticsReportEntry(
            diagnostics,
            providerRegistered: true,
            warningsAsErrors);

        return new NativeWebViewDiagnosticsReport(
            generatedAtUtc: DateTimeOffset.UtcNow,
            warningsAsErrors,
            [entry]);
    }
}
