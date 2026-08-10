using System.Collections.Generic;
using Enigma.Msi.Desktop.ViewModels;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the shortcut row's own behaviour: the card header it exposes over the edited name.
/// </summary>
public sealed class ShortcutViewModelTests
{
    [Fact]
    public void DisplayName_FallsBackWhileTheRowHasNoName()
    {
        var row = new ShortcutViewModel();

        Assert.Equal(ShortcutViewModel.UnnamedDisplayName, row.DisplayName);

        row.ShortcutName = "   ";

        // Whitespace is not a name either — a header of blanks is worse than the fallback.
        Assert.Equal(ShortcutViewModel.UnnamedDisplayName, row.DisplayName);
    }

    [Fact]
    public void DisplayName_IsTheNameOnceThereIsOne()
    {
        var row = new ShortcutViewModel { ShortcutName = "Contoso Widget" };

        Assert.Equal("Contoso Widget", row.DisplayName);
    }

    [Fact]
    public void DisplayName_IsNotifiedWhenTheNameChanges()
    {
        var row = new ShortcutViewModel();
        List<string?> changed = [];
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        row.ShortcutName = "Renamed";

        // Without this the header would keep the fallback until the row was re-created.
        Assert.Contains(nameof(ShortcutViewModel.DisplayName), changed);
    }
}
