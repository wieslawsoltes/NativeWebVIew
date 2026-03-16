namespace NativeWebView.Interop;

public readonly record struct NativePlatformHandle(nint Handle, string HandleDescriptor);

public interface IPlatformHandleProvider
{
    bool TryGetPlatformHandle(out NativePlatformHandle handle);
}

public interface INativeWebViewPlatformHandleProvider : IPlatformHandleProvider
{
    bool TryGetViewHandle(out NativePlatformHandle handle);

    bool TryGetControllerHandle(out NativePlatformHandle handle);
}

public interface INativeWebViewManagedControlHandleProvider
{
    object CreateManagedControlHandle();

    void ReleaseManagedControlHandle(object? handle);
}

public interface INativeWebDialogPlatformHandleProvider : IPlatformHandleProvider
{
    bool TryGetDialogHandle(out NativePlatformHandle handle);

    bool TryGetHostWindowHandle(out NativePlatformHandle handle);
}

public interface INativeWebViewNativeControlAttachment
{
    NativePlatformHandle AttachToNativeParent(NativePlatformHandle parentHandle);

    void DetachFromNativeParent();
}
