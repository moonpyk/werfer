using System.Collections.ObjectModel;
using FluentAvalonia.UI.Controls;

namespace Werfer.Services;

/// <summary>
///     A message shown to the user as an info bar. A record, so two messages with the same content are equal.
/// </summary>
public record FlashMessage(
    string Message,
    string? Title = null,
    FAInfoBarSeverity Severity = FAInfoBarSeverity.Informational
);

/// <summary>
///     Messages currently shown to the user, each one at most once.
/// </summary>
public class FlashMessageService
{
    private readonly ObservableCollection<FlashMessage> _messages = [];

    public FlashMessageService()
    {
        Messages = new ReadOnlyObservableCollection<FlashMessage>(_messages);
    }

    // Read-only so nothing bypasses the duplicate check in Push
    public ReadOnlyObservableCollection<FlashMessage> Messages { get; }

    /// <summary>
    ///     Shows the message, unless an equal one is already shown.
    /// </summary>
    public FlashMessageService Push(FlashMessage message)
    {
        if (!_messages.Contains(message))
        {
            _messages.Add(message);
        }

        return this;
    }
    
    public FlashMessageService ReplaceWith(FlashMessage message)
    {
        return Clear().Push(message);
    }

    public FlashMessageService Remove(FlashMessage message)
    {
        _messages.Remove(message);
        return this;
    }
    
    public FlashMessageService Clear()
    {
        _messages.Clear();
        return this;
    }
}
