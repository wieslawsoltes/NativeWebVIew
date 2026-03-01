using Avalonia;

namespace NativeWebView.Sample.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(static arg => string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase)))
        {
            return DesktopSampleSmokeRunner.RunAsync().GetAwaiter().GetResult();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
