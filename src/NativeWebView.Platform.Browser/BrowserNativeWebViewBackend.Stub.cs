using NativeWebView.Core;
using NativeWebView.Interop;

namespace NativeWebView.Platform.Browser;

public sealed class BrowserNativeWebViewBackend : NativeWebViewBackendStubBase, INativeWebViewPlatformHandleProvider
{
    private static long s_nextSyntheticHandle = 0x6000;

    private readonly NativePlatformHandle _platformHandle;
    private readonly NativePlatformHandle _viewHandle;
    private readonly NativePlatformHandle _controllerHandle;

    public BrowserNativeWebViewBackend()
        : base(NativeWebViewPlatform.Browser, BrowserPlatformFeatures.Instance)
    {
        var handleSeed = Interlocked.Add(ref s_nextSyntheticHandle, 3);
        _platformHandle = new NativePlatformHandle((nint)(handleSeed - 2), "Window");
        _viewHandle = new NativePlatformHandle((nint)(handleSeed - 1), "HTMLIFrameElement");
        _controllerHandle = new NativePlatformHandle((nint)handleSeed, "Window");
    }

    public bool TryGetPlatformHandle(out NativePlatformHandle handle)
    {
        handle = _platformHandle;
        return true;
    }

    public bool TryGetViewHandle(out NativePlatformHandle handle)
    {
        handle = _viewHandle;
        return true;
    }

    public bool TryGetControllerHandle(out NativePlatformHandle handle)
    {
        handle = _controllerHandle;
        return true;
    }
}
