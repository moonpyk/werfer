using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlClockify.Core;
using SlClockify.Settings;
using SlClockify.Utils;
using SlClockify.ViewModels;

namespace SlClockify;

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
            .AddTransient<TimerViewModel>()
            .AddTransient<SettingsViewModel>()
            .AddTransient<MainWindowViewModel>()
            ;

        var provider = services.BuildServiceProvider();

        Logger.Sink = new AvaloniaLogSink(
            provider.GetRequiredService<ILoggerFactory>()
        );

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
