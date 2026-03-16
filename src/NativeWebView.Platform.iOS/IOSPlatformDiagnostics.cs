using NativeWebView.Core;

namespace NativeWebView.Platform.iOS;

internal static class IOSPlatformDiagnostics
{
    public static NativeWebViewPlatformDiagnostics Create()
    {
        var issues = new List<NativeWebViewDiagnosticIssue>();
        AddContractOnlyControlWarning(issues);

        if (!NativeWebViewDiagnosticsHostPlatformOverride.IsEffectiveHostPlatform(NativeWebViewPlatform.IOS))
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "ios.host.mismatch",
                severity: NativeWebViewDiagnosticSeverity.Warning,
                message: "iOS backend diagnostics are running on a non-iOS host.",
                recommendation: "Run this diagnostic on iOS to validate device/runtime prerequisites."));
        }
        else if (!OperatingSystem.IsIOSVersionAtLeast(17))
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "ios.os.version",
                severity: NativeWebViewDiagnosticSeverity.Error,
                message: "iOS 17 or newer is required for the current NativeWebView iOS runtime path.",
                recommendation: "Upgrade the target device/simulator to iOS 17+."));
        }

        if (issues.Count == 0)
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "ios.ready",
                severity: NativeWebViewDiagnosticSeverity.Info,
                message: "iOS prerequisite checks passed."));
        }

        return new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.IOS,
            providerName: nameof(IOSPlatformDiagnostics),
            issues);
    }

    private static void AddContractOnlyControlWarning(List<NativeWebViewDiagnosticIssue> issues)
    {
        var implementationStatus = NativeWebViewPlatformImplementationStatusMatrix.Get(NativeWebViewPlatform.IOS);
        if (implementationStatus.EmbeddedControl != NativeWebViewRepositoryImplementationStatus.RuntimeImplemented)
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "ios.control.contract_only",
                severity: NativeWebViewDiagnosticSeverity.Warning,
                message: "iOS currently registers the NativeWebView control contract, but this build does not include the Apple-workload-targeted embedded control runtime.",
                recommendation: "Build the iOS-targeted backend assembly and check NativeWebViewPlatformImplementationStatusMatrix before shipping embedded control support."));
        }
    }
}
