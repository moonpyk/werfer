using Avalonia;
using Velopack;

namespace Werfer;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // First thing: Velopack's installer and updater start the app with their own arguments (install, update,
        // uninstall hooks), handled here before anything else runs. Does nothing on a normal start
        VelopackApp.Build().Run();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace() // Will log to Trace until the real logger is set up.
#if DEBUG
            .WithDeveloperTools()
#endif
            ;
    }
}
