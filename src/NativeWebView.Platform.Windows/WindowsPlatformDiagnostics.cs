using NativeWebView.Core;

namespace NativeWebView.Platform.Windows;

internal static class WindowsPlatformDiagnostics
{
    public static NativeWebViewPlatformDiagnostics Create()
    {
        var issues = new List<NativeWebViewDiagnosticIssue>();
        AddContractOnlyControlWarning(issues);

        if (!NativeWebViewDiagnosticsHostPlatformOverride.IsEffectiveHostPlatform(NativeWebViewPlatform.Windows))
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "windows.host.mismatch",
                severity: NativeWebViewDiagnosticSeverity.Warning,
                message: "Windows backend diagnostics are running on a non-Windows host.",
                recommendation: "Run this diagnostic on Windows to validate native runtime requirements."));
        }
        else
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            {
                issues.Add(new NativeWebViewDiagnosticIssue(
                    code: "windows.os.version",
                    severity: NativeWebViewDiagnosticSeverity.Error,
                    message: "Windows 10 or newer is required.",
                    recommendation: "Upgrade the host OS to Windows 10+."));
            }

            var runtimePath = Environment.GetEnvironmentVariable("NATIVEWEBVIEW_WEBVIEW2_RUNTIME_PATH");
            if (!string.IsNullOrWhiteSpace(runtimePath) && !Directory.Exists(runtimePath))
            {
                issues.Add(new NativeWebViewDiagnosticIssue(
                    code: "windows.webview2.path",
                    severity: NativeWebViewDiagnosticSeverity.Error,
                    message: $"NATIVEWEBVIEW_WEBVIEW2_RUNTIME_PATH does not exist: {runtimePath}",
                    recommendation: "Fix the path or unset the override environment variable."));
            }
        }

        if (issues.Count == 0)
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "windows.ready",
                severity: NativeWebViewDiagnosticSeverity.Info,
                message: "Windows prerequisite checks passed."));
        }

        return new NativeWebViewPlatformDiagnostics(
            NativeWebViewPlatform.Windows,
            providerName: nameof(WindowsPlatformDiagnostics),
            issues);
    }

    private static void AddContractOnlyControlWarning(List<NativeWebViewDiagnosticIssue> issues)
    {
        var implementationStatus = NativeWebViewPlatformImplementationStatusMatrix.Get(NativeWebViewPlatform.Windows);
        if (implementationStatus.EmbeddedControl != NativeWebViewRepositoryImplementationStatus.RuntimeImplemented)
        {
            issues.Add(new NativeWebViewDiagnosticIssue(
                code: "windows.control.contract_only",
                severity: NativeWebViewDiagnosticSeverity.Warning,
                message: "Windows currently registers the NativeWebView control contract, but the embedded control runtime is not implemented in this repo yet.",
                recommendation: "Treat Windows embedded control support as planned work and check NativeWebViewPlatformImplementationStatusMatrix before shipping."));
        }
    }
}
