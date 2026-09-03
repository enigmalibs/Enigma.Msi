using System;
using System.IO;
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
    public void AddShortcut_AppendsARowSeededFromTheAppName()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.AppName = "Contoso Widget";

        viewModel.AddShortcutCommand.Execute(null);

        ShortcutViewModel row = Assert.Single(viewModel.Shortcuts);
        Assert.Equal("Contoso Widget", row.ShortcutName);
        Assert.Equal(ShortcutViewModel.DefaultShortcutPath, row.ShortcutPath);
        Assert.True(viewModel.HasShortcuts);
    }

    [Fact]
    public void AddShortcut_AppendsRatherThanReplacing()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.AddShortcutCommand.Execute(null);
        viewModel.Shortcuts[0].ShortcutName = "First";
        viewModel.AddShortcutCommand.Execute(null);

        Assert.Equal(2, viewModel.Shortcuts.Count);
        Assert.Equal("First", viewModel.Shortcuts[0].ShortcutName);
    }

    [Fact]
    public void RemoveShortcut_RemovesTheRowItIsGiven_WhicheverItIs()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.AddShortcutCommand.Execute(null);
        viewModel.AddShortcutCommand.Execute(null);
        viewModel.Shortcuts[0].ShortcutName = "Keep";
        ShortcutViewModel second = viewModel.Shortcuts[1];

        // No selection is involved: the row's own button hands its row over as the parameter.
        viewModel.RemoveShortcutCommand.Execute(second);

        ShortcutViewModel remaining = Assert.Single(viewModel.Shortcuts);
        Assert.Equal("Keep", remaining.ShortcutName);
    }

    [Fact]
    public void RemoveShortcut_IsAlwaysExecutable_AndDoesNothingWithoutARow()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.AddShortcutCommand.Execute(null);

        Assert.True(viewModel.RemoveShortcutCommand.CanExecute(null));

        viewModel.RemoveShortcutCommand.Execute(null);

        Assert.Single(viewModel.Shortcuts);

        viewModel.RemoveShortcutCommand.Execute(viewModel.Shortcuts[0]);

        Assert.Empty(viewModel.Shortcuts);
        Assert.False(viewModel.HasShortcuts);
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
        viewModel.RemoveShortcutCommand.Execute(row);

        int changes = 0;
        viewModel.Changed += (_, _) => changes++;
        row.TargetPath = @"[INSTALLDIR]\Gone.exe";

        Assert.Equal(0, changes);
    }

    // ---- control panel default ----------------------------------------------------------------

    [Fact]
    public void Reset_LeavesTheControlPanelSectionOn()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.HasControlPanelInfo = false;

        viewModel.Reset();

        Assert.True(viewModel.HasControlPanelInfo);
        Assert.NotNull(viewModel.ToPackage().ControlPanel);
    }

    [Fact]
    public void Reset_ThenSaved_WritesAnEmptyControlPanelBlock()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        // What the toggle honestly reports: the section is included, and nothing in it is filled in.
        Assert.Contains("\"controlPanel\": {}", MsiPackageJson.Serialize(viewModel.ToPackage()));
    }

    // ---- has data -----------------------------------------------------------------------------

    [Fact]
    public void HasData_IsFalseOnAFreshPackage_DespiteItsGeneratedIdentifiers()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.HasData);
    }

    [Fact]
    public void HasData_IgnoresTheVersionAndTheTwoIdentifiers()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.Version = "9.9.9";
        viewModel.NewProductIdCommand.Execute(null);
        viewModel.NewUpgradeCodeCommand.Execute(null);

        Assert.False(viewModel.HasData);
    }

    [Fact]
    public void HasData_IsTrueForEachFieldThatCountsAsWork()
    {
        Assert.True(WithField(viewModel => viewModel.AppName = "Widget"));
        Assert.True(WithField(viewModel => viewModel.Manufacturer = "Contoso AG"));
        Assert.True(WithField(viewModel => viewModel.InstallPath = @"%ProgramFiles%\Widget"));
        Assert.True(WithField(viewModel => viewModel.ReleasePath = @"C:\payload"));
        Assert.True(WithField(viewModel => viewModel.OutputPath = @"C:\out"));
        Assert.True(WithField(viewModel => viewModel.MsiFilename = "Widget"));
        Assert.True(WithField(viewModel => viewModel.ProductIcon = @"C:\art\app.ico"));
        Assert.True(WithField(viewModel => viewModel.Comments = "A widget."));
        Assert.True(WithField(viewModel => viewModel.Contact = "support@contoso.example"));
        Assert.True(WithField(viewModel => viewModel.HelpLink = "https://contoso.example/support"));
        Assert.True(WithField(viewModel => viewModel.UrlInfoAbout = "https://contoso.example/widget"));
        Assert.True(WithField(viewModel => viewModel.AddShortcutCommand.Execute(null)));
        Assert.True(WithField(viewModel => viewModel.Ui.IsCustomized = true));
    }

    [Fact]
    public void HasData_TreatsWhitespaceAsNothing()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.AppName = "   ";

        Assert.False(viewModel.HasData);
    }

    // ---- quick start --------------------------------------------------------------------------

    [Fact]
    public void ApplyQuickStart_TakesTheEnteredFieldsVerbatim()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        QuickStartSettings settings = CreateSettings();

        viewModel.ApplyQuickStart(settings);

        Assert.Equal("Widget", viewModel.AppName);
        Assert.Equal("2.1.0", viewModel.Version);
        Assert.Equal("Contoso AG", viewModel.Manufacturer);
        Assert.Equal(settings.ReleasePath, viewModel.ReleasePath);
        Assert.Equal(settings.OutputPath, viewModel.OutputPath);
    }

    [Fact]
    public void ApplyQuickStart_DerivesTheInstallPathUnderProgramFiles()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.ApplyQuickStart(CreateSettings() with { AppName = "  Widget  " });

        Assert.Equal(@"%ProgramFiles%\Widget", viewModel.InstallPath);
    }

    [Fact]
    public void ApplyQuickStart_TakesTheOutputFolderAsEntered_WithoutDerivingItFromTheReleaseFolder()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        string release = Path.Combine(Root, "payload", "release");
        string output = Path.Combine(Root, "drops", "msi");

        viewModel.ApplyQuickStart(CreateSettings() with { ReleasePath = release, OutputPath = output });

        // Neither the release folder nor its parent: nothing derives this field any more.
        Assert.Equal(output, viewModel.OutputPath);
    }

    [Fact]
    public void ApplyQuickStart_AcceptsAnOutputFolderInsideTheReleaseFolder()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        string release = Path.Combine(Root, "payload", "release");
        string output = Path.Combine(release, "installer");

        viewModel.ApplyQuickStart(CreateSettings() with { ReleasePath = release, OutputPath = output });

        Assert.Equal(output, viewModel.OutputPath);
        Assert.Empty(viewModel.GetInputErrors());
    }

    [Fact]
    public void ApplyQuickStart_NamesTheMsiAfterTheApplication()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.ApplyQuickStart(CreateSettings());

        Assert.Equal("Widget", viewModel.MsiFilename);
    }

    [Fact]
    public void ApplyQuickStart_SanitizesTheMsiFileName()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        // '/' is an invalid file-name character on every platform this suite runs on, so the expectation
        // does not depend on the host.
        viewModel.ApplyQuickStart(CreateSettings() with { AppName = "Contoso/Widget" });

        Assert.Equal("ContosoWidget", viewModel.MsiFilename);
        Assert.DoesNotContain(
            new MsiPackageValidator().Validate(viewModel.ToPackage()).Errors,
            error => error.Path == "output.msiFilename");
    }

    [Fact]
    public void ApplyQuickStart_StripsATrailingMsiExtensionFromTheFileName()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.ApplyQuickStart(CreateSettings() with { AppName = "Widget.MSI" });

        Assert.Equal("Widget", viewModel.MsiFilename);
    }

    [Fact]
    public void ApplyQuickStart_FallsBackToAFileNameWhenNothingSurvivesSanitization()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.ApplyQuickStart(CreateSettings() with { AppName = "///" });

        Assert.Equal(PackageEditorViewModel.FallbackMsiFilename, viewModel.MsiFilename);
    }

    [Fact]
    public void ApplyQuickStart_SwitchesTheControlPanelSectionOnWithTheIcon()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        QuickStartSettings settings = CreateSettings();

        viewModel.ApplyQuickStart(settings);

        Assert.True(viewModel.HasControlPanelInfo);
        Assert.Equal(settings.IconPath, viewModel.ProductIcon);
        Assert.Equal(settings.IconPath, viewModel.ToPackage().ControlPanel?.ProductIcon);
    }

    [Fact]
    public void ApplyQuickStart_AppendsExactlyTwoShortcuts_ProgramMenuThenDesktop()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        QuickStartSettings settings = CreateSettings();

        viewModel.ApplyQuickStart(settings);

        Assert.Equal(2, viewModel.Shortcuts.Count);
        Assert.Equal("%ProgramMenu%", viewModel.Shortcuts[0].ShortcutPath);
        Assert.Equal("%Desktop%", viewModel.Shortcuts[1].ShortcutPath);

        foreach (ShortcutViewModel shortcut in viewModel.Shortcuts)
        {
            Assert.Equal("Widget", shortcut.ShortcutName);
            Assert.Equal(@"[INSTALLDIR]\Widget.exe", shortcut.TargetPath);
            Assert.Equal(settings.IconPath, shortcut.IconPath);
            Assert.Equal(string.Empty, shortcut.Arguments);
        }
    }

    [Fact]
    public void ApplyQuickStart_KeepsANestedExecutablesSubFolderInTheTarget()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.ApplyQuickStart(CreateSettings() with { ExecutableRelativePath = @"bin\Widget.exe" });

        Assert.Equal(@"[INSTALLDIR]\bin\Widget.exe", viewModel.Shortcuts[0].TargetPath);
        Assert.Equal(@"[INSTALLDIR]\bin\Widget.exe", viewModel.Shortcuts[1].TargetPath);
    }

    [Fact]
    public void ApplyQuickStart_ReplacesTheWholePackage_WithFreshIdentifiers()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.LoadFrom(TestPackages.CreateFull());
        string productId = viewModel.ProductId;
        string upgradeCode = viewModel.UpgradeCode;

        viewModel.ApplyQuickStart(CreateSettings());

        Assert.NotEqual(productId, viewModel.ProductId);
        Assert.NotEqual(upgradeCode, viewModel.UpgradeCode);
        Assert.Equal(2, viewModel.Shortcuts.Count);
        Assert.Equal(string.Empty, viewModel.Comments);
        Assert.Equal(string.Empty, viewModel.Contact);
        Assert.Equal(string.Empty, viewModel.HelpLink);
        Assert.Equal(string.Empty, viewModel.UrlInfoAbout);
        Assert.False(viewModel.Ui.IsCustomized);
        Assert.Equal(InstallScope.PerMachine, viewModel.Scope);
        Assert.Equal(CompressionLevel.High, viewModel.Compression);
    }

    [Fact]
    public void ApplyQuickStart_WiresTheNewShortcutsIntoTheChangeNotification()
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        viewModel.ApplyQuickStart(CreateSettings());
        int changes = 0;
        viewModel.Changed += (_, _) => changes++;

        viewModel.Shortcuts[1].Arguments = "--quiet";

        Assert.True(changes > 0);
    }

    [Fact]
    public void ApplyQuickStart_RejectsMissingSettings()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => viewModel.ApplyQuickStart(null!));

        Assert.Equal("settings", exception.ParamName);
    }

    [Fact]
    public void ApplyQuickStart_ProducesAPackageThatPassesTheInMemoryRules()
    {
        PackageEditorViewModel viewModel = CreateViewModel();

        viewModel.ApplyQuickStart(CreateSettings());

        Assert.Empty(viewModel.GetInputErrors());
        Assert.True(new MsiPackageValidator().Validate(viewModel.ToPackage()).IsValid);
    }

    // ---- helpers ------------------------------------------------------------------------------

    // The drive root the quick-start tests build their paths under: "C:\" on Windows, "/" elsewhere, so
    // the paths are the host's own rather than a hard-coded Windows literal.
    private static string Root => Path.GetPathRoot(Path.GetTempPath())!;

    private static QuickStartSettings CreateSettings() => new(
        "Widget",
        "2.1.0",
        "Contoso AG",
        Path.Combine(Root, "payload", "release"),
        Path.Combine(Root, "drops"),
        Path.Combine(Root, "art", "app.ico"),
        "Widget.exe");

    private bool WithField(Action<PackageEditorViewModel> edit)
    {
        PackageEditorViewModel viewModel = CreateViewModel();
        edit(viewModel);

        return viewModel.HasData;
    }

    private PackageEditorViewModel CreateViewModel() => new(_pathPicker);
}
