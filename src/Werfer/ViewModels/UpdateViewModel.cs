using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Velopack;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     Updates from the GitHub releases, through Velopack, in the More menu: "Check for updates", or "Download
///     &lt;version&gt;" once one is found. A downloaded update is installed when the app closes, never while it runs.
///     Only for an app installed or unzipped from a Velopack package: a debug run, outside Velopack, has nothing to
///     update.
/// </summary>
public partial class UpdateViewModel : ViewModelBase
{
    /// <summary>
    ///     Creates the menu entries on the app's updater, see App.CreateUpdateManager.
    /// </summary>
    public UpdateViewModel(
        ILogger<UpdateViewModel> logger,
        FlashMessageService flashMessages,
        UpdateManager manager
    ) : base(logger, flashMessages)
    {
        Manager = manager;
    }

    private UpdateManager Manager { get; }

    /// <summary>
    ///     True for an app installed by Velopack's setup: the menu entries are hidden otherwise.
    /// </summary>
    public bool IsInstalled => Manager.IsInstalled;

    /// <summary>
    ///     The newer release found, null until one is.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(AvailableVersion),
        nameof(CanCheck),
        nameof(CanDownload)
    )]
    public partial UpdateInfo? Available { get; private set; }

    /// <summary>
    ///     True once <see cref="Available" /> is downloaded: it waits for the app to close.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDownload))]
    public partial bool IsDownloaded { get; private set; }

    /// <summary>
    ///     The version of <see cref="Available" />, "v" included, as the About page shows its own: "Download v0.7.0".
    /// </summary>
    public string? AvailableVersion => Available is { } update ? $"v{update.TargetFullRelease.Version}" : null;

    /// <summary>
    ///     True while no update is known: the menu shows "Check for updates".
    /// </summary>
    public bool CanCheck => IsInstalled && Available is null;

    /// <summary>
    ///     True while an update is known but not downloaded: the menu shows "Download &lt;version&gt;".
    /// </summary>
    public bool CanDownload => IsInstalled && Available is not null && !IsDownloaded;

    /// <summary>
    ///     The check at startup. Silent when it fails, offline for example: the menu can check again later.
    /// </summary>
    public async Task CheckAtStartupAsync()
    {
        if (!IsInstalled)
        {
            return;
        }

        try
        {
            await FindAsync();
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Checking for updates at startup");
        }
    }

    // "Check for updates": says so when there is none, a failure is reported like any other operation
    [RelayCommand]
    private Task CheckAsync()
    {
        return WithBusyIndicator(async () =>
        {
            if (!await FindAsync())
            {
                FlashMessages.Push(new FlashMessage("Werfer is up to date"));
            }
        });
    }

    // "Download <version>": downloads it, then has it installed once the app closes, without starting it again
    [RelayCommand]
    private Task DownloadAsync()
    {
        return WithBusyIndicator(async () =>
        {
            if (Available is not { } update)
            {
                return;
            }

            await Manager.DownloadUpdatesAsync(update);
            Manager.WaitExitThenApplyUpdates(
                update.TargetFullRelease,
                true,
                false
            );
            IsDownloaded = true;

            FlashMessages.Push(new FlashMessage(
                $"Werfer {AvailableVersion} will be installed at the next restart",
                "Update downloaded"
            ));
        });
    }

    // True when a newer release was found, announced with a message pointing to the menu
    private async Task<bool> FindAsync()
    {
        Available = await Manager.CheckForUpdatesAsync();

        if (Available is null)
        {
            return false;
        }

        FlashMessages.Push(new FlashMessage(
            $"Werfer {AvailableVersion} is available, download it from the menu",
            "Update available"
        ));

        return true;
    }
}
