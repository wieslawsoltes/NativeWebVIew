using NativeWebView.Core;

using System.Reflection;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.iOS;

namespace NativeWebView.Core.Tests;

public sealed class PlatformImplementationStatusTests
{
    public static TheoryData<
        NativeWebViewPlatform,
        NativeWebViewRepositoryImplementationStatus,
        NativeWebViewRepositoryImplementationStatus,
        NativeWebViewRepositoryImplementationStatus,
        int?> MatrixCases =>
        new()
        {
            {
                NativeWebViewPlatform.Windows,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                null
            },
            {
                NativeWebViewPlatform.MacOS,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                null
            },
            {
                NativeWebViewPlatform.Linux,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                null
            },
            {
                NativeWebViewPlatform.IOS,
                GetAssemblyMetadataStatus(typeof(IOSNativeWebViewBackend).Assembly, "NativeWebView.EmbeddedControlRuntime"),
                NativeWebViewRepositoryImplementationStatus.Unsupported,
                GetAssemblyMetadataStatus(typeof(IOSNativeWebViewBackend).Assembly, "NativeWebView.AuthenticationBrokerRuntime"),
                null
            },
            {
                NativeWebViewPlatform.Android,
                GetAssemblyMetadataStatus(typeof(AndroidNativeWebViewBackend).Assembly, "NativeWebView.EmbeddedControlRuntime"),
                NativeWebViewRepositoryImplementationStatus.Unsupported,
                GetAssemblyMetadataStatus(typeof(AndroidNativeWebViewBackend).Assembly, "NativeWebView.AuthenticationBrokerRuntime"),
                null
            },
            {
                NativeWebViewPlatform.Browser,
                GetAssemblyMetadataStatus(typeof(BrowserNativeWebViewBackend).Assembly, "NativeWebView.EmbeddedControlRuntime"),
                NativeWebViewRepositoryImplementationStatus.Unsupported,
                GetAssemblyMetadataStatus(typeof(BrowserNativeWebViewBackend).Assembly, "NativeWebView.AuthenticationBrokerRuntime"),
                null
            },
            {
                NativeWebViewPlatform.Unknown,
                NativeWebViewRepositoryImplementationStatus.Unsupported,
                NativeWebViewRepositoryImplementationStatus.Unsupported,
                NativeWebViewRepositoryImplementationStatus.Unsupported,
                null
            },
        };

    [Theory]
    [MemberData(nameof(MatrixCases))]
    public void Matrix_ReportsExpectedRepositoryImplementationStatus(
        NativeWebViewPlatform platform,
        NativeWebViewRepositoryImplementationStatus embeddedControl,
        NativeWebViewRepositoryImplementationStatus dialog,
        NativeWebViewRepositoryImplementationStatus authenticationBroker,
        int? recommendedBringUpOrder)
    {
        var status = NativeWebViewPlatformImplementationStatusMatrix.Get(platform);

        Assert.Equal(platform, status.Platform);
        Assert.Equal(embeddedControl, status.EmbeddedControl);
        Assert.Equal(dialog, status.Dialog);
        Assert.Equal(authenticationBroker, status.AuthenticationBroker);
        Assert.Equal(recommendedBringUpOrder, status.RecommendedBringUpOrder);
        Assert.False(string.IsNullOrWhiteSpace(status.Summary));
    }

    [Fact]
    public void RemainingBringUpOrder_IsStable()
    {
        var order = NativeWebViewPlatformImplementationStatusMatrix.GetRemainingPlatformBringUpOrder();

        Assert.Empty(order);
    }

    [Fact]
    public void RemainingBringUpOrder_IsExposedAsReadOnlySequence()
    {
        var order = NativeWebViewPlatformImplementationStatusMatrix.GetRemainingPlatformBringUpOrder();

        Assert.False(order is NativeWebViewPlatform[]);

        var list = Assert.IsAssignableFrom<IList<NativeWebViewPlatform>>(order);
        Assert.True(list.IsReadOnly);
    }

    [Fact]
    public void HasEmbeddedControlRuntime_MatchesEmbeddedControlStatus()
    {
        foreach (var platform in Enum.GetValues<NativeWebViewPlatform>())
        {
            var status = NativeWebViewPlatformImplementationStatusMatrix.Get(platform);

            Assert.Equal(
                status.EmbeddedControl == NativeWebViewRepositoryImplementationStatus.RuntimeImplemented,
                status.HasEmbeddedControlRuntime);
        }
    }

    private static NativeWebViewRepositoryImplementationStatus GetAssemblyMetadataStatus(Assembly assembly, string key)
    {
        var value = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
            ?.Value;

        return bool.TryParse(value, out var isImplemented) && isImplemented
            ? NativeWebViewRepositoryImplementationStatus.RuntimeImplemented
            : NativeWebViewRepositoryImplementationStatus.ContractOnly;
    }
}
