using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Refit;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     Change notification plus the busy flag and error reporting shared by every page.
/// </summary>
public abstract class ViewModelBase(
    ILogger logger,
    FlashMessageService flashMessages
) : INotifyPropertyChanged
{
    /// <summary>
    ///     Shown when Clockify rejects the API key, cleared once a key is accepted.
    /// </summary>
    protected static readonly FlashMessage InvalidApiKey = new(
        "Check your Clockify API key in Settings",
        "Invalid API key",
        FAInfoBarSeverity.Error
    );

    protected ILogger Logger { get; } = logger;

    public FlashMessageService FlashMessages { get; } = flashMessages;

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
    ///     Runs an operation with <see cref="IsBusy" /> set, turning any failure into an error flash message.
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
            FlashMessages.Push(InvalidApiKey);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "While running operation");
            FlashMessages.Push(new FlashMessage(e.Message, Severity: FAInfoBarSeverity.Error));
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
