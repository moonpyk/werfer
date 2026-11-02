using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using SlClockify.Core;
using SlClockify.Core.Services;

namespace SlClockify.ViewModels;

/// <summary>
///     Settings page: the Clockify API key, checked against Clockify before it is saved.
/// </summary>
[UsedImplicitly]
public class SettingsViewModel(
    ILogger<SettingsViewModel> logger,
    IApplicationSettings applicationSettings,
    TimeTrackingService timeTracking
) : ViewModelBase(logger)
{
    private IApplicationSettings ApplicationSettings { get; } = applicationSettings;
    private TimeTrackingService TimeTracking { get; } = timeTracking;

    // Only written by the TextBox, so no change notification needed
    public string ApiKey { get; set; } = applicationSettings.ApiKey ?? string.Empty;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    ///     Connects with the key and saves it only once Clockify accepted it.
    /// </summary>
    public Task ConnectAsync()
    {
        return WithBusyIndicator(async () =>
        {
            var user = await TimeTracking.ConnectAsync(ApiKey);

            ApplicationSettings.ApiKey = ApiKey;
            ApplicationSettings.Save();

            Status = $"{user.Email}";
        });
    }
}
