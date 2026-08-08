using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Model;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the optional managed-UI block, whose off state is what "<c>ui</c> is absent" looks like in
/// the form.
/// </summary>
public sealed class UiSettingsViewModelTests
{
    [Fact]
    public void ANewEditor_IsOffButPreFilledWithTheBuildsOwnDefaults()
    {
        var viewModel = new UiSettingsViewModel();
        UiSettings defaults = UiSettings.CreateDefault();

        Assert.False(viewModel.IsCustomized);
        Assert.Null(viewModel.ToModel());
        Assert.Equal(defaults.Wui, viewModel.Wui);
        Assert.Equal(defaults.InstallDialogs, viewModel.InstallDialogs.ToModel());
        Assert.Equal(defaults.ModifyDialogs, viewModel.ModifyDialogs.ToModel());
    }

    [Fact]
    public void SwitchingItOn_MaterializesWhatWasAlreadyShown()
    {
        var viewModel = new UiSettingsViewModel();

        viewModel.IsCustomized = true;
        UiSettings? materialized = viewModel.ToModel();

        Assert.NotNull(materialized);
        Assert.Equal(UiSettings.CreateDefault().Wui, materialized.Wui);
        Assert.Equal(UiSettings.CreateDefault().InstallDialogs, materialized.InstallDialogs);
    }

    [Fact]
    public void LoadFrom_ASettingsBlock_SwitchesItOnAndShowsIt()
    {
        var viewModel = new UiSettingsViewModel();
        var settings = new UiSettings
        {
            Wui = Wui.WixUI_FeatureTree,
            InstallDialogs = [Dialog.Welcome, Dialog.Features, Dialog.Exit],
            ModifyDialogs = [Dialog.Welcome, Dialog.Exit]
        };

        viewModel.LoadFrom(settings);

        Assert.True(viewModel.IsCustomized);
        Assert.Equal(Wui.WixUI_FeatureTree, viewModel.Wui);
        Assert.Equal(settings.InstallDialogs, viewModel.InstallDialogs.ToModel());
        Assert.Equal(settings.ModifyDialogs, viewModel.ModifyDialogs.ToModel());
    }

    [Fact]
    public void LoadFrom_Null_SwitchesItBackOffAndRestoresTheDefaults()
    {
        var viewModel = new UiSettingsViewModel();
        viewModel.LoadFrom(new UiSettings
        {
            Wui = Wui.WixUI_Minimal,
            InstallDialogs = [Dialog.Progress],
            ModifyDialogs = [Dialog.Progress]
        });

        viewModel.LoadFrom(null);

        Assert.False(viewModel.IsCustomized);
        Assert.Null(viewModel.ToModel());
        Assert.Equal(UiSettings.CreateDefault().Wui, viewModel.Wui);
        Assert.Equal(UiSettings.CreateDefault().InstallDialogs, viewModel.InstallDialogs.ToModel());
    }

    [Fact]
    public void AnEmptySequence_IsMaterializedAsIs_ForTheValidatorToReject()
    {
        var viewModel = new UiSettingsViewModel();
        viewModel.IsCustomized = true;
        viewModel.InstallDialogs.Load([]);

        UiSettings? materialized = viewModel.ToModel();

        Assert.NotNull(materialized);
        Assert.Empty(materialized.InstallDialogs);
    }

    [Fact]
    public void AvailableWuis_OffersEveryModelDialogSet()
    {
        var viewModel = new UiSettingsViewModel();

        Assert.Equal(System.Enum.GetValues<Wui>().Length, viewModel.AvailableWuis.Count);
    }
}
