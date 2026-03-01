using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class RenderStatisticsTrackerTests
{
    [Fact]
    public void Tracker_RecordsAttemptsFailuresSkipsAndFrameSourceBreakdown()
    {
        var tracker = new NativeWebViewRenderStatisticsTracker();

        tracker.MarkCaptureSkipped("capture skipped");
        tracker.MarkCaptureAttempt();
        tracker.MarkCaptureFailure("capture failed");

        var syntheticFrame = CreateFrame(
            frameId: 41,
            renderMode: NativeWebViewRenderMode.GpuSurface,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback,
            isSynthetic: true);
        tracker.MarkCaptureAttempt();
        tracker.MarkCaptureSuccess(syntheticFrame);

        var nativeFrame = CreateFrame(
            frameId: 42,
            renderMode: NativeWebViewRenderMode.Offscreen,
            origin: NativeWebViewRenderFrameOrigin.NativeCapture,
            isSynthetic: false);
        tracker.MarkCaptureAttempt();
        tracker.MarkCaptureSuccess(nativeFrame);

        var snapshot = tracker.CreateSnapshot();
        Assert.Equal(3, snapshot.CaptureAttemptCount);
        Assert.Equal(2, snapshot.CaptureSuccessCount);
        Assert.Equal(1, snapshot.CaptureFailureCount);
        Assert.Equal(1, snapshot.CaptureSkippedCount);
        Assert.Equal(1, snapshot.SyntheticFrameCount);
        Assert.Equal(1, snapshot.NativeFrameCount);
        Assert.Equal(42, snapshot.LastFrameId);
        Assert.Equal(nativeFrame.CapturedAtUtc, snapshot.LastFrameCapturedAtUtc);
        Assert.Equal(NativeWebViewRenderMode.Offscreen, snapshot.LastFrameRenderMode);
        Assert.Equal(NativeWebViewRenderFrameOrigin.NativeCapture, snapshot.LastFrameOrigin);
        Assert.Null(snapshot.LastFailureMessage);
        Assert.Null(snapshot.LastFailureAtUtc);
    }

    [Fact]
    public void Tracker_Reset_ClearsAllState()
    {
        var tracker = new NativeWebViewRenderStatisticsTracker();

        tracker.MarkCaptureSkipped("initial skip");
        tracker.MarkCaptureAttempt();
        tracker.MarkCaptureFailure("initial failure");
        tracker.MarkCaptureAttempt();
        tracker.MarkCaptureSuccess(CreateFrame(
            frameId: 9,
            renderMode: NativeWebViewRenderMode.GpuSurface,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback,
            isSynthetic: true));

        tracker.Reset();
        var snapshot = tracker.CreateSnapshot();

        Assert.Equal(0, snapshot.CaptureAttemptCount);
        Assert.Equal(0, snapshot.CaptureSuccessCount);
        Assert.Equal(0, snapshot.CaptureFailureCount);
        Assert.Equal(0, snapshot.CaptureSkippedCount);
        Assert.Equal(0, snapshot.SyntheticFrameCount);
        Assert.Equal(0, snapshot.NativeFrameCount);
        Assert.Equal(0, snapshot.LastFrameId);
        Assert.Null(snapshot.LastFrameCapturedAtUtc);
        Assert.Equal(NativeWebViewRenderMode.Embedded, snapshot.LastFrameRenderMode);
        Assert.Equal(NativeWebViewRenderFrameOrigin.Unknown, snapshot.LastFrameOrigin);
        Assert.Null(snapshot.LastFailureMessage);
        Assert.Null(snapshot.LastFailureAtUtc);
    }

    private static NativeWebViewRenderFrame CreateFrame(
        long frameId,
        NativeWebViewRenderMode renderMode,
        NativeWebViewRenderFrameOrigin origin,
        bool isSynthetic)
    {
        return new NativeWebViewRenderFrame(
            pixelWidth: 4,
            pixelHeight: 2,
            bytesPerRow: 16,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: new byte[32],
            isSynthetic: isSynthetic,
            frameId: frameId,
            capturedAtUtc: DateTimeOffset.UtcNow,
            renderMode: renderMode,
            origin: origin);
    }
}
