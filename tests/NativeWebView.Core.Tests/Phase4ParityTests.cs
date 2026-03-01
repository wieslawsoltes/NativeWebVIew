using NativeWebView.Core;
using NativeWebView.Interop;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.Linux;
using NativeWebView.Platform.Windows;
using NativeWebView.Platform.iOS;
using NativeWebView.Platform.macOS;
using NativeWebViewControl = NativeWebView.Controls.NativeWebView;
using NativeWebDialogFacade = NativeWebView.Dialog.NativeWebDialog;

namespace NativeWebView.Core.Tests;

public sealed class Phase4ParityTests
{
    private static readonly (NativeWebViewPlatform Platform, Action<NativeWebViewBackendFactory> Register)[] AllPlatforms =
    [
        (NativeWebViewPlatform.Windows, static factory => factory.UseNativeWebViewWindows()),
        (NativeWebViewPlatform.MacOS, static factory => factory.UseNativeWebViewMacOS()),
        (NativeWebViewPlatform.Linux, static factory => factory.UseNativeWebViewLinux()),
        (NativeWebViewPlatform.IOS, static factory => factory.UseNativeWebViewIOS()),
        (NativeWebViewPlatform.Android, static factory => factory.UseNativeWebViewAndroid()),
        (NativeWebViewPlatform.Browser, static factory => factory.UseNativeWebViewBrowser()),
    ];

    private static readonly (NativeWebViewPlatform Platform, Action<NativeWebViewBackendFactory> Register)[] DesktopPlatforms =
    [
        (NativeWebViewPlatform.Windows, static factory => factory.UseNativeWebViewWindows()),
        (NativeWebViewPlatform.MacOS, static factory => factory.UseNativeWebViewMacOS()),
        (NativeWebViewPlatform.Linux, static factory => factory.UseNativeWebViewLinux()),
    ];

    [Fact]
    public async Task AllWebViewBackends_RaiseEnvironmentAndControllerOptionEvents()
    {
        foreach (var (platform, register) in AllPlatforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryCreateNativeWebViewBackend(platform, out var backend));
            using (backend)
            {
                var environmentRequestedCount = 0;
                var controllerRequestedCount = 0;

                backend.CoreWebView2EnvironmentRequested += (_, _) => environmentRequestedCount++;
                backend.CoreWebView2ControllerOptionsRequested += (_, _) => controllerRequestedCount++;

                await backend.InitializeAsync();

                Assert.Equal(1, environmentRequestedCount);
                Assert.Equal(1, controllerRequestedCount);
            }
        }
    }

    [Fact]
    public void AllWebViewBackends_ExposeNativePlatformHandles()
    {
        foreach (var (platform, register) in AllPlatforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryCreateNativeWebViewBackend(platform, out var backend));
            using (backend)
            {
                var provider = Assert.IsAssignableFrom<INativeWebViewPlatformHandleProvider>(backend);

                Assert.True(provider.TryGetPlatformHandle(out var platformHandle));
                Assert.True(provider.TryGetViewHandle(out var viewHandle));
                Assert.True(provider.TryGetControllerHandle(out var controllerHandle));

                AssertValidHandle(platformHandle);
                AssertValidHandle(viewHandle);
                AssertValidHandle(controllerHandle);
            }
        }
    }

    [Fact]
    public void AllWebViewBackends_ExposeCookieAndCommandManagers()
    {
        foreach (var (platform, register) in AllPlatforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryCreateNativeWebViewBackend(platform, out var backend));
            using (backend)
            {
                Assert.True(backend.TryGetCookieManager(out var cookieManager));
                Assert.NotNull(cookieManager);

                Assert.True(backend.TryGetCommandManager(out var commandManager));
                Assert.NotNull(commandManager);
            }
        }
    }

    [Fact]
    public void DesktopDialogBackends_ExposeNativePlatformHandles()
    {
        foreach (var (platform, register) in DesktopPlatforms)
        {
            var factory = new NativeWebViewBackendFactory();
            register(factory);

            Assert.True(factory.TryCreateNativeWebDialogBackend(platform, out var backend));
            using (backend)
            {
                var provider = Assert.IsAssignableFrom<INativeWebDialogPlatformHandleProvider>(backend);

                Assert.True(provider.TryGetPlatformHandle(out var platformHandle));
                Assert.True(provider.TryGetDialogHandle(out var dialogHandle));
                Assert.True(provider.TryGetHostWindowHandle(out var hostWindowHandle));

                AssertValidHandle(platformHandle);
                AssertValidHandle(dialogHandle);
                AssertValidHandle(hostWindowHandle);
            }
        }
    }

    [Fact]
    public void Facades_ExposeHandleProviders_WhenBackendSupportsInterop()
    {
        var factory = new NativeWebViewBackendFactory();
        factory.UseNativeWebViewWindows();

        Assert.True(factory.TryCreateNativeWebViewBackend(NativeWebViewPlatform.Windows, out var webViewBackend));
        using var control = new NativeWebViewControl(webViewBackend);
        Assert.True(control.TryGetPlatformHandle(out var platformHandle));
        Assert.True(control.TryGetViewHandle(out var viewHandle));
        Assert.True(control.TryGetControllerHandle(out var controllerHandle));

        AssertValidHandle(platformHandle);
        AssertValidHandle(viewHandle);
        AssertValidHandle(controllerHandle);

        Assert.True(factory.TryCreateNativeWebDialogBackend(NativeWebViewPlatform.Windows, out var dialogBackend));
        using var dialog = new NativeWebDialogFacade(dialogBackend);
        Assert.True(dialog.TryGetPlatformHandle(out platformHandle));
        Assert.True(dialog.TryGetDialogHandle(out var dialogHandle));
        Assert.True(dialog.TryGetHostWindowHandle(out var hostWindowHandle));

        AssertValidHandle(platformHandle);
        AssertValidHandle(dialogHandle);
        AssertValidHandle(hostWindowHandle);
    }

    private static void AssertValidHandle(NativePlatformHandle handle)
    {
        Assert.NotEqual((nint)0, handle.Handle);
        Assert.False(string.IsNullOrWhiteSpace(handle.HandleDescriptor));
    }
}
