using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Refit;

namespace SlClockify.ViewModels;

/// <summary>
///     Change notification plus the busy flag and error status shared by every page.
/// </summary>
public abstract class ViewModelBase(
    ILogger logger
) : INotifyPropertyChanged
{
    protected ILogger Logger { get; } = logger;

    /// <summary>
    ///     Represents the current status message, typically used to display information or error messages.
    /// </summary>
    public string? Status
    {
        get;
        protected set => Set(ref field, value);
    }

    /// <summary>
    ///     Indicates whether a background process or operation is currently in progress.
    /// </summary>
    public bool IsBusy
    {
        get;
        private set => Set(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Runs an operation with <see cref="IsBusy" /> set, turning any failure into <see cref="Status" />.
    /// </summary>
    protected async Task WithBusyIndicator(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action();
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Logger.LogWarning(e, "Invalid API key");
            Status = "Invalid API key";
        }
        catch (Exception e)
        {
            Logger.LogError(e, "While running operation");
            Status = e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        NotifyPropertyChanged(name);
    }

    protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
