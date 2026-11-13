using FluentAvalonia.UI.Controls;

namespace Werfer.Services;

/// <summary>
///     Flash messages used in several places, or removed later by reference: each one is a single shared instance.
/// </summary>
public static class WellKnownMessages
{
    /// <summary>
    ///     No API key saved yet: without one the app can't do anything.
    /// </summary>
    public static FlashMessage NoApiKey { get; } = new(
        "Set your Clockify API key in Settings",
        "Not connected",
        FAInfoBarSeverity.Error
    );

    /// <summary>
    ///     Shown when Clockify rejects the API key, cleared once a key is accepted.
    /// </summary>
    public static FlashMessage InvalidApiKey { get; } = new(
        "Check your Clockify API key in Settings",
        "Invalid API key",
        FAInfoBarSeverity.Error
    );

    /// <summary>
    ///     The settings file couldn't be written, the change only lasts until the app closes.
    /// </summary>
    public static FlashMessage SettingsNotSaved { get; } = new(
        "Unable to persist settings",
        Severity: FAInfoBarSeverity.Warning
    );

    /// <summary>
    ///     Start pressed with no project and task picked.
    /// </summary>
    public static FlashMessage PickProjectFirst { get; } = new(
        "Pick a project and a task first",
        Severity: FAInfoBarSeverity.Warning
    );

    /// <summary>
    ///     Start pressed with no project picked, and the default one can't be started: archived, done, or no last entry.
    /// </summary>
    public static FlashMessage DefaultChoiceUnavailable { get; } = new(
        "The default project and task can't be started anymore, pick one",
        Severity: FAInfoBarSeverity.Warning
    );
}
