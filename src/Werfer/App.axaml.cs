using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Werfer.Services;
using Werfer.Settings;
using Werfer.Utils;
using Werfer.ViewModels;

namespace Werfer;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services
            .RegisterClockifyClient()
            .AddSingleton<IApplicationSettings>(sp =>
                ApplicationSettings.Current
                    .WithLogger(sp.GetRequiredService<ILogger<ApplicationSettings>>())
                    .Load()
            )
            .RegisterServices()
            .AddSingleton<FlashMessageService>()
            .AddSingleton<TimerViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<MainWindowViewModel>()
            ;

        var provider = services.BuildServiceProvider();

        Logger.Sink = new AvaloniaLogSink(
            provider.GetRequiredService<ILoggerFactory>()
        );

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(provider.GetRequiredService<MainWindowViewModel>());
        }

        base.OnFrameworkInitializationCompleted();
    }
}
