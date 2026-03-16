using System.Reflection;

namespace NativeWebView.Core;

public enum NativeWebViewRepositoryImplementationStatus
{
    Unsupported = 0,
    ContractOnly,
    RuntimeImplemented,
}

public sealed class NativeWebViewPlatformImplementationStatus
{
    internal NativeWebViewPlatformImplementationStatus(
        NativeWebViewPlatform platform,
        NativeWebViewRepositoryImplementationStatus embeddedControl,
        NativeWebViewRepositoryImplementationStatus dialog,
        NativeWebViewRepositoryImplementationStatus authenticationBroker,
        string summary,
        int? recommendedBringUpOrder = null)
    {
        Platform = platform;
        EmbeddedControl = embeddedControl;
        Dialog = dialog;
        AuthenticationBroker = authenticationBroker;
        Summary = summary;
        RecommendedBringUpOrder = recommendedBringUpOrder;
    }

    public NativeWebViewPlatform Platform { get; }

    public NativeWebViewRepositoryImplementationStatus EmbeddedControl { get; }

    public NativeWebViewRepositoryImplementationStatus Dialog { get; }

    public NativeWebViewRepositoryImplementationStatus AuthenticationBroker { get; }

    public string Summary { get; }

    public int? RecommendedBringUpOrder { get; }

    public bool HasEmbeddedControlRuntime =>
        EmbeddedControl == NativeWebViewRepositoryImplementationStatus.RuntimeImplemented;
}

public static class NativeWebViewPlatformImplementationStatusMatrix
{
    private const string EmbeddedControlRuntimeMetadataKey = "NativeWebView.EmbeddedControlRuntime";
    private const string AuthenticationBrokerRuntimeMetadataKey = "NativeWebView.AuthenticationBrokerRuntime";

    private static readonly IReadOnlyList<NativeWebViewPlatform> RemainingPlatformBringUpOrder =
        Array.AsReadOnly(Array.Empty<NativeWebViewPlatform>());

    public static NativeWebViewPlatformImplementationStatus Get(NativeWebViewPlatform platform)
    {
        return platform switch
        {
            NativeWebViewPlatform.Windows => new NativeWebViewPlatformImplementationStatus(
                platform,
                embeddedControl: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                dialog: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                authenticationBroker: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                summary: "Windows now ships real embedded NativeWebView, NativeWebDialog, and WebAuthenticationBroker runtime paths backed by WebView2 plus a native dialog host."),
            NativeWebViewPlatform.MacOS => new NativeWebViewPlatformImplementationStatus(
                platform,
                embeddedControl: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                dialog: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                authenticationBroker: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                summary: "macOS now ships real embedded NativeWebView, NativeWebDialog, and dialog-backed WebAuthenticationBroker runtime paths in this repo."),
            NativeWebViewPlatform.Linux => new NativeWebViewPlatformImplementationStatus(
                platform,
                embeddedControl: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                dialog: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                authenticationBroker: NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                summary: "Linux now ships real embedded NativeWebView, NativeWebDialog, and WebAuthenticationBroker runtime paths backed by GTK3/WebKitGTK on X11."),
            NativeWebViewPlatform.IOS => CreateConditionallyCompiledStatus(
                platform,
                "NativeWebView.Platform.iOS",
                runtimeSummary: "iOS now ships a real embedded NativeWebView control runtime plus a modal WKWebView-based WebAuthenticationBroker when the iOS backend is built with the .NET 8 Apple workload; NativeWebDialog remains unsupported.",
                contractOnlySummary: "The iOS runtime implementations exist in this repo, but the current build only includes the contract backend because the .NET 8 Apple workload-targeted assembly is not compiled; NativeWebDialog remains unsupported."),
            NativeWebViewPlatform.Android => CreateConditionallyCompiledStatus(
                platform,
                "NativeWebView.Platform.Android",
                runtimeSummary: "Android now ships a real embedded NativeWebView control runtime plus a dedicated WebView-backed WebAuthenticationBroker activity when the Android backend is built with the .NET 8 Android workload; NativeWebDialog remains unsupported.",
                contractOnlySummary: "The Android runtime implementations exist in this repo, but the current build only includes the contract backend because the .NET 8 Android workload-targeted assembly is not compiled; NativeWebDialog remains unsupported."),
            NativeWebViewPlatform.Browser => CreateConditionallyCompiledStatus(
                platform,
                "NativeWebView.Platform.Browser",
                runtimeSummary: "Browser now ships a real embedded NativeWebView control runtime plus a popup-driven WebAuthenticationBroker backed by Avalonia Browser hosting and DOM/browser APIs when the browser-targeted backend assembly is compiled; NativeWebDialog remains unsupported.",
                contractOnlySummary: "The Browser runtime implementations exist in this repo, but the current build only includes the contract backend because the browser-targeted backend assembly is not compiled; NativeWebDialog remains unsupported."),
            _ => new NativeWebViewPlatformImplementationStatus(
                platform,
                embeddedControl: NativeWebViewRepositoryImplementationStatus.Unsupported,
                dialog: NativeWebViewRepositoryImplementationStatus.Unsupported,
                authenticationBroker: NativeWebViewRepositoryImplementationStatus.Unsupported,
                summary: "Runtime implementation status is unknown for this platform."),
        };
    }

    public static IReadOnlyList<NativeWebViewPlatform> GetRemainingPlatformBringUpOrder()
    {
        return RemainingPlatformBringUpOrder;
    }

    private static NativeWebViewPlatformImplementationStatus CreateConditionallyCompiledStatus(
        NativeWebViewPlatform platform,
        string assemblyName,
        string runtimeSummary,
        string contractOnlySummary)
    {
        var embeddedControl = GetConditionallyCompiledRuntimeStatus(assemblyName, EmbeddedControlRuntimeMetadataKey);
        var authenticationBroker = GetConditionallyCompiledRuntimeStatus(assemblyName, AuthenticationBrokerRuntimeMetadataKey);

        var summary = embeddedControl == NativeWebViewRepositoryImplementationStatus.RuntimeImplemented &&
            authenticationBroker == NativeWebViewRepositoryImplementationStatus.RuntimeImplemented
            ? runtimeSummary
            : contractOnlySummary;

        return new NativeWebViewPlatformImplementationStatus(
            platform,
            embeddedControl,
            dialog: NativeWebViewRepositoryImplementationStatus.Unsupported,
            authenticationBroker,
            summary);
    }

    private static NativeWebViewRepositoryImplementationStatus GetConditionallyCompiledRuntimeStatus(
        string assemblyName,
        string metadataKey)
    {
        var assembly = TryGetLoadedOrResolvableAssembly(assemblyName);
        if (assembly is null)
        {
            return NativeWebViewRepositoryImplementationStatus.ContractOnly;
        }

        foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (!string.Equals(metadata.Key, metadataKey, StringComparison.Ordinal))
            {
                continue;
            }

            return bool.TryParse(metadata.Value, out var enabled) && enabled
                ? NativeWebViewRepositoryImplementationStatus.RuntimeImplemented
                : NativeWebViewRepositoryImplementationStatus.ContractOnly;
        }

        return NativeWebViewRepositoryImplementationStatus.ContractOnly;
    }

    private static Assembly? TryGetLoadedOrResolvableAssembly(string assemblyName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
            {
                return assembly;
            }
        }

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            return null;
        }
    }
}
