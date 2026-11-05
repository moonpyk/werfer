using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     Settings page: the Clockify API key, checked against Clockify before it is saved.
/// </summary>
[UsedImplicitly]
public class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel(ILogger<SettingsViewModel> logger,
        IApplicationSettings applicationSettings,
        FlashMessageService flashMessages,
        TimeTrackingService timeTracking
    ) : base(logger, flashMessages)
    {
        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;
        ApiKey = applicationSettings.ApiKey ?? string.Empty;
    }

    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    public string ApiKey
    {
        get;
        set
        {
            field = value;
            NotifyPropertyChanged(nameof(HasApiKey));

            // Only raised here: a typed key isn't verified yet, ConnectAsync clears it once Clockify accepts one
            if (!HasApiKey)
            {
                NeedsAttention = true;
            }
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public DayOfWeek[] DaysOfWeek { get; } = Enum.GetValues<DayOfWeek>();

    /// <summary>
    ///     First day of the history's week, saved as soon as picked; the timer page reloads its history on change
    /// </summary>
    public DayOfWeek WeekStart
    {
        get => ApplicationSettings.WeekStart;
        set
        {
            if (value == ApplicationSettings.WeekStart)
            {
                return;
            }

            ApplicationSettings.WeekStart = value;
            NotifyPropertyChanged();

            if (!ApplicationSettings.TrySave())
            {
                FlashMessages.Push(WellKnownMessages.SettingsNotSaved);
            }
        }
    }

    /// <summary>
    ///     Shows a badge on the settings item: no key, or the last connection failed.
    /// </summary>
    public bool NeedsAttention
    {
        get;
        private set => Set(ref field, value);
    }

    /// <summary>
    ///     Connects with the key and saves it only once Clockify accepted it.
    /// </summary>
    public Task ConnectAsync()
    {
        return WithBusyIndicator(async () =>
        {
            try
            {
                await TimeTracking.ConnectAsync(ApiKey);
            }
            catch
            {
                NeedsAttention = true;
                throw;
            }

            NeedsAttention = false;

            ApplicationSettings.ApiKey = ApiKey;
            if (!ApplicationSettings.TrySave())
            {
                FlashMessages.Push(WellKnownMessages.SettingsNotSaved);
            }
        });
    }
}
