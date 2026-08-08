using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Msi.Model;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// Editor for one ordered managed-UI dialog sequence — the install sequence or the modify sequence.
/// </summary>
/// <remarks>
/// An add/remove/reorder list rather than a set of check boxes, because the model's sequences are
/// <em>ordered</em> and the order is not the enum's: the default modify sequence is
/// Welcome → MaintenanceType → Progress → Exit, while <see cref="Dialog"/> declares
/// <see cref="Dialog.Progress"/> before <see cref="Dialog.MaintenanceType"/>. A checklist would
/// silently reorder it.
/// </remarks>
public sealed partial class DialogSequenceViewModel : ObservableObject
{
    /// <summary>Creates an empty sequence.</summary>
    public DialogSequenceViewModel()
    {
        Dialogs.CollectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        PropertyChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raised whenever the sequence or the selection changes — what the window's Build gating listens
    /// to, since <see cref="ObservableObject"/>'s own notifications do not carry collection edits.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>The sequence being edited, in the order the installer will show it.</summary>
    public ObservableCollection<Dialog> Dialogs { get; } = [];

    /// <summary>Every dialog that can be added, for the add drop-down.</summary>
    public IReadOnlyList<Dialog> AvailableDialogs { get; } = Enum.GetValues<Dialog>();

    [ObservableProperty]
    private Dialog _dialogToAdd = Dialog.Welcome;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private int _selectedIndex = -1;

    /// <summary>Replaces the sequence with <paramref name="dialogs"/>.</summary>
    /// <param name="dialogs">The sequence to show, in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialogs"/> is <see langword="null"/>.</exception>
    public void Load(IEnumerable<Dialog> dialogs)
    {
        if (dialogs is null)
        {
            throw new ArgumentNullException(nameof(dialogs));
        }

        Dialogs.Clear();

        foreach (Dialog dialog in dialogs)
        {
            Dialogs.Add(dialog);
        }

        SelectedIndex = -1;
    }

    /// <summary>Materializes the sequence as a model list.</summary>
    /// <returns>A copy of the sequence, in order.</returns>
    public List<Dialog> ToModel() => [.. Dialogs];

    [RelayCommand]
    private void Add()
    {
        Dialogs.Add(DialogToAdd);
        SelectedIndex = Dialogs.Count - 1;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        int index = SelectedIndex;
        Dialogs.RemoveAt(index);
        // Keep a neighbour selected so repeated removals need no extra click.
        SelectedIndex = Dialogs.Count == 0 ? -1 : Math.Min(index, Dialogs.Count - 1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        int index = SelectedIndex;
        Dialogs.Move(index, index - 1);
        SelectedIndex = index - 1;
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        int index = SelectedIndex;
        Dialogs.Move(index, index + 1);
        SelectedIndex = index + 1;
    }

    private bool HasSelection => SelectedIndex >= 0 && SelectedIndex < Dialogs.Count;

    private bool CanMoveUp => SelectedIndex > 0 && SelectedIndex < Dialogs.Count;

    private bool CanMoveDown => SelectedIndex >= 0 && SelectedIndex < Dialogs.Count - 1;
}
