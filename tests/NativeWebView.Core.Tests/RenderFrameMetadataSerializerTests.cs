using System.Text.Json;
using System.Security.Cryptography;
using NativeWebView.Core;

namespace NativeWebView.Core.Tests;

public sealed class RenderFrameMetadataSerializerTests
{
    [Fact]
    public async Task Serializer_CreateAndWrite_ProducesExpectedMetadataPayload()
    {
        var pixelData = new byte[200];
        for (var i = 0; i < pixelData.Length; i++)
        {
            pixelData[i] = (byte)(i & 0xFF);
        }

        var frame = new NativeWebViewRenderFrame(
            pixelWidth: 8,
            pixelHeight: 6,
            bytesPerRow: 32,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: pixelData,
            isSynthetic: true,
            frameId: 77,
            capturedAtUtc: DateTimeOffset.Parse("2026-03-01T12:00:00+00:00"),
            renderMode: NativeWebViewRenderMode.Offscreen,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback);

        var stats = new NativeWebViewRenderStatistics(
            captureAttemptCount: 9,
            captureSuccessCount: 7,
            captureFailureCount: 1,
            captureSkippedCount: 1,
            syntheticFrameCount: 6,
            nativeFrameCount: 1,
            lastFrameId: 77,
            lastFrameCapturedAtUtc: frame.CapturedAtUtc,
            lastFrameRenderMode: NativeWebViewRenderMode.Offscreen,
            lastFrameOrigin: NativeWebViewRenderFrameOrigin.SyntheticFallback,
            lastFailureMessage: null,
            lastFailureAtUtc: null);

        var metadata = NativeWebViewRenderFrameMetadataSerializer.Create(
            frame,
            stats,
            platform: NativeWebViewPlatform.Windows,
            renderMode: NativeWebViewRenderMode.Offscreen,
            renderFramesPerSecond: 25,
            isUsingSyntheticFrameSource: true,
            renderDiagnosticsMessage: "fallback synthetic",
            currentUrl: new Uri("https://example.com/"));

        Assert.Equal(NativeWebViewRenderFrameMetadataSerializer.CurrentFormatVersion, metadata.FormatVersion);
        Assert.Equal(77, metadata.FrameId);
        Assert.Equal(NativeWebViewPlatform.Windows, metadata.Platform);
        Assert.Equal(NativeWebViewRenderMode.Offscreen, metadata.RenderMode);
        Assert.Equal(25, metadata.RenderFramesPerSecond);
        Assert.True(metadata.IsUsingSyntheticFrameSource);
        Assert.Equal("fallback synthetic", metadata.RenderDiagnosticsMessage);
        Assert.Equal("https://example.com/", metadata.CurrentUrl?.ToString());
        Assert.Equal(9, metadata.Statistics.CaptureAttemptCount);
        Assert.Equal(7, metadata.Statistics.CaptureSuccessCount);
        Assert.Equal(192, metadata.PixelDataLength);
        var expectedHash = Convert.ToHexString(SHA256.HashData(pixelData.AsSpan(0, 192)));
        Assert.Equal(expectedHash, metadata.PixelDataSha256);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "NativeWebView.Tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputDirectory, "metadata.json");

        try
        {
            await NativeWebViewRenderFrameMetadataSerializer.WriteToFileAsync(metadata, outputPath);

            Assert.True(File.Exists(outputPath));

            await using var stream = File.OpenRead(outputPath);
            using var document = await JsonDocument.ParseAsync(stream);
            var root = document.RootElement;

            Assert.Equal(
                NativeWebViewRenderFrameMetadataSerializer.CurrentFormatVersion,
                root.GetProperty("FormatVersion").GetString());
            Assert.Equal(77, root.GetProperty("FrameId").GetInt64());
            Assert.Equal(8, root.GetProperty("PixelWidth").GetInt32());
            Assert.Equal(6, root.GetProperty("PixelHeight").GetInt32());
            Assert.Equal("https://example.com/", root.GetProperty("CurrentUrl").GetString());
            Assert.Equal(192, root.GetProperty("PixelDataLength").GetInt64());
            Assert.Equal(expectedHash, root.GetProperty("PixelDataSha256").GetString());

            var statistics = root.GetProperty("Statistics");
            Assert.Equal(9, statistics.GetProperty("CaptureAttemptCount").GetInt64());
            Assert.Equal(7, statistics.GetProperty("CaptureSuccessCount").GetInt64());
            Assert.Equal(1, statistics.GetProperty("CaptureFailureCount").GetInt64());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Serializer_Create_BgraHash_IgnoresStridePaddingBytes()
    {
        var frame1Buffer = new byte[32];
        var frame2Buffer = new byte[32];

        // Two rows, visible bytes are first 8 bytes of each row (width=2, BGRA8888).
        frame1Buffer.AsSpan(0, 8).Fill(0x11);
        frame1Buffer.AsSpan(16, 8).Fill(0x22);
        frame2Buffer.AsSpan(0, 8).Fill(0x11);
        frame2Buffer.AsSpan(16, 8).Fill(0x22);

        // Padding bytes differ intentionally and should not change hash.
        frame1Buffer.AsSpan(8, 8).Fill(0xA1);
        frame1Buffer.AsSpan(24, 8).Fill(0xA2);
        frame2Buffer.AsSpan(8, 8).Fill(0xB1);
        frame2Buffer.AsSpan(24, 8).Fill(0xB2);

        var frame1 = new NativeWebViewRenderFrame(
            pixelWidth: 2,
            pixelHeight: 2,
            bytesPerRow: 16,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: frame1Buffer,
            frameId: 1,
            renderMode: NativeWebViewRenderMode.Offscreen,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback);

        var frame2 = new NativeWebViewRenderFrame(
            pixelWidth: 2,
            pixelHeight: 2,
            bytesPerRow: 16,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: frame2Buffer,
            frameId: 2,
            renderMode: NativeWebViewRenderMode.Offscreen,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback);

        var stats = new NativeWebViewRenderStatistics(0, 0, 0, 0, 0, 0, 0, null, NativeWebViewRenderMode.Embedded, NativeWebViewRenderFrameOrigin.Unknown, null, null);

        var metadata1 = NativeWebViewRenderFrameMetadataSerializer.Create(
            frame1,
            stats,
            platform: NativeWebViewPlatform.Windows,
            renderMode: NativeWebViewRenderMode.Offscreen,
            renderFramesPerSecond: 30,
            isUsingSyntheticFrameSource: true,
            renderDiagnosticsMessage: null,
            currentUrl: null);

        var metadata2 = NativeWebViewRenderFrameMetadataSerializer.Create(
            frame2,
            stats,
            platform: NativeWebViewPlatform.Windows,
            renderMode: NativeWebViewRenderMode.Offscreen,
            renderFramesPerSecond: 30,
            isUsingSyntheticFrameSource: true,
            renderDiagnosticsMessage: null,
            currentUrl: null);

        Assert.Equal(16, metadata1.PixelDataLength);
        Assert.Equal(16, metadata2.PixelDataLength);
        Assert.Equal(metadata1.PixelDataSha256, metadata2.PixelDataSha256);
    }

    [Fact]
    public async Task Serializer_ReadFromFileAsync_RoundTripsPayload()
    {
        var frame = new NativeWebViewRenderFrame(
            pixelWidth: 3,
            pixelHeight: 2,
            bytesPerRow: 12,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: new byte[24],
            isSynthetic: true,
            frameId: 12,
            capturedAtUtc: DateTimeOffset.Parse("2026-03-01T13:00:00+00:00"),
            renderMode: NativeWebViewRenderMode.GpuSurface,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback);

        var stats = new NativeWebViewRenderStatistics(1, 1, 0, 0, 1, 0, 12, frame.CapturedAtUtc, NativeWebViewRenderMode.GpuSurface, NativeWebViewRenderFrameOrigin.SyntheticFallback, null, null);
        var metadata = NativeWebViewRenderFrameMetadataSerializer.Create(
            frame,
            stats,
            platform: NativeWebViewPlatform.Windows,
            renderMode: NativeWebViewRenderMode.GpuSurface,
            renderFramesPerSecond: 30,
            isUsingSyntheticFrameSource: true,
            renderDiagnosticsMessage: null,
            currentUrl: new Uri("https://example.com/roundtrip"));

        var outputDirectory = Path.Combine(Path.GetTempPath(), "NativeWebView.Tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputDirectory, "metadata-roundtrip.json");

        try
        {
            await NativeWebViewRenderFrameMetadataSerializer.WriteToFileAsync(metadata, outputPath);
            var reloaded = await NativeWebViewRenderFrameMetadataSerializer.ReadFromFileAsync(outputPath);

            Assert.Equal(NativeWebViewRenderFrameMetadataSerializer.CurrentFormatVersion, reloaded.FormatVersion);
            Assert.Equal(metadata.FrameId, reloaded.FrameId);
            Assert.Equal(metadata.PixelDataLength, reloaded.PixelDataLength);
            Assert.Equal(metadata.PixelDataSha256, reloaded.PixelDataSha256);
            Assert.Equal(metadata.Statistics.CaptureSuccessCount, reloaded.Statistics.CaptureSuccessCount);
            Assert.Equal("https://example.com/roundtrip", reloaded.CurrentUrl?.ToString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Serializer_TryVerifyIntegrity_ReturnsTrueForMatchingMetadata_AndFalseForMismatch()
    {
        var frame = new NativeWebViewRenderFrame(
            pixelWidth: 2,
            pixelHeight: 2,
            bytesPerRow: 8,
            pixelFormat: NativeWebViewRenderPixelFormat.Bgra8888Premultiplied,
            pixelData: new byte[]
            {
                1,2,3,4, 5,6,7,8,
                9,10,11,12, 13,14,15,16
            },
            isSynthetic: true,
            frameId: 30,
            renderMode: NativeWebViewRenderMode.Offscreen,
            origin: NativeWebViewRenderFrameOrigin.SyntheticFallback);

        var stats = new NativeWebViewRenderStatistics(0, 0, 0, 0, 0, 0, 0, null, NativeWebViewRenderMode.Embedded, NativeWebViewRenderFrameOrigin.Unknown, null, null);
        var metadata = NativeWebViewRenderFrameMetadataSerializer.Create(
            frame,
            stats,
            platform: NativeWebViewPlatform.Windows,
            renderMode: NativeWebViewRenderMode.Offscreen,
            renderFramesPerSecond: 30,
            isUsingSyntheticFrameSource: true,
            renderDiagnosticsMessage: null,
            currentUrl: null);

        Assert.True(NativeWebViewRenderFrameMetadataSerializer.TryVerifyIntegrity(frame, metadata, out var validError));
        Assert.Null(validError);

        var invalidMetadata = new NativeWebViewRenderFrameExportMetadata
        {
            FormatVersion = metadata.FormatVersion,
            ExportedAtUtc = metadata.ExportedAtUtc,
            Platform = metadata.Platform,
            RenderMode = metadata.RenderMode,
            RenderFramesPerSecond = metadata.RenderFramesPerSecond,
            IsUsingSyntheticFrameSource = metadata.IsUsingSyntheticFrameSource,
            RenderDiagnosticsMessage = metadata.RenderDiagnosticsMessage,
            CurrentUrl = metadata.CurrentUrl,
            FrameId = metadata.FrameId,
            CapturedAtUtc = metadata.CapturedAtUtc,
            Origin = metadata.Origin,
            IsSynthetic = metadata.IsSynthetic,
            PixelWidth = metadata.PixelWidth,
            PixelHeight = metadata.PixelHeight,
            BytesPerRow = metadata.BytesPerRow,
            PixelFormat = metadata.PixelFormat,
            PixelDataLength = metadata.PixelDataLength,
            PixelDataSha256 = "BADHASH",
            Statistics = metadata.Statistics,
        };
        Assert.False(NativeWebViewRenderFrameMetadataSerializer.TryVerifyIntegrity(frame, invalidMetadata, out var invalidError));
        Assert.False(string.IsNullOrWhiteSpace(invalidError));

        var unsupportedVersionMetadata = new NativeWebViewRenderFrameExportMetadata
        {
            FormatVersion = "1",
            ExportedAtUtc = metadata.ExportedAtUtc,
            Platform = metadata.Platform,
            RenderMode = metadata.RenderMode,
            RenderFramesPerSecond = metadata.RenderFramesPerSecond,
            IsUsingSyntheticFrameSource = metadata.IsUsingSyntheticFrameSource,
            RenderDiagnosticsMessage = metadata.RenderDiagnosticsMessage,
            CurrentUrl = metadata.CurrentUrl,
            FrameId = metadata.FrameId,
            CapturedAtUtc = metadata.CapturedAtUtc,
            Origin = metadata.Origin,
            IsSynthetic = metadata.IsSynthetic,
            PixelWidth = metadata.PixelWidth,
            PixelHeight = metadata.PixelHeight,
            BytesPerRow = metadata.BytesPerRow,
            PixelFormat = metadata.PixelFormat,
            PixelDataLength = metadata.PixelDataLength,
            PixelDataSha256 = metadata.PixelDataSha256,
            Statistics = metadata.Statistics,
        };

        Assert.False(NativeWebViewRenderFrameMetadataSerializer.TryVerifyIntegrity(frame, unsupportedVersionMetadata, out var unsupportedVersionError));
        Assert.Contains("Unsupported metadata format version", unsupportedVersionError, StringComparison.Ordinal);
    }
}
