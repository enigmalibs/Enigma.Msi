using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Model;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the ordered dialog-sequence editor — the reason it is a reorderable list and not a set of
/// check boxes.
/// </summary>
public sealed class DialogSequenceViewModelTests
{
    [Fact]
    public void Load_KeepsAnOrderThatIsNotTheEnumsOwn()
    {
        var viewModel = new DialogSequenceViewModel();

        // The default modify sequence: MaintenanceType comes before Progress, while the enum declares
        // Progress first. A checklist-style editor would silently swap them.
        viewModel.Load(UiSettings.CreateDefault().ModifyDialogs);

        Assert.Equal(
            new[] { Dialog.Welcome, Dialog.MaintenanceType, Dialog.Progress, Dialog.Exit },
            viewModel.ToModel());
    }

    [Fact]
    public void Load_ReplacesTheSequenceAndClearsTheSelection()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome, Dialog.Exit]);
        viewModel.SelectedIndex = 1;

        viewModel.Load([Dialog.Progress]);

        Assert.Equal(new[] { Dialog.Progress }, viewModel.ToModel());
        Assert.Equal(-1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Add_AppendsTheChosenDialogAndSelectsIt()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome]);
        viewModel.DialogToAdd = Dialog.Licence;

        viewModel.AddCommand.Execute(null);

        Assert.Equal(new[] { Dialog.Welcome, Dialog.Licence }, viewModel.ToModel());
        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Add_AllowsTheSameDialogTwice()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.DialogToAdd = Dialog.Progress;

        viewModel.AddCommand.Execute(null);
        viewModel.AddCommand.Execute(null);

        Assert.Equal(new[] { Dialog.Progress, Dialog.Progress }, viewModel.ToModel());
    }

    [Fact]
    public void Remove_IsDisabledWithoutASelection()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome, Dialog.Exit]);

        Assert.False(viewModel.RemoveCommand.CanExecute(null));

        viewModel.SelectedIndex = 0;

        Assert.True(viewModel.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public void Remove_KeepsANeighbourSelected()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome, Dialog.InstallDir, Dialog.Exit]);
        viewModel.SelectedIndex = 2;

        viewModel.RemoveCommand.Execute(null);

        Assert.Equal(new[] { Dialog.Welcome, Dialog.InstallDir }, viewModel.ToModel());
        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Remove_TheLastEntryClearsTheSelection()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome]);
        viewModel.SelectedIndex = 0;

        viewModel.RemoveCommand.Execute(null);

        Assert.Empty(viewModel.ToModel());
        Assert.Equal(-1, viewModel.SelectedIndex);
        Assert.False(viewModel.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public void MoveUp_IsDisabledAtTheTopAndMovesTheSelectionWithTheEntry()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome, Dialog.InstallDir, Dialog.Exit]);
        viewModel.SelectedIndex = 0;

        Assert.False(viewModel.MoveUpCommand.CanExecute(null));

        viewModel.SelectedIndex = 2;
        viewModel.MoveUpCommand.Execute(null);

        Assert.Equal(new[] { Dialog.Welcome, Dialog.Exit, Dialog.InstallDir }, viewModel.ToModel());
        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public void MoveDown_IsDisabledAtTheBottomAndMovesTheSelectionWithTheEntry()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome, Dialog.InstallDir, Dialog.Exit]);
        viewModel.SelectedIndex = 2;

        Assert.False(viewModel.MoveDownCommand.CanExecute(null));

        viewModel.SelectedIndex = 0;
        viewModel.MoveDownCommand.Execute(null);

        Assert.Equal(new[] { Dialog.InstallDir, Dialog.Welcome, Dialog.Exit }, viewModel.ToModel());
        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public void ToModel_IsACopy_NotTheLiveCollection()
    {
        var viewModel = new DialogSequenceViewModel();
        viewModel.Load([Dialog.Welcome]);

        var snapshot = viewModel.ToModel();
        viewModel.Dialogs.Add(Dialog.Exit);

        Assert.Single(snapshot);
    }

    [Fact]
    public void AvailableDialogs_OffersEveryModelDialog()
    {
        var viewModel = new DialogSequenceViewModel();

        Assert.Equal(System.Enum.GetValues<Dialog>().Length, viewModel.AvailableDialogs.Count);
    }
}
