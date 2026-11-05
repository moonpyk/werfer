using FluentAvalonia.UI.Controls;

namespace Werfer.Services;

/// <summary>
///     A message shown to the user as an info bar.
///     A record, so two messages with the same content are equal.
/// </summary>
public record FlashMessage(
    string Message,
    string? Title = null,
    FAInfoBarSeverity Severity = FAInfoBarSeverity.Informational
);
