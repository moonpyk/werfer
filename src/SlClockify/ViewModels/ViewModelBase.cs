using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Refit;

namespace SlClockify.ViewModels;

/// <summary>
///     Change notification plus the busy flag and error status shared by every page.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public string? Status
    {
        get;
        protected set => Set(ref field, value);
    }

    public bool IsBusy
    {
        get;
        private set => Set(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Runs an operation with <see cref="IsBusy" /> set, turning any failure into <see cref="Status" />.
    /// </summary>
    protected async Task RunAsync(Func<Task> action)
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

    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
