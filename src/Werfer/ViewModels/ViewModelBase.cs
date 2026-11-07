using System.Diagnostics.CodeAnalysis;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Refit;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     Change notification plus the busy flag and error reporting shared by every page.
/// </summary>
public abstract partial class ViewModelBase(
    ILogger logger,
    FlashMessageService flashMessages
) : ObservableObject
{
    // Operations can overlap (a history reload during a stop): busy until the last one ends
    private int _busyCount;

    [SuppressMessage(
        "ReSharper",
        "MemberCanBePrivate.Global",
        Justification = "Is useful to have on hand"
    )]
    protected ILogger Logger { get; } = logger;

    public FlashMessageService FlashMessages { get; } = flashMessages;

    /// <summary>
    ///     Indicates whether a background process or operation is currently in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    /// <summary>
    ///     Runs an operation with <see cref="IsBusy" /> set, turning any failure into an error flash message.
    /// </summary>
    protected async Task WithBusyIndicator(Func<Task> action)
    {
        IsBusy = Interlocked.Increment(ref _busyCount) > 0;

        try
        {
            await action();
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Logger.LogWarning(e, "Invalid API key");
            FlashMessages.ReplaceWith(WellKnownMessages.InvalidApiKey);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "While running operation");
            FlashMessages.ReplaceWith(new FlashMessage(e.Message, Severity: FAInfoBarSeverity.Error));
        }
        finally
        {
            IsBusy = Interlocked.Decrement(ref _busyCount) > 0;
        }
    }
}
