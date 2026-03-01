using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class DiagnosticsRegressionEvaluatorTests
{
    [Fact]
    public void Evaluate_NoComparisonAndNoStrictPolicies_ReturnsPassingEvaluation()
    {
        var report = CreateReport(isReady: true, warningsAsErrors: false);

        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison: null,
            requireReady: false,
            failOnRegression: true,
            requireBaselineSync: false);

        Assert.True(evaluation.IsReady);
        Assert.False(evaluation.HasComparison);
        Assert.False(evaluation.HasRegression);
        Assert.False(evaluation.HasStaleBaseline);
        Assert.False(evaluation.RequiresBaselineUpdate);
        Assert.False(evaluation.WouldFailRequireReady);
        Assert.False(evaluation.WouldFailRegressionGate);
        Assert.False(evaluation.WouldFailBaselineSyncGate);
        Assert.Empty(evaluation.FailingGates);
        Assert.Empty(evaluation.GateFailures);
        Assert.False(evaluation.HasMultipleGateFailures);
        Assert.Null(evaluation.PrimaryFailingGate);
        Assert.Equal(0, evaluation.EffectiveExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.CurrentFingerprintVersion, evaluation.FingerprintVersion);
        Assert.Matches("^[0-9a-f]{64}$", evaluation.Fingerprint);
    }

    [Fact]
    public void Evaluate_RequireReadyAndNotReady_FailsRequireReadyGate()
    {
        var report = CreateReport(isReady: false, warningsAsErrors: false);

        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison: null,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: false);

        Assert.True(evaluation.WouldFailRequireReady);
        var gate = Assert.Single(evaluation.FailingGates);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.RequireReady, gate);
        Assert.False(evaluation.FailingGates is NativeWebViewDiagnosticsGateFailureKind[]);
        var gateFailure = Assert.Single(evaluation.GateFailures);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.RequireReady, gateFailure.Kind);
        Assert.Equal(10, gateFailure.ExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureMessage(NativeWebViewDiagnosticsGateFailureKind.RequireReady), gateFailure.Message);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureRecommendation(NativeWebViewDiagnosticsGateFailureKind.RequireReady), gateFailure.Recommendation);
        Assert.False(evaluation.HasMultipleGateFailures);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.RequireReady, evaluation.PrimaryFailingGate);
        Assert.Equal(10, evaluation.EffectiveExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.CurrentFingerprintVersion, evaluation.FingerprintVersion);
    }

    [Fact]
    public void Evaluate_ComparisonWithRegression_FailsRegressionGate()
    {
        var report = CreateReport(isReady: true, warningsAsErrors: false);
        var comparison = new NativeWebViewDiagnosticsRegressionResult(
            baselineBlockingIssues: [],
            currentBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "windows.error")
            ],
            newBlockingIssues:
            [
                new NativeWebViewDiagnosticIssueReference(NativeWebViewPlatform.Windows, "windows.error")
            ],
            resolvedBlockingIssues: []);

        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison,
            requireReady: false,
            failOnRegression: true,
            requireBaselineSync: false);

        Assert.True(evaluation.HasComparison);
        Assert.True(evaluation.HasRegression);
        Assert.False(evaluation.HasStaleBaseline);
        Assert.True(evaluation.WouldFailRegressionGate);
        var gate = Assert.Single(evaluation.FailingGates);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.Regression, gate);
        var gateFailure = Assert.Single(evaluation.GateFailures);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.Regression, gateFailure.Kind);
        Assert.Equal(11, gateFailure.ExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureMessage(NativeWebViewDiagnosticsGateFailureKind.Regression), gateFailure.Message);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureRecommendation(NativeWebViewDiagnosticsGateFailureKind.Regression), gateFailure.Recommendation);
        Assert.False(evaluation.HasMultipleGateFailures);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.Regression, evaluation.PrimaryFailingGate);
        Assert.Equal(11, evaluation.EffectiveExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.CurrentFingerprintVersion, evaluation.FingerprintVersion);
    }

    [Fact]
    public void Evaluate_ComparisonWithStaleBaseline_FailsBaselineSyncGate()
    {
        var report = CreateReport(isReady: true, warningsAsErrors: false);
        var comparison = new NativeWebViewDiagnosticsRegressionResult(
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

        var evaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            report,
            comparison,
            requireReady: false,
            failOnRegression: true,
            requireBaselineSync: true);

        Assert.False(evaluation.HasRegression);
        Assert.True(evaluation.HasStaleBaseline);
        Assert.True(evaluation.RequiresBaselineUpdate);
        Assert.True(evaluation.WouldFailBaselineSyncGate);
        var gate = Assert.Single(evaluation.FailingGates);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, gate);
        var gateFailure = Assert.Single(evaluation.GateFailures);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, gateFailure.Kind);
        Assert.Equal(12, gateFailure.ExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureMessage(NativeWebViewDiagnosticsGateFailureKind.BaselineSync), gateFailure.Message);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureRecommendation(NativeWebViewDiagnosticsGateFailureKind.BaselineSync), gateFailure.Recommendation);
        Assert.False(evaluation.HasMultipleGateFailures);
        Assert.Equal(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, evaluation.PrimaryFailingGate);
        Assert.Equal(12, evaluation.EffectiveExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.CurrentFingerprintVersion, evaluation.FingerprintVersion);
    }

    [Fact]
    public void Evaluate_MultipleFailingGates_UsesCombinedExitCode()
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

        Assert.True(evaluation.WouldFailRequireReady);
        Assert.True(evaluation.WouldFailRegressionGate);
        Assert.True(evaluation.WouldFailBaselineSyncGate);
        Assert.Equal(3, evaluation.FailingGates.Count);
        Assert.Contains(NativeWebViewDiagnosticsGateFailureKind.RequireReady, evaluation.FailingGates);
        Assert.Contains(NativeWebViewDiagnosticsGateFailureKind.Regression, evaluation.FailingGates);
        Assert.Contains(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, evaluation.FailingGates);
        Assert.Equal(3, evaluation.GateFailures.Count);
        Assert.Contains(evaluation.GateFailures, static item => item.Kind == NativeWebViewDiagnosticsGateFailureKind.RequireReady && item.ExitCode == 10);
        Assert.Contains(evaluation.GateFailures, static item => item.Kind == NativeWebViewDiagnosticsGateFailureKind.Regression && item.ExitCode == 11);
        Assert.Contains(evaluation.GateFailures, static item => item.Kind == NativeWebViewDiagnosticsGateFailureKind.BaselineSync && item.ExitCode == 12);
        Assert.True(evaluation.HasMultipleGateFailures);
        Assert.Null(evaluation.PrimaryFailingGate);
        Assert.Equal(13, evaluation.EffectiveExitCode);
        Assert.Equal(NativeWebViewDiagnosticsRegressionEvaluation.CurrentFingerprintVersion, evaluation.FingerprintVersion);
    }

    [Fact]
    public void Evaluate_RequireBaselineSyncWithoutComparison_ThrowsArgumentException()
    {
        var report = CreateReport(isReady: true, warningsAsErrors: false);

        var ex = Assert.Throws<ArgumentException>(() =>
            NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
                report,
                comparison: null,
                requireReady: false,
                failOnRegression: true,
                requireBaselineSync: true));

        Assert.Equal("comparison", ex.ParamName);
        Assert.Contains("Baseline sync policy requires a baseline comparison result.", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.RequireReady, 10)]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.Regression, 11)]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, 12)]
    public void GetExitCodeForGate_UsesStableMapping(
        NativeWebViewDiagnosticsGateFailureKind gate,
        int expectedExitCode)
    {
        var actualExitCode = NativeWebViewDiagnosticsRegressionEvaluation.GetExitCodeForGate(gate);

        Assert.Equal(expectedExitCode, actualExitCode);
    }

    [Theory]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.RequireReady, "Blocking diagnostics issues were found and --require-ready is enabled.")]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.Regression, "Blocking diagnostics regressions were found and regression gating is enabled.")]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, "Blocking diagnostics baseline contains resolved entries and --require-baseline-sync is enabled.")]
    public void GetGateFailureMessage_UsesStableMessageContract(
        NativeWebViewDiagnosticsGateFailureKind gate,
        string expectedMessage)
    {
        var actualMessage = NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureMessage(gate);

        Assert.Equal(expectedMessage, actualMessage);
    }

    [Theory]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.RequireReady, "Fix blocking diagnostics issues or run with --allow-not-ready when collecting non-gating reports.")]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.Regression, "Resolve newly introduced blocking issues or run with --allow-regression when triaging intentional changes.")]
    [InlineData(NativeWebViewDiagnosticsGateFailureKind.BaselineSync, "Refresh baseline using ./scripts/update-blocking-baseline.sh when resolved entries are intentional.")]
    public void GetGateFailureRecommendation_UsesStableRecommendationContract(
        NativeWebViewDiagnosticsGateFailureKind gate,
        string expectedRecommendation)
    {
        var actualRecommendation = NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureRecommendation(gate);

        Assert.Equal(expectedRecommendation, actualRecommendation);
    }

    [Fact]
    public void GetGateHelpers_InvalidGate_ThrowArgumentOutOfRangeException()
    {
        var invalidGate = (NativeWebViewDiagnosticsGateFailureKind)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NativeWebViewDiagnosticsRegressionEvaluation.GetExitCodeForGate(invalidGate));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureMessage(invalidGate));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NativeWebViewDiagnosticsRegressionEvaluation.GetGateFailureRecommendation(invalidGate));
    }

    [Fact]
    public void EvaluationFingerprint_IgnoresGeneratedTimestamp()
    {
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

        var first = new NativeWebViewDiagnosticsRegressionEvaluation(
            generatedAtUtc: DateTimeOffset.Parse("2026-03-01T10:00:00Z", null, System.Globalization.DateTimeStyles.AssumeUniversal),
            warningsAsErrors: true,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: true,
            isReady: false,
            comparison);
        var second = new NativeWebViewDiagnosticsRegressionEvaluation(
            generatedAtUtc: DateTimeOffset.Parse("2026-03-01T11:30:00Z", null, System.Globalization.DateTimeStyles.AssumeUniversal),
            warningsAsErrors: true,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: true,
            isReady: false,
            comparison);

        Assert.Equal(first.EffectiveExitCode, second.EffectiveExitCode);
        Assert.Equal(first.FingerprintVersion, second.FingerprintVersion);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void EvaluationFingerprint_ChangesWhenGateOutcomeChanges()
    {
        var reportReady = CreateReport(isReady: true, warningsAsErrors: false);
        var reportNotReady = CreateReport(isReady: false, warningsAsErrors: false);

        var readyEvaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            reportReady,
            comparison: null,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: false);
        var notReadyEvaluation = NativeWebViewDiagnosticsRegressionEvaluator.Evaluate(
            reportNotReady,
            comparison: null,
            requireReady: true,
            failOnRegression: true,
            requireBaselineSync: false);

        Assert.Equal(0, readyEvaluation.EffectiveExitCode);
        Assert.Equal(10, notReadyEvaluation.EffectiveExitCode);
        Assert.Equal(readyEvaluation.FingerprintVersion, notReadyEvaluation.FingerprintVersion);
        Assert.NotEqual(readyEvaluation.Fingerprint, notReadyEvaluation.Fingerprint);
    }

    [Fact]
    public void FingerprintVersion_UsesStableCurrentVersion()
    {
        Assert.Equal(1, NativeWebViewDiagnosticsRegressionEvaluation.CurrentFingerprintVersion);
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
