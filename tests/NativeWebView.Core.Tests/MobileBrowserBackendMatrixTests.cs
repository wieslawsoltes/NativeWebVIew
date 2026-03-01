using NativeWebView.Core;
using NativeWebView.Interop;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.iOS;

namespace NativeWebView.Core.Tests;

public sealed class MobileBrowserBackendMatrixTests
{
    [Fact]
    public async Task IOSBackend_MatrixSmokeTest()
    {
        await RunMatrixSmokeTestAsync(
            NativeWebViewPlatform.IOS,
            factory => factory.UseNativeWebViewIOS());
    }

    [Fact]
    public async Task AndroidBackend_MatrixSmokeTest()
    {
        await RunMatrixSmokeTestAsync(
            NativeWebViewPlatform.Android,
            factory => factory.UseNativeWebViewAndroid());
    }

    [Fact]
    public async Task BrowserBackend_MatrixSmokeTest()
    {
        await RunMatrixSmokeTestAsync(
            NativeWebViewPlatform.Browser,
            factory => factory.UseNativeWebViewBrowser());
    }

    private static async Task RunMatrixSmokeTestAsync(
        NativeWebViewPlatform platform,
        Action<NativeWebViewBackendFactory> register)
    {
        var factory = new NativeWebViewBackendFactory();
        register(factory);

        Assert.True(factory.TryCreateNativeWebViewBackend(platform, out var webViewBackend));
        Assert.True(factory.TryCreateWebAuthenticationBrokerBackend(platform, out var authBackend));
        Assert.False(factory.TryCreateNativeWebDialogBackend(platform, out var dialogBackend));

        using (webViewBackend)
        {
            Assert.True(webViewBackend.Features.Supports(NativeWebViewFeature.EmbeddedView));
            Assert.True(webViewBackend.Features.Supports(NativeWebViewFeature.AuthenticationBroker));
            Assert.False(webViewBackend.Features.Supports(NativeWebViewFeature.Dialog));

            var environmentRequestedCount = 0;
            var controllerRequestedCount = 0;
            webViewBackend.CoreWebView2EnvironmentRequested += (_, _) => environmentRequestedCount++;
            webViewBackend.CoreWebView2ControllerOptionsRequested += (_, _) => controllerRequestedCount++;

            var receivedMessages = new List<NativeWebViewMessageReceivedEventArgs>();
            webViewBackend.WebMessageReceived += (_, args) => receivedMessages.Add(args);

            await webViewBackend.InitializeAsync();
            Assert.Equal(1, environmentRequestedCount);
            Assert.Equal(1, controllerRequestedCount);

            var handleProvider = Assert.IsAssignableFrom<INativeWebViewPlatformHandleProvider>(webViewBackend);
            Assert.True(handleProvider.TryGetPlatformHandle(out var platformHandle));
            Assert.True(handleProvider.TryGetViewHandle(out var viewHandle));
            Assert.True(handleProvider.TryGetControllerHandle(out var controllerHandle));
            AssertValidHandle(platformHandle);
            AssertValidHandle(viewHandle);
            AssertValidHandle(controllerHandle);

            Assert.True(webViewBackend.TryGetCookieManager(out var cookieManager));
            Assert.NotNull(cookieManager);
            Assert.True(webViewBackend.TryGetCommandManager(out var commandManager));
            Assert.NotNull(commandManager);

            webViewBackend.Navigate("https://example.com/mobile-browser");

            _ = await webViewBackend.ExecuteScriptAsync("1 + 2");
            await webViewBackend.PostWebMessageAsStringAsync("matrix-message");
            await webViewBackend.PostWebMessageAsJsonAsync("{\"kind\":\"matrix\"}");

            Assert.Equal(new Uri("https://example.com/mobile-browser"), webViewBackend.CurrentUrl);
            Assert.Equal(2, receivedMessages.Count);
            Assert.Equal("matrix-message", receivedMessages[0].Message);
            Assert.Equal("{\"kind\":\"matrix\"}", receivedMessages[1].Json);
        }

        using (authBackend)
        {
            var interactiveResult = await authBackend.AuthenticateAsync(
                new Uri("https://example.com/auth"),
                new Uri("https://example.com/callback"),
                WebAuthenticationOptions.UseTitle);

            Assert.Equal(WebAuthenticationStatus.Success, interactiveResult.ResponseStatus);
            Assert.NotNull(interactiveResult.ResponseData);

            switch (platform)
            {
                case NativeWebViewPlatform.IOS:
                    Assert.Contains("platform=ios", interactiveResult.ResponseData!, StringComparison.Ordinal);
                    break;
                case NativeWebViewPlatform.Android:
                    Assert.Contains("platform=android", interactiveResult.ResponseData!, StringComparison.Ordinal);
                    break;
                case NativeWebViewPlatform.Browser:
                    Assert.Contains("platform=browser", interactiveResult.ResponseData!, StringComparison.Ordinal);
                    Assert.Contains("popup=1", interactiveResult.ResponseData!, StringComparison.Ordinal);
                    break;
            }

            var composedResult = await authBackend.AuthenticateAsync(
                new Uri("https://example.com/auth"),
                new Uri("https://example.com/callback?state=123#token=seed"),
                WebAuthenticationOptions.UseTitle);

            Assert.Equal(WebAuthenticationStatus.Success, composedResult.ResponseStatus);
            Assert.NotNull(composedResult.ResponseData);

            switch (platform)
            {
                case NativeWebViewPlatform.IOS:
                    Assert.Equal(
                        "https://example.com/callback?state=123#token=seed&platform=ios&status=success",
                        composedResult.ResponseData);
                    break;
                case NativeWebViewPlatform.Android:
                    Assert.Equal(
                        "https://example.com/callback?state=123#token=seed&platform=android&status=success",
                        composedResult.ResponseData);
                    break;
                case NativeWebViewPlatform.Browser:
                    Assert.Equal(
                        "https://example.com/callback?state=123&popup=1&platform=browser#token=seed",
                        composedResult.ResponseData);
                    break;
            }

            var silentResult = await authBackend.AuthenticateAsync(
                new Uri("https://example.com/auth"),
                new Uri("https://example.com/callback"),
                WebAuthenticationOptions.SilentMode);

            Assert.Equal(WebAuthenticationStatus.UserCancel, silentResult.ResponseStatus);

            if (platform is NativeWebViewPlatform.Android)
            {
                var postResult = await authBackend.AuthenticateAsync(
                    new Uri("https://example.com/auth"),
                    new Uri("https://example.com/callback"),
                    WebAuthenticationOptions.UseHttpPost);

                Assert.Equal(WebAuthenticationStatus.ErrorHttp, postResult.ResponseStatus);
            }
        }

        using (dialogBackend)
        {
            Assert.Throws<PlatformNotSupportedException>(() => dialogBackend.Show());
        }
    }

    private static void AssertValidHandle(NativePlatformHandle handle)
    {
        Assert.NotEqual((nint)0, handle.Handle);
        Assert.False(string.IsNullOrWhiteSpace(handle.HandleDescriptor));
    }
}
