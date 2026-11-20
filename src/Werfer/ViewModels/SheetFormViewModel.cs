using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Werfer.ViewModels;

/// <summary>
///     A form shown in the timer page's sheet, under its Save and Cancel buttons. The timer page saves and closes it.
/// </summary>
public abstract partial class SheetFormViewModel : ObservableObject
{
    /// <summary>
    ///     Creates the form; <paramref name="cancel" /> closes the sheet without saving.
    /// </summary>
    protected SheetFormViewModel(Action cancel)
    {
        CancelAction = cancel;

        // Cancel is disabled while saving: it would only close the sheet, not stop the requests already sent
        SaveCommand.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SaveCommand.IsRunning))
            {
                CancelCommand.NotifyCanExecuteChanged();
            }
        };
    }

    private Action CancelAction { get; }

    /// <summary>
    ///     True when the form can be saved. A derived form notifies <see cref="SaveCommand" /> when it changes.
    /// </summary>
    public abstract bool CanSave { get; }

    private bool CanCancel => !SaveCommand.IsRunning;

    /// <summary>
    ///     Sends the form, see <see cref="SaveCommand" />.
    /// </summary>
    protected abstract Task SaveImplAsync();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        return SaveImplAsync();
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        CancelAction();
    }
}
