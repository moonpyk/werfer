using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Werfer.DataProtection;
using Werfer.Services;
using Werfer.Settings;
using Werfer.Utils;
using Werfer.ViewModels;

namespace Werfer;

public class App : Application
{
    public IServiceProvider? Provider { get; private set; }

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
            .AddSingleton<FlashMessageService>()
            .AddSingleton<TimerViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<MainWindowViewModel>()
            ;

        Provider = services.BuildServiceProvider();

        Logger.Sink = new AvaloniaLogSink(
            Provider.GetRequiredService<ILoggerFactory>()
        );

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(
                Provider.GetRequiredService<MainWindowViewModel>()
            );
        }

        base.OnFrameworkInitializationCompleted();
    }
}
