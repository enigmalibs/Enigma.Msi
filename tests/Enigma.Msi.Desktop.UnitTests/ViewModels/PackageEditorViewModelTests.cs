using System;
using System.Linq;
using System.Threading.Tasks;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;
using Enigma.Msi.Validation;
using NSubstitute;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the form: the two model crossings (<c>LoadFrom</c>/<c>ToPackage</c>), the text that no model
/// can hold, and the commands the form owns.
/// </summary>
public sealed class PackageEditorViewModelTests
{
    private readonly IPathPickerService _pathPicker = Substitute.For<IPathPickerService>();

    [Fact]
    public void RoundTrip_PreservesEveryField()
    {
        MsiPackage original = TestPackages.CreateFull();
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.LoadFrom(original);

        // Comparing the serializations compares every member the profile format carries, which is the
        // property that matters: what the form loses, a saved profile loses.
        Assert.Equal(MsiPackageJson.Serialize(original), MsiPackageJson.Serialize(viewModel.ToPackage()));
    }

    [Fact]
    public void RoundTrip_PreservesShortcutOrderAndOptionalMembers()
    {
        MsiPackage original = TestPackages.CreateFull();
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.LoadFrom(original);
        MsiPackage materialized = viewModel.ToPackage();

        Assert.Equal(2, materialized.Shortcuts.Count);
        Assert.Equal("%Desktop%", materialized.Shortcuts[0].ShortcutPath);
        Assert.Equal("--first-run", materialized.Shortcuts[0].Arguments);
        // The second fixture shortcut leaves both optional members unset: a blank text box must come
        // back as null, not as "".
        Assert.Null(materialized.Shortcuts[1].IconPath);
        Assert.Null(materialized.Shortcuts[1].Arguments);
    }

    [Fact]
    public void RoundTrip_PreservesAFutureSchemaVersion()
    {
        MsiPackage original = TestPackages.CreateMinimalValid();
        original.SchemaVersion = MsiPackage.CurrentSchemaVersion + 1;
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.LoadFrom(original);

        Assert.Equal(original.SchemaVersion, viewModel.ToPackage().SchemaVersion);
    }

    [Fact]
    public void LoadFrom_WithoutOptionalBlocks_SwitchesThemOff()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.LoadFrom(TestPackages.CreateMinimalValid());

