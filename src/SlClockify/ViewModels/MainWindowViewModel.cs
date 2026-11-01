using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Threading;
using JetBrains.Annotations;
using Refit;
using SlClockify.Core;
using SlClockify.Core.Services;
using SlClockify.Utils;

namespace SlClockify.ViewModels;

[UsedImplicitly]
public class MainWindowViewModel : INotifyPropertyChanged
{
    private IDisposable? _ticker;

    public MainWindowViewModel(
        IApplicationSettings? applicationSettings,
        TimeTrackingService timeTracking
    )
    {
        if (!string.IsNullOrWhiteSpace(applicationSettings?.ApiKey))
        {
            ApiKey = applicationSettings.ApiKey;
        }

        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;
        // The running entry may change from another screen, the timer display follows it
        TimeTracking.RunningEntryChanged += (_, _) => OnRunningEntryChanged();
    }

    private IApplicationSettings? ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    // Only written by the TextBox, so no change notification needed
    public string ApiKey { get; set; } = string.Empty;

    public string? Status
    {
        get;
        private set => Set(ref field, value);
    }

    public bool IsBusy
    {
        get;
        private set => Set(ref field, value);
    }

    // Null until connected, the timer panel is hidden meanwhile
    public List<ProjectTaskChoice>? ProjectChoices
    {
        get;
        private set => Set(ref field, value);
    }

    public AutoCompleteFilterPredicate<object?> ChoiceFilter { get; } = AutoComplete.ProjectAutoCompleteFilterPredicate;

    public ProjectTaskChoice? SelectedChoice
    {
        get;
        set => Set(ref field, value);
    }

    // Free text, usually a Jira ticket key, pushed to the running entry when edited (on lost focus)
    public string? Description
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            Set(ref field, value);

            // Skips values coming from the running entry itself, a nested RunAsync would reset IsBusy mid-operation
            if (TimeTracking.RunningEntry is { } running && running.Description != value)
            {
                RunAsync(() => TimeTracking.UpdateDescriptionAsync(value))
                    .ContinueWith(t => t.Exception?.Handle(_ => true));
            }
        }
    }

    public bool IsRunning
    {
        get;
        private set => Set(ref field, value);
    }

    public string Elapsed
    {
        get;
        private set => Set(ref field, value);
    } = "00:00:00";

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task ConnectAsync()
    {
        return RunAsync(async () =>
        {
            var user = await TimeTracking.ConnectAsync(ApiKey);

            if (ApplicationSettings != null)
            {
                ApplicationSettings.ApiKey = ApiKey;
                ApplicationSettings.Save();
            }

            Status = $"Connected as {user.Name} ({user.Email})";

            ProjectChoices = (await TimeTracking.GetProjectTasksAsync())
                .Select(pt => new ProjectTaskChoice(pt.Project, pt.Task))
                // Projects sharing a color end up next to each other in the list
                .OrderBy(c => c.Project.Color)
                .ThenBy(c => c.ToString())
                .ToList();

            // Choices weren't loaded yet when ConnectAsync raised the change
            OnRunningEntryChanged();
        });
    }

    public Task StartAsync()
    {
        return RunAsync(async () =>
        {
            if (SelectedChoice is null)
            {
                Status = "Pick a project and a task first";
                return;
            }

            await TimeTracking.StartAsync(
                SelectedChoice.Project.Id,
                SelectedChoice.Task.Id,
                Description
            );
        });
    }

    public Task StopAsync()
    {
        return RunAsync(() => TimeTracking.StopAsync());
    }

    private void OnRunningEntryChanged()
    {
        _ticker?.Dispose();
        _ticker = null;

        var running = TimeTracking.RunningEntry;

        IsRunning = running is not null;

        if (running is null)
        {
            Elapsed = "00:00:00";
            return;
        }

        SelectedChoice = ProjectChoices?.Find(c => c.Task.Id == running.TaskId
        ) ?? SelectedChoice;
        Description = running.Description;

        var start = running.TimeInterval?.Start ?? DateTimeOffset.UtcNow;

        UpdateElapsed();

        _ticker = DispatcherTimer.Run(() =>
        {
            UpdateElapsed();
            return true;
        }, TimeSpan.FromSeconds(1));
        return;

        void UpdateElapsed()
        {
            Elapsed = (DateTimeOffset.UtcNow - start).ToString(@"hh\:mm\:ss");
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action();
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Status = "Invalid API key";
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
