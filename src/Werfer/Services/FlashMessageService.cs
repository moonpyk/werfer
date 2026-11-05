using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Werfer.Services;

/// <summary>
///     Messages currently shown to the user, each one at most once.
/// </summary>
[SuppressMessage(
    "ReSharper",
    "UnusedMethodReturnValue.Global",
    Justification = "Fluent usage"
)]
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

    /// <summary>
    ///     Clears all current messages and shows the specified message.
    /// </summary>
    /// <param name="message">The message to show after clearing existing messages.</param>
    public FlashMessageService ReplaceWith(FlashMessage message)
    {
        return Clear().Push(message);
    }

    /// <summary>
    ///     Removes the specified message from the current messages.
    /// </summary>
    /// <param name="message">The message to remove.</param>
    public FlashMessageService Remove(FlashMessage message)
    {
        _messages.Remove(message);
        return this;
    }

    /// <summary>
    ///     Removes all currently shown flash messages.
    /// </summary>
    public FlashMessageService Clear()
    {
        _messages.Clear();
        return this;
    }
}
