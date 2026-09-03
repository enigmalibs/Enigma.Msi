using System;
using System.IO;
using System.Threading.Tasks;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using NSubstitute;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the quick start's form: the Apply gate, the relative-path derivation that a shortcut target
/// needs, and the four browse commands over the substituted picker.
/// </summary>
public sealed class QuickStartViewModelTests
{
    private readonly IPathPickerService _pathPicker = Substitute.For<IPathPickerService>();

    // "C:\" on Windows, "/" elsewhere: the paths below are the host's own rather than a hard-coded
    // Windows literal, so the derivation is exercised the same way on either.
    private static string Root => Path.GetPathRoot(Path.GetTempPath())!;

    private static string ReleaseFolder => Path.Combine(Root, "payload", "release");

    private static string OutputFolder => Path.Combine(Root, "drops");

    // ---- the apply gate -----------------------------------------------------------------------

    [Fact]
    public void CanApply_IsFalseOnAFreshForm()
    {
        QuickStartViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.CanApply);
        Assert.Equal(PackageEditorViewModel.DefaultVersion, viewModel.Version);
    }

    [Fact]
    public void CanApply_IsTrueOnceAllSevenAnswersAreThere()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        Assert.True(viewModel.CanApply);
    }

    [Fact]
    public void CanApply_IsFalseWhileAnySingleAnswerIsBlank()
    {
        Assert.False(WithoutAnswer(viewModel => viewModel.AppName = "  "));
        Assert.False(WithoutAnswer(viewModel => viewModel.Version = string.Empty));
        Assert.False(WithoutAnswer(viewModel => viewModel.Manufacturer = string.Empty));
        Assert.False(WithoutAnswer(viewModel => viewModel.ReleasePath = string.Empty));
        Assert.False(WithoutAnswer(viewModel => viewModel.OutputPath = "   "));
        Assert.False(WithoutAnswer(viewModel => viewModel.IconPath = string.Empty));
        Assert.False(WithoutAnswer(viewModel => viewModel.ExecutablePath = string.Empty));
    }

    [Fact]
    public void CanApply_IsFalseForAVersionThatDoesNotParse()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        viewModel.Version = "one point two";

        Assert.False(viewModel.CanApply);
    }

    [Fact]
    public void CanApply_TracksTheLastAnswerBeingFilledIn()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        viewModel.Manufacturer = string.Empty;
        Assert.False(viewModel.CanApply);

        viewModel.Manufacturer = "Contoso AG";
        Assert.True(viewModel.CanApply);
    }

    // ---- the executable ------------------------------------------------------------------------

    [Fact]
    public void ExecutableRelativePath_IsTheBareNameForAnExecutableAtTheTop()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        Assert.Equal("Widget.exe", viewModel.ExecutableRelativePath);
        Assert.Null(viewModel.ExecutableError);
        Assert.False(viewModel.HasExecutableError);
    }

    [Fact]
    public void ExecutableRelativePath_KeepsTheSubFolderOfANestedExecutable()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        viewModel.ExecutablePath = Path.Combine(ReleaseFolder, "bin", "Widget.exe");

        Assert.Equal(@"bin\Widget.exe", viewModel.ExecutableRelativePath);
    }

    [Fact]
    public void ExecutableRelativePath_IgnoresSeparatorStyleAndCasing()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.ReleasePath = @"C:\Payload\Release\";
        viewModel.ExecutablePath = @"c:/payload/release/bin/Widget.exe";

        Assert.Equal(@"bin\Widget.exe", viewModel.ExecutableRelativePath);
    }

    [Fact]
    public void ExecutableRelativePath_IsNullForAnExecutableOutsideTheReleaseFolder()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        viewModel.ExecutablePath = Path.Combine(Root, "elsewhere", "Widget.exe");

        Assert.Null(viewModel.ExecutableRelativePath);
        Assert.False(viewModel.CanApply);
        Assert.Equal(QuickStartViewModel.ExecutableOutsideReleaseFolderMessage, viewModel.ExecutableError);
        Assert.True(viewModel.HasExecutableError);
    }

    [Fact]
    public void ExecutableRelativePath_IsNullForAPathThatMerelySharesAPrefix()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.ReleasePath = Path.Combine(Root, "payload");
        viewModel.ExecutablePath = Path.Combine(Root, "payload-old", "Widget.exe");

        Assert.Null(viewModel.ExecutableRelativePath);
    }

    [Fact]
    public void ExecutableError_SaysNothingWhileNoExecutableIsChosen()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.ReleasePath = ReleaseFolder;

        Assert.Null(viewModel.ExecutableError);
        Assert.False(viewModel.HasExecutableError);
    }

    // ---- the four browse commands --------------------------------------------------------------

    [Fact]
    public async Task BrowseReleasePath_TakesThePickedFolder()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        _pathPicker.PickFolderAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(ReleaseFolder);

        await viewModel.BrowseReleasePathCommand.ExecuteAsync(null);

        Assert.Equal(ReleaseFolder, viewModel.ReleasePath);
    }

    [Fact]
    public async Task BrowseReleasePath_LeavesTheFieldAloneWhenCancelled()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.ReleasePath = ReleaseFolder;
        _pathPicker.PickFolderAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);

        await viewModel.BrowseReleasePathCommand.ExecuteAsync(null);

        Assert.Equal(ReleaseFolder, viewModel.ReleasePath);
    }

    [Fact]
    public async Task BrowseOutputPath_TakesThePickedFolderAndOpensAtWhatIsAlreadyThere()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.OutputPath = Path.Combine(Root, "old-drops");
        _pathPicker.PickFolderAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(OutputFolder);

        await viewModel.BrowseOutputPathCommand.ExecuteAsync(null);

        Assert.Equal(OutputFolder, viewModel.OutputPath);
        _ = _pathPicker.Received(1).PickFolderAsync(Arg.Any<string>(), Path.Combine(Root, "old-drops"));
    }

    [Fact]
    public async Task BrowseOutputPath_LeavesTheFieldAloneWhenCancelled()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.OutputPath = OutputFolder;
        _pathPicker.PickFolderAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);

        await viewModel.BrowseOutputPathCommand.ExecuteAsync(null);

        Assert.Equal(OutputFolder, viewModel.OutputPath);
    }

    [Fact]
    public async Task BrowseIcon_TakesThePickedFileAndOpensAtTheReleaseFolder()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.ReleasePath = ReleaseFolder;
        string icon = Path.Combine(ReleaseFolder, "app.ico");
        _pathPicker.PickIconAsync(Arg.Any<string?>()).Returns(icon);

        await viewModel.BrowseIconCommand.ExecuteAsync(null);

        Assert.Equal(icon, viewModel.IconPath);
        _ = _pathPicker.Received(1).PickIconAsync(ReleaseFolder);
    }

    [Fact]
    public async Task BrowseExecutable_TakesThePickedFileAndOpensAtTheReleaseFolder()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.ReleasePath = ReleaseFolder;
        string executable = Path.Combine(ReleaseFolder, "Widget.exe");
        _pathPicker.PickExecutableAsync(Arg.Any<string?>()).Returns(executable);

        await viewModel.BrowseExecutableCommand.ExecuteAsync(null);

        Assert.Equal(executable, viewModel.ExecutablePath);
        _ = _pathPicker.Received(1).PickExecutableAsync(ReleaseFolder);
    }

    [Fact]
    public void BrowseIcon_And_BrowseExecutable_CannotRunWithoutAReleaseFolder()
    {
        QuickStartViewModel viewModel = CreateViewModel();

        // The output folder depends on nothing, so its picker is open from the start — unlike the two
        // that need somewhere to start browsing from.
        Assert.True(viewModel.BrowseReleasePathCommand.CanExecute(null));
        Assert.True(viewModel.BrowseOutputPathCommand.CanExecute(null));
        Assert.False(viewModel.BrowseIconCommand.CanExecute(null));
        Assert.False(viewModel.BrowseExecutableCommand.CanExecute(null));

        viewModel.ReleasePath = ReleaseFolder;

        Assert.True(viewModel.BrowseIconCommand.CanExecute(null));
        Assert.True(viewModel.BrowseExecutableCommand.CanExecute(null));
    }

    // ---- what crosses the seam -----------------------------------------------------------------

    [Fact]
    public void ToSettings_CarriesTheRelativeExecutablePath_NotTheAbsoluteOne()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();
        viewModel.ExecutablePath = Path.Combine(ReleaseFolder, "bin", "Widget.exe");

        QuickStartSettings settings = viewModel.ToSettings();

        Assert.Equal(@"bin\Widget.exe", settings.ExecutableRelativePath);
    }

    [Fact]
    public void ToSettings_TrimsEveryAnswer()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();
        viewModel.AppName = "  Widget  ";
        viewModel.Version = " 2.1.0 ";
        viewModel.Manufacturer = " Contoso AG ";
        viewModel.OutputPath = $"  {OutputFolder}  ";

        QuickStartSettings settings = viewModel.ToSettings();

        Assert.Equal("Widget", settings.AppName);
        Assert.Equal("2.1.0", settings.Version);
        Assert.Equal("Contoso AG", settings.Manufacturer);
        Assert.Equal(ReleaseFolder, settings.ReleasePath);
        Assert.Equal(OutputFolder, settings.OutputPath);
    }

    [Fact]
    public void ToSettings_CarriesTheEnteredOutputFolder()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        QuickStartSettings settings = viewModel.ToSettings();

        Assert.Equal(OutputFolder, settings.OutputPath);
    }

    [Fact]
    public void CanApply_AcceptsAnOutputFolderInsideTheReleaseFolder()
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();

        viewModel.OutputPath = Path.Combine(ReleaseFolder, "installer");

        Assert.True(viewModel.CanApply);
    }

    // ---- helpers -------------------------------------------------------------------------------

    private QuickStartViewModel CreateViewModel() => new(_pathPicker);

    private QuickStartViewModel CreateCompleteViewModel()
    {
        QuickStartViewModel viewModel = CreateViewModel();
        viewModel.AppName = "Widget";
        viewModel.Version = "2.1.0";
        viewModel.Manufacturer = "Contoso AG";
        viewModel.ReleasePath = ReleaseFolder;
        viewModel.OutputPath = OutputFolder;
        viewModel.IconPath = Path.Combine(Root, "art", "app.ico");
        viewModel.ExecutablePath = Path.Combine(ReleaseFolder, "Widget.exe");

        return viewModel;
    }

    private bool WithoutAnswer(Action<QuickStartViewModel> blank)
    {
        QuickStartViewModel viewModel = CreateCompleteViewModel();
        blank(viewModel);

        return viewModel.CanApply;
    }
}