        Assert.False(viewModel.HasControlPanelInfo);
        Assert.False(viewModel.Ui.IsCustomized);
        Assert.Null(viewModel.ToPackage().ControlPanel);
        Assert.Null(viewModel.ToPackage().Ui);
    }

    [Fact]
    public void ToPackage_WithControlPanelOn_ButEveryFieldBlank_WritesNulls()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.LoadFrom(TestPackages.CreateMinimalValid());
        viewModel.HasControlPanelInfo = true;

        ControlPanelInfo? controlPanel = viewModel.ToPackage().ControlPanel;

        Assert.NotNull(controlPanel);
        Assert.Null(controlPanel.ProductIcon);
        Assert.Null(controlPanel.Comments);
        Assert.Null(controlPanel.Contact);
        Assert.Null(controlPanel.HelpLink);
        Assert.Null(controlPanel.UrlInfoAbout);
    }

    [Fact]
    public void Reset_ProducesANewPackageWithFreshDistinctIdentifiers()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.LoadFrom(TestPackages.CreateFull());

        viewModel.Reset();

        Assert.Equal(string.Empty, viewModel.AppName);
        Assert.Equal(PackageEditorViewModel.DefaultVersion, viewModel.Version);
        Assert.Empty(viewModel.Shortcuts);
        Assert.NotEqual(Guid.Empty, Guid.Parse(viewModel.ProductId));
        Assert.NotEqual(Guid.Empty, Guid.Parse(viewModel.UpgradeCode));
        Assert.NotEqual(viewModel.ProductId, viewModel.UpgradeCode);
    }

    [Fact]
    public void GetInputErrors_AreEmptyForARepresentablePackage()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.LoadFrom(TestPackages.CreateFull());

        Assert.Empty(viewModel.GetInputErrors());
    }

    [Fact]
    public void GetInputErrors_ReportABadVersionAndBothBadIdentifiers()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.LoadFrom(TestPackages.CreateMinimalValid());
        viewModel.Version = "one point two";
        viewModel.ProductId = "not-a-guid";
        viewModel.UpgradeCode = "also-not-a-guid";

        string[] paths = [.. viewModel.GetInputErrors().Select(error => error.Path)];

        Assert.Equal(new[] { "version", "productId", "upgradeCode" }, paths);
    }

    [Fact]
    public void GetInputErrors_TreatBlankAsTheValidatorsProblem_NotTheirs()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.LoadFrom(TestPackages.CreateMinimalValid());
        viewModel.Version = string.Empty;
        viewModel.ProductId = string.Empty;

        Assert.Empty(viewModel.GetInputErrors());

        // …and the validator is the one that complains, with the same member paths.
        MsiValidationResult result = new MsiPackageValidator().Validate(viewModel.ToPackage());
        Assert.Contains(result.Errors, error => error.Path == "version");
        Assert.Contains(result.Errors, error => error.Path == "productId");
    }

    [Fact]
    public void NewProductId_And_NewUpgradeCode_ReplaceOnlyTheirOwnField()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        string productId = viewModel.ProductId;
        string upgradeCode = viewModel.UpgradeCode;

        viewModel.NewProductIdCommand.Execute(null);

        Assert.NotEqual(productId, viewModel.ProductId);
        Assert.Equal(upgradeCode, viewModel.UpgradeCode);

        viewModel.NewUpgradeCodeCommand.Execute(null);

        Assert.NotEqual(upgradeCode, viewModel.UpgradeCode);
    }

    [Fact]
    public void AddShortcut_SelectsTheNewRowAndSeedsItsNameFromTheApp()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.AppName = "Contoso Widget";

        viewModel.AddShortcutCommand.Execute(null);

        ShortcutViewModel row = Assert.Single(viewModel.Shortcuts);
        Assert.Same(row, viewModel.SelectedShortcut);
        Assert.Equal("Contoso Widget", row.ShortcutName);
        Assert.Equal(ShortcutViewModel.DefaultShortcutPath, row.ShortcutPath);
        Assert.True(viewModel.HasShortcuts);
    }

    [Fact]
    public void RemoveShortcut_IsDisabledUntilARowIsSelected()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.RemoveShortcutCommand.CanExecute(null));

        viewModel.AddShortcutCommand.Execute(null);

        Assert.True(viewModel.RemoveShortcutCommand.CanExecute(null));

        viewModel.RemoveShortcutCommand.Execute(null);

        Assert.Empty(viewModel.Shortcuts);
        Assert.False(viewModel.HasShortcuts);
        Assert.False(viewModel.RemoveShortcutCommand.CanExecute(null));
    }

    [Fact]
    public async Task BrowseReleasePath_TakesThePickedFolder()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        _pathPicker.PickFolderAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(@"C:\picked\release");

        await viewModel.BrowseReleasePathCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\picked\release", viewModel.ReleasePath);
    }

    [Fact]
    public async Task BrowseOutputPath_LeavesTheFieldAloneWhenCancelled()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.OutputPath = @"C:\kept";
        _pathPicker.PickFolderAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);

        await viewModel.BrowseOutputPathCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\kept", viewModel.OutputPath);
    }

    [Fact]
    public async Task BrowseProductIcon_TakesThePickedFile()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        _pathPicker.PickIconAsync(Arg.Any<string?>()).Returns(@"C:\picked\app.ico");

        await viewModel.BrowseProductIconCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\picked\app.ico", viewModel.ProductIcon);
    }

    [Fact]
    public void Changed_FiresForAFieldEdit_AShortcutEdit_AndAManagedUiEdit()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        int changes = 0;
        viewModel.Changed += (_, _) => changes++;

        viewModel.AppName = "Widget";
        Assert.True(changes > 0);

        viewModel.AddShortcutCommand.Execute(null);
        int afterAdd = changes;

        // The point of the event: an edit inside a child row must reach the window's Build gating.
        viewModel.Shortcuts[0].TargetPath = @"[INSTALLDIR]\Widget.exe";
        Assert.True(changes > afterAdd);

        int afterShortcut = changes;
        viewModel.Ui.IsCustomized = true;
        Assert.True(changes > afterShortcut);

        int afterUi = changes;
        viewModel.Ui.InstallDialogs.Dialogs.Add(Dialog.Features);
        Assert.True(changes > afterUi);
    }

    [Fact]
    public void Changed_StopsFollowingARemovedShortcut()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.AddShortcutCommand.Execute(null);
        ShortcutViewModel row = viewModel.Shortcuts[0];
        viewModel.RemoveShortcutCommand.Execute(null);

        int changes = 0;
        viewModel.Changed += (_, _) => changes++;
        row.TargetPath = @"[INSTALLDIR]\Gone.exe";

        Assert.Equal(0, changes);
    }

    private PackageEditorViewModel CreateViewModel() => new(_pathPicker);
}
