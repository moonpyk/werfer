using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Werfer.DataProtection;
using Werfer.Services;
using Werfer.Settings;
using Werfer.Utils;
using Werfer.ViewModels;

namespace Werfer;

public class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public static string ApplicationPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "werfer"
#if DEBUG
        , "debug"
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
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayShowClick(object? sender, EventArgs e)
    {
        Services?.GetService<MainWindow>()?.ShowFromTray();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services
            .RegisterClockifyClient()
            .AddSingleton<IDataProtectionProvider>(_ =>
            {
                return Environment.OSVersion.Platform switch
                {
                    PlatformID.Win32NT => new WindowsDataProtectionProvider(),
                    _ => PassThroughDataProtectionProvider.Instance
                };
            })
            .AddSingleton<IApplicationSettings>(sp =>
                ApplicationSettings.Current
                    .WithLogger(sp.GetRequiredService<ILogger<ApplicationSettings>>())
                    .WithDataProtectionProvider(sp.GetRequiredService<IDataProtectionProvider>())
                    .Load()
            )
            .RegisterServices()
            .AddLogging(ConfigureLogging)
            .AddHttpClient()
            // Services
            .AddSingleton<FlashMessageService>()
            // ViewModels
            .AddSingleton<TimerViewModel>()
            .AddSingleton<SettingsViewModel>()
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
}
