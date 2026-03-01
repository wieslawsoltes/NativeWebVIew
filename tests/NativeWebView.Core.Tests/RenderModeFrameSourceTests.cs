using NativeWebView.Core;
using NativeWebView.Platform.Android;
using NativeWebView.Platform.Browser;
using NativeWebView.Platform.Linux;
using NativeWebView.Platform.Windows;
using NativeWebView.Platform.iOS;
using NativeWebView.Platform.macOS;

namespace NativeWebView.Core.Tests;

public sealed class RenderModeFrameSourceTests
{
    [Fact]
    public async Task AllRegisteredBackends_ExposeGpuAndOffscreenFrameSources()
    {
        var matrix = new (NativeWebViewPlatform Platform, Action<NativeWebViewBackendFactory> Register)[]
        {
            (NativeWebViewPlatform.Windows, static factory => factory.UseNativeWebViewWindows()),
            (NativeWebViewPlatform.MacOS, static factory => factory.UseNativeWebViewMacOS()),
            (NativeWebViewPlatform.Linux, static factory => factory.UseNativeWebViewLinux()),
            (NativeWebViewPlatform.IOS, static factory => factory.UseNativeWebViewIOS()),
            (NativeWebViewPlatform.Android, static factory => factory.UseNativeWebViewAndroid()),
            (NativeWebViewPlatform.Browser, static factory => factory.UseNativeWebViewBrowser()),
        };

        foreach (var item in matrix)
        {
            var factory = new NativeWebViewBackendFactory();
            item.Register(factory);

            Assert.True(factory.TryCreateNativeWebViewBackend(item.Platform, out var backend));

            using (backend)
            {
                Assert.True(backend.Features.Supports(NativeWebViewFeature.RenderFrameCapture));

                var frameSource = Assert.IsAssignableFrom<INativeWebViewFrameSource>(backend);
                Assert.True(frameSource.SupportsRenderMode(NativeWebViewRenderMode.GpuSurface));
                Assert.True(frameSource.SupportsRenderMode(NativeWebViewRenderMode.Offscreen));

                var request = new NativeWebViewRenderFrameRequest
                {
                    PixelWidth = 320,
                    PixelHeight = 200,
                };

                var gpuFrame = await frameSource.CaptureFrameAsync(NativeWebViewRenderMode.GpuSurface, request);
                Assert.NotNull(gpuFrame);
                Assert.True(gpuFrame.IsSynthetic);
                Assert.Equal(320, gpuFrame.PixelWidth);
                Assert.Equal(200, gpuFrame.PixelHeight);
                Assert.True(gpuFrame.PixelData.Length >= gpuFrame.BytesPerRow * gpuFrame.PixelHeight);
                Assert.True(gpuFrame.FrameId > 0);
                Assert.Equal(NativeWebViewRenderMode.GpuSurface, gpuFrame.RenderMode);
                Assert.Equal(NativeWebViewRenderFrameOrigin.SyntheticFallback, gpuFrame.Origin);
                Assert.True(gpuFrame.CapturedAtUtc <= DateTimeOffset.UtcNow);

                var offscreenFrame = await frameSource.CaptureFrameAsync(NativeWebViewRenderMode.Offscreen, request);
                Assert.NotNull(offscreenFrame);
                Assert.True(offscreenFrame.IsSynthetic);
                Assert.Equal(320, offscreenFrame.PixelWidth);
                Assert.Equal(200, offscreenFrame.PixelHeight);
                Assert.True(offscreenFrame.PixelData.Length >= offscreenFrame.BytesPerRow * offscreenFrame.PixelHeight);
                Assert.True(offscreenFrame.FrameId > gpuFrame.FrameId);
                Assert.Equal(NativeWebViewRenderMode.Offscreen, offscreenFrame.RenderMode);
                Assert.Equal(NativeWebViewRenderFrameOrigin.SyntheticFallback, offscreenFrame.Origin);
                Assert.True(offscreenFrame.CapturedAtUtc <= DateTimeOffset.UtcNow);
            }
        }
    }

    [Fact]
    public async Task FrameSource_EmbeddedModeCapture_ReturnsNull()
    {
        var backend = new WindowsNativeWebViewBackend();
        using (backend)
        {
            var frameSource = Assert.IsAssignableFrom<INativeWebViewFrameSource>(backend);

            var frame = await frameSource.CaptureFrameAsync(
                NativeWebViewRenderMode.Embedded,
                new NativeWebViewRenderFrameRequest { PixelWidth = 300, PixelHeight = 180 });

            Assert.Null(frame);
        }
    }
}
