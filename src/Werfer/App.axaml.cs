using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Velopack;
using Velopack.Sources;
using Werfer.DataProtection;
using Werfer.Services;
using Werfer.Settings;
using Werfer.Utils;
using Werfer.Utils.Taskbar;
using Werfer.ViewModels;

namespace Werfer;

public class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public static string ApplicationPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "werfer"
#if DEBUG
        + "-debug"
#endif
    );

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        Logger.Sink = new AvaloniaLogSink(
            Services.GetRequiredService<ILoggerFactory>()
        );

        var crashLogger = Services.GetRequiredService<ILogger<App>>();

        // Without these an exception on the UI thread kills the app with nothing logged
        // Dispatcher.UIThread written in full: inside an AvaloniaObject, Dispatcher alone means the instance property
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            crashLogger.LogCritical(
                e.Exception,
                "Unhandled UI exception"
            );
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            crashLogger.LogCritical(
                e.ExceptionObject as Exception,
                "Unhandled exception"
            );
        };

        // For the tray icon, shown only while MinimizeToTray is on
        DataContext = Services.GetRequiredService<SettingsViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

            // In the background: a newer release is announced, never downloaded without asking
            _ = Services.GetRequiredService<UpdateViewModel>().CheckAtStartupAsync();

            if (TrayIcon.GetIcons(this) is [var tray])
            {
                tray.FollowTaskbarTheme(
                    Services.GetRequiredService<ITaskbarThemeProvider>(),
                    desktop.MainWindow
                );
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    // The stable releases of the GitHub repository. WERFER_UPDATE_SOURCE, a folder or URL of Velopack releases (vpk
    // pack's output), replaces them: to try an update locally, without publishing a release
    private static UpdateManager CreateUpdateManager(ILogger<App> logger)
    {
        var source = Environment.GetEnvironmentVariable("WERFER_UPDATE_SOURCE");

        if (string.IsNullOrWhiteSpace(source))
        {
            return new UpdateManager(new GithubSource("https://github.com/moonpyk/werfer", null, false));
        }

        logger.LogInformation("Updates from {Source}, set by WERFER_UPDATE_SOURCE", source);
        return new UpdateManager(source);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services
            .RegisterClockifyClient()
            .AddSingleton<IDataProtectionProvider>(_ => OperatingSystem.IsWindows()
                ? WindowsDataProtectionProvider.Instance
                : PassThroughDataProtectionProvider.Instance
            )
            .AddSingleton<ITaskbarThemeProvider>(_ => OperatingSystem.IsWindows()
                ? WindowsTaskbarThemeProvider.Instance
                : DefaultTaskbarThemeProvider.Instance
            )
            .AddSingleton<IApplicationSettings>(sp =>
                new ApplicationSettings(
                    ApplicationPath,
                    sp.GetRequiredService<IDataProtectionProvider>(),
                    sp.GetRequiredService<ILogger<ApplicationSettings>>()
                ).Load()
            )
            .AddSingleton(sp => CreateUpdateManager(sp.GetRequiredService<ILogger<App>>()))
            .RegisterServices()
            .AddLogging(ConfigureLogging)
            .AddHttpClient()
            // Services
            .AddSingleton<FlashMessageService>()
            // ViewModels
            .AddSingleton<TimerViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<UpdateViewModel>()
            .AddSingleton<MainWindowViewModel>()
            // Windows
            .AddSingleton<MainWindow>()
            ;

        return services.BuildServiceProvider();
    }

    private static void ConfigureLogging(ILoggingBuilder builder)
    {
        builder.ClearProviders()
            .AddSerilog(
                new LoggerConfiguration()
#if DEBUG
                    .MinimumLevel.Information()
#else
                    .MinimumLevel.Warning()
#endif
#if DEBUG
                    .WriteTo.Debug()
#else
                    .WriteTo.Trace()
#endif
                    .WriteTo.File(
                        Path.Combine(ApplicationPath, "logs", "werfer-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 2
                    )
                    .CreateLogger(),
                /* dispose: */true
            );
    }

    private void OnTrayIconClick(object? sender, EventArgs e)
    {
        Services?.GetService<MainWindow>()?.ToggleFromTray();
    }

    private void OnTrayShowClick(object? sender, EventArgs e)
    {
        Services?.GetService<MainWindow>()?.ShowFromTray();
    }

    // Through the window's own close: the confirmation and the window position saving still apply. Shown first only
    // for the confirmation, a dialog can't open on a hidden window
    private void OnTrayExitClick(object? sender, EventArgs e)
    {
        var window = Services?.GetService<MainWindow>();
        if (window == null)
        {
            return;
        }

        if (Services?.GetService<IApplicationSettings>()?.ConfirmClose == true)
        {
            window.ShowFromTray();
        }

        window.Close();
    }
}
