using Avalonia;
using NativeWebView.Platform.Windows;
using System.Reflection;

namespace NativeWebView.Core.Tests;

public sealed class NativeWebViewRenderCaptureBehaviorTests
{
    [Fact]
    public async Task CaptureRenderFrameAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var backend = new WindowsNativeWebViewBackend();
        using var webView = new NativeWebView.Controls.NativeWebView(backend)
        {
            RenderMode = NativeWebViewRenderMode.GpuSurface,
        };

        webView.Measure(new Size(320, 200));
        webView.Arrange(new Rect(0, 0, 320, 200));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => webView.CaptureRenderFrameAsync(cancellation.Token));
    }

    [Fact]
    public async Task SaveRenderFrameAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var backend = new WindowsNativeWebViewBackend();
        using var webView = new NativeWebView.Controls.NativeWebView(backend)
        {
            RenderMode = NativeWebViewRenderMode.GpuSurface,
        };

        webView.Measure(new Size(320, 200));
        webView.Arrange(new Rect(0, 0, 320, 200));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            webView.SaveRenderFrameAsync("artifacts/tests/should-not-write.png", cancellation.Token));
    }

    [Fact]
    public async Task CaptureRenderFrameAsync_EventHandlerException_DoesNotFailCapture()
    {
        using var backend = new WindowsNativeWebViewBackend();
        using var webView = new NativeWebView.Controls.NativeWebView(backend)
        {
            RenderMode = NativeWebViewRenderMode.GpuSurface,
        };

        webView.Measure(new Size(320, 200));
        webView.Arrange(new Rect(0, 0, 320, 200));

        webView.RenderFrameCaptured += static (_, _) =>
        {
            throw new InvalidOperationException("handler failure");
        };

        var frame = new NativeWebViewRenderFrame(
            pixelWidth: 4,
            pixelHeight: 2,
            bytesPerRow: 16,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: new byte[32],
            isSynthetic: true,
            frameId: 1,
            capturedAtUtc: DateTimeOffset.UtcNow,
            renderMode: NativeWebViewRenderMode.GpuSurface,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback);

        var method = typeof(NativeWebView.Controls.NativeWebView).GetMethod(
            "RaiseRenderFrameCaptured",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var exception = Record.Exception(() =>
        {
            method!.Invoke(webView, [frame]);
        }

        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveRenderFrameWithMetadataAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var backend = new WindowsNativeWebViewBackend();
        using var webView = new NativeWebView.Controls.NativeWebView(backend)
        {
            RenderMode = NativeWebViewRenderMode.GpuSurface,
        };

        webView.Measure(new Size(320, 200));
        webView.Arrange(new Rect(0, 0, 320, 200));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            webView.SaveRenderFrameWithMetadataAsync(
                "artifacts/tests/should-not-write-frame.png",
                "artifacts/tests/should-not-write-frame.json",
                cancellation.Token));
    }

    [Fact]
    public async Task SaveRenderFrameWithMetadataAsync_EmbeddedMode_ReturnsFalse()
    {
        using var backend = new WindowsNativeWebViewBackend();
        using var webView = new NativeWebView.Controls.NativeWebView(backend)
        {
            RenderMode = NativeWebViewRenderMode.Embedded,
        };

        var outputDirectory = Path.Combine(Path.GetTempPath(), "NativeWebView.Tests", Guid.NewGuid().ToString("N"));
        var imagePath = Path.Combine(outputDirectory, "frame.png");
        var metadataPath = Path.Combine(outputDirectory, "frame.json");

        try
        {
            var saved = await webView.SaveRenderFrameWithMetadataAsync(imagePath, metadataPath);

            Assert.False(saved);
            Assert.False(File.Exists(imagePath));
            Assert.False(File.Exists(metadataPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
