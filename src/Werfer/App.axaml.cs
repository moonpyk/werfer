using System.Diagnostics;
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
    public IServiceProvider? Provider { get; private set; }

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
            // Services
            .AddSingleton<FlashMessageService>()
            // ViewModels
            .AddSingleton<TimerViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<MainWindowViewModel>()
            // Windows
            .AddSingleton<MainWindow>()
            ;

        Provider = services.BuildServiceProvider();

        Logger.Sink = new AvaloniaLogSink(
            Provider.GetRequiredService<ILoggerFactory>()
        );

        var crashLogger = Provider.GetRequiredService<ILogger<App>>();

        // Without these an exception on the UI thread kills the app with nothing logged
        // Qualified: inside an AvaloniaObject, Dispatcher is the instance property
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

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Provider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureLogging(ILoggingBuilder builder)
    {
        builder.ClearProviders()
            // A switch created by name only defaults to Off and lets nothing through
            .AddTraceSource(new SourceSwitch("slclockify")
            {
                Level = SourceLevels.Warning
            })
#if DEBUG
            .AddDebug()
#endif
            .AddSerilog(
                new LoggerConfiguration()
#if DEBUG
                    .MinimumLevel.Information()
#else
                    .MinimumLevel.Warning()
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
