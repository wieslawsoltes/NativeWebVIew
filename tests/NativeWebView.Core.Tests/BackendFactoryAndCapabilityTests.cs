using NativeWebView.Core;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.Windows;
using NativeWebView.Platform.iOS;

namespace NativeWebView.Core.Tests;

public sealed class BackendFactoryAndCapabilityTests
{
    [Fact]
    public void Register_WindowsModule_WorksForMultipleFactories()
    {
        var firstFactory = new NativeWebViewBackendFactory();
        var secondFactory = new NativeWebViewBackendFactory();

        NativeWebViewPlatformWindowsModule.Register(firstFactory);
        NativeWebViewPlatformWindowsModule.Register(secondFactory);

        var firstResult = firstFactory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Windows, out var firstBackend);
        var secondResult = secondFactory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Windows, out var secondBackend);

        Assert.True(firstResult);
        Assert.True(secondResult);
        Assert.Equal(NativeWebViewPlatform.Windows, firstBackend.Platform);
        Assert.Equal(NativeWebViewPlatform.Windows, secondBackend.Platform);
    }

    [Fact]
    public async Task UnregisteredWebViewBackend_ThrowsWhenEmbeddedViewIsNotSupported()
    {
        var factory = new NativeWebViewBackendFactory();
        var created = factory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Windows, out var backend);

        Assert.False(created);
        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => backend.InitializeAsync().AsTask());
        Assert.Throws<PlatformNotSupportedException>(() => backend.Navigate("https://example.com"));
    }

    [Fact]
    public async Task BrowserBackend_RaisesEnvironmentAndControllerEvents()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.UseNativeWebViewBrowser();

        var created = factory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Browser, out var backend);
        Assert.True(created);

        var environmentRequestedCount = 0;
        var controllerRequestedCount = 0;

        backend.CoreWebView2EnvironmentRequested += (_, _) => environmentRequestedCount++;
        backend.CoreWebView2ControllerOptionsRequested += (_, _) => controllerRequestedCount++;

        await backend.InitializeAsync();

        Assert.Equal(1, environmentRequestedCount);
        Assert.Equal(1, controllerRequestedCount);
    }

    [Fact]
    public void IOSDialogBackend_IsNotRegistered_AndFallbackThrowsOnShow()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.UseNativeWebViewIOS();

        var created = factory.TryCreateNativeWebDialogBackend(NativeWebViewPlatform.IOS, out var dialogBackend);

        Assert.False(created);
        Assert.Throws<PlatformNotSupportedException>(() => dialogBackend.Show());
    }

    [Fact]
    public async Task AndroidBackend_RaisesEnvironmentAndControllerEvents()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.UseNativeWebViewAndroid();

        var created = factory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Android, out var backend);
        Assert.True(created);
        Assert.True(factory.TryCreateWebAuthenticationBrokerBackend(NativeWebViewPlatform.Android, out var authBackend));
        Assert.False(factory.TryCreateNativeWebDialogBackend(NativeWebViewPlatform.Android, out var dialogBackend));

        var environmentRequestedCount = 0;
        var controllerRequestedCount = 0;

        backend.CoreWebView2EnvironmentRequested += (_, _) => environmentRequestedCount++;
        backend.CoreWebView2ControllerOptionsRequested += (_, _) => controllerRequestedCount++;

        await backend.InitializeAsync();

        Assert.Equal(1, environmentRequestedCount);
        Assert.Equal(1, controllerRequestedCount);

        authBackend.Dispose();
        dialogBackend.Dispose();
    }

    [Fact]
    public async Task PartialRegistration_FallbackDialogAndAuthRemainUnsupported()
    {
        var factory = new NativeWebViewBackendFactory();
        var declaredFeatures = new WebViewPlatformFeatures(
            NativeWebViewPlatform.Windows,
            NativeWebViewFeature.EmbeddedView |
            NativeWebViewFeature.Dialog |
            NativeWebViewFeature.AuthenticationBroker |
            NativeWebViewFeature.ScriptExecution |
            NativeWebViewFeature.WebMessageChannel);

        factory.RegisterNativeWebViewBackend(
            NativeWebViewPlatform.Windows,
            () => new UnregisteredNativeWebViewBackend(NativeWebViewPlatform.Windows, declaredFeatures),
            declaredFeatures);

        var dialogCreated = factory.TryCreateNativeWebDialogBackend(NativeWebViewPlatform.Windows, out var dialogBackend);
        using (dialogBackend)
        {
            Assert.False(dialogCreated);
            Assert.False(dialogBackend.Features.Supports(NativeWebViewFeature.Dialog));
            Assert.Throws<PlatformNotSupportedException>(() => dialogBackend.Show());
        }

        var authCreated = factory.TryCreateWebAuthenticationBrokerBackend(NativeWebViewPlatform.Windows, out var authBackend);
        using (authBackend)
        {
            Assert.False(authCreated);
            Assert.False(authBackend.Features.Supports(NativeWebViewFeature.AuthenticationBroker));

            var result = await authBackend.AuthenticateAsync(
                new Uri("https://example.com/auth"),
                new Uri("https://example.com/callback"));

            Assert.Equal(WebAuthenticationStatus.ErrorHttp, result.ResponseStatus);
            Assert.NotEqual(0, result.ResponseErrorDetail);
        }
    }
}
