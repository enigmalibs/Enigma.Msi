using Enigma.Msi.Model;
using Xunit;

namespace Enigma.Msi.UnitTests.Model;

/// <summary>
/// Pins the model's defaults — they are part of the contract: a freshly created package is editable
/// (no null sections) and picks the choices the family's profiles use.
/// </summary>
public sealed class MsiPackageDefaultsTests
{
    [Fact]
    public void NewPackage_HasEditableSectionsAndFamilyDefaults()
    {
        var package = new MsiPackage();

        Assert.Equal(1, MsiPackage.CurrentSchemaVersion);
        Assert.Equal(MsiPackage.CurrentSchemaVersion, package.SchemaVersion);
        Assert.Equal(InstallScope.PerMachine, package.Scope);
        Assert.Equal(CompressionLevel.High, package.Compression);
        Assert.NotNull(package.Install);
        Assert.NotNull(package.Output);
        Assert.Empty(package.Shortcuts);
        Assert.Null(package.ControlPanel);
        Assert.Null(package.Ui);
        Assert.Null(package.Version);
    }

    [Fact]
    public void CreateDefaultUi_MatchesTheSequencesTheBuildAppliesWhenUiIsNull()
    {
        UiSettings ui = UiSettings.CreateDefault();

        Assert.Equal(Wui.WixUI_InstallDir, ui.Wui);
        Assert.Equal(new[] { Dialog.Welcome, Dialog.InstallDir, Dialog.Progress, Dialog.Exit }, ui.InstallDialogs);
        Assert.Equal(new[] { Dialog.Welcome, Dialog.MaintenanceType, Dialog.Progress, Dialog.Exit }, ui.ModifyDialogs);
    }

    [Fact]
    public void NewUiSettings_DefaultsToInstallDirWithEmptyDialogSequences()
    {
        var ui = new UiSettings();

        Assert.Equal(Wui.WixUI_InstallDir, ui.Wui);
        Assert.Empty(ui.InstallDialogs);
        Assert.Empty(ui.ModifyDialogs);
    }
}
