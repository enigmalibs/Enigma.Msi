using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Controls.InfoBar;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Msi.Build;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;
using Enigma.Msi.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the window: command gating, the validate/pre-flight/build/cancel cycle, the append-only log,
/// and the file commands driven through a substituted picker over real temp files.
/// </summary>
public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "enigma-msi-desktop-tests",
        Guid.NewGuid().ToString("N"));

    private readonly IPathPickerService _pathPicker = Substitute.For<IPathPickerService>();
    private readonly IContentDialogService _contentDialogService = Substitute.For<IContentDialogService>();
    private readonly IInfoBarService _infoBarService = Substitute.For<IInfoBarService>();
    private readonly IBuildProgressService _buildProgress = Substitute.For<IBuildProgressService>();
    private readonly IAboutDialogService _aboutDialog = Substitute.For<IAboutDialogService>();
    private readonly IQuickStartDialogService _quickStartDialog = Substitute.For<IQuickStartDialogService>();
    private readonly FakeBuildService _buildService = new();

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    // ---- gating -------------------------------------------------------------------------------

    [Fact]
    public void Build_IsDisabledForAnIncompletePackage()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.IsPackageValid);
        Assert.False(viewModel.BuildCommand.CanExecute(null));
    }

    [Fact]
    public void Build_BecomesAvailableAsTheLastMissingFieldIsFilledIn()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage valid = TestPackages.CreateMinimalValid();
        viewModel.Package.LoadFrom(valid);

        Assert.True(viewModel.BuildCommand.CanExecute(null));

        viewModel.Package.AppName = string.Empty;

        Assert.False(viewModel.BuildCommand.CanExecute(null));

        viewModel.Package.AppName = valid.AppName;

        Assert.True(viewModel.BuildCommand.CanExecute(null));
    }

    [Fact]
    public void Build_IsGatedOnTheInMemoryRulesOnly_NotOnPathsExisting()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage valid = TestPackages.CreateMinimalValid();
        valid.Install.ReleasePath = Path.Combine(_tempDirectory, "does-not-exist");
        viewModel.Package.LoadFrom(valid);

        // Typing a path that is not there yet must not disable Build as you type; the missing folder is
        // reported when the build actually runs.
        Assert.True(viewModel.BuildCommand.CanExecute(null));
    }

    [Fact]
    public void Cancel_IsDisabledWhileNoBuildIsRunning()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.CancelBuildCommand.CanExecute(null));
    }

    // ---- validate -----------------------------------------------------------------------------

    [Fact]
    public async Task Validate_ReportsEveryProblemWithItsMemberPath()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        await viewModel.ValidateCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Path == "appName");
        Assert.Contains(viewModel.ValidationErrors, error => error.Path == "manufacturer");
        Assert.Contains(viewModel.ValidationErrors, error => error.Path == "install.installPath");
        Assert.All(viewModel.ValidationErrors, error => Assert.False(string.IsNullOrWhiteSpace(error.Message)));
        _ = _infoBarService.Received().ShowAsync(Arg.Any<Action<InfoBar>>());
    }

    [Fact]
    public async Task Validate_ReportsTheFormsOwnUnparseableTextFirst()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(TestPackages.CreateMinimalValid());
        viewModel.Package.Version = "not a version";

        await viewModel.ValidateCommand.ExecuteAsync(null);

        MsiValidationError first = viewModel.ValidationErrors[0];
        Assert.Equal("version", first.Path);
        Assert.Contains("not a version", first.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_AlsoRunsTheEnvironmentRules()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = Path.Combine(_tempDirectory, "missing-release");
        viewModel.Package.LoadFrom(package);

        await viewModel.ValidateCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.ValidationErrors, error => error.Path == "install.releasePath");
    }

    [Fact]
    public async Task Validate_ClearsTheProblemsOnceTheyAreFixed()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        await viewModel.ValidateCommand.ExecuteAsync(null);
        Assert.True(viewModel.HasValidationErrors);

        viewModel.Package.LoadFrom(CreateBuildablePackage());
        await viewModel.ValidateCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasValidationErrors);
        Assert.Equal("The package is valid.", viewModel.StatusMessage);
    }

    // ---- build --------------------------------------------------------------------------------

    [Fact]
    public async Task Build_HandsTheWorkerTheMaterializedPackageAndReportsTheMsiPath()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage package = CreateBuildablePackage();
        viewModel.Package.LoadFrom(package);
        _buildService.Result = MsiBuildResult.Succeeded(@"C:\out\Widget.msi");

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Equal(1, _buildService.BuildCount);
        Assert.NotNull(_buildService.LastPackage);
        Assert.Equal(MsiPackageJson.Serialize(package), MsiPackageJson.Serialize(_buildService.LastPackage));
        Assert.Contains(@"Built C:\out\Widget.msi.", viewModel.BuildLog);
        Assert.False(viewModel.IsBuilding);
    }

    [Fact]
    public async Task Build_StreamsTheWorkersOutputIntoTheLogInOrder()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        string[] lines = [.. Enumerable.Range(0, 500).Select(index => $"line {index}")];
        _buildService.LogLines = lines;

        await viewModel.BuildCommand.ExecuteAsync(null);

        // Append-only: every line arrives once, in order, with the worker-path line ahead of them.
        List<string> streamed = [.. viewModel.BuildLog.Where(line => line.StartsWith("line ", StringComparison.Ordinal))];
        Assert.Equal(lines, streamed);
    }

    [Fact]
    public async Task Build_ReportsEveryReasonAFailedBuildGaveBack()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.Result = MsiBuildResult.Failed(["wix said no", "and also this"]);

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Contains("wix said no", viewModel.BuildLog);
        Assert.Contains("and also this", viewModel.BuildLog);
        Assert.Equal("The build failed.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Build_StopsAtAFailedPreFlightCheckWithItsActionableMessage()
    {
        const string Problem = "The WiX CLI was not found. Run: dotnet tool install --global wix";

        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.Prerequisites = new MsiPrerequisites
        {
            WorkerPath = @"C:\app\worker\Enigma.Msi.Worker.exe",
            Problems = [Problem]
        };

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Equal(0, _buildService.BuildCount);
        Assert.Contains(Problem, viewModel.BuildLog);
        Assert.Equal("The build environment is not ready.", viewModel.StatusMessage);
        _ = _infoBarService.Received().ShowAsync(Arg.Any<Action<InfoBar>>());
    }

    [Fact]
    public async Task Build_RefusesAPackageWhoseFoldersAreMissing()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = Path.Combine(_tempDirectory, "missing-release");
        package.Output.OutputPath = Path.Combine(_tempDirectory, "missing-output");
        viewModel.Package.LoadFrom(package);

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Equal(0, _buildService.BuildCount);
        Assert.Contains(viewModel.ValidationErrors, error => error.Path == "install.releasePath");
    }

    [Fact]
    public async Task Cancel_TerminatesTheRunningBuildAndSaysSo()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.WaitForCancellation = true;

        Task build = viewModel.BuildCommand.ExecuteAsync(null);

        // Bounded: a build that never starts is a failure to report, not a suite that hangs.
        await _buildService.BuildStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsBuilding);
        Assert.True(viewModel.CancelBuildCommand.CanExecute(null));
        Assert.False(viewModel.BuildCommand.CanExecute(null));

        viewModel.CancelBuildCommand.Execute(null);
        await build;

        Assert.False(viewModel.IsBuilding);
        Assert.False(viewModel.CancelBuildCommand.CanExecute(null));
        Assert.Contains("Build cancelled.", viewModel.BuildLog);
        Assert.Equal("Build cancelled.", viewModel.StatusMessage);
    }

    // ---- the build overlay --------------------------------------------------------------------

    [Fact]
    public async Task Build_ShowsTheOverlayOnce_DrivenByTheCancelCommand()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());

        await viewModel.BuildCommand.ExecuteAsync(null);

        // The overlay's Cancel is the only cancel affordance while a build runs — the toolbar has none.
        _ = _buildProgress.Received(1).ShowAsync(viewModel.CancelBuildCommand);
    }

    [Fact]
    public async Task Build_ShowsNoOverlayForAPackageThatDoesNotEvenValidate()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        await viewModel.BuildCommand.ExecuteAsync(null);

        _ = _buildProgress.DidNotReceive().ShowAsync(Arg.Any<ICommand>());
    }

    [Fact]
    public async Task Build_TakesTheOverlayDownBeforeReportingSuccess()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.Result = MsiBuildResult.Succeeded(@"C:\out\Widget.msi");
        List<string> order = RecordOverlayAndReportingOrder();

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Equal("hide", order[0]);
        Assert.Contains("report", order);
    }

    [Fact]
    public async Task Build_TakesTheOverlayDownBeforeReportingAFailedBuild()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.Result = MsiBuildResult.Failed(["wix said no"]);
        List<string> order = RecordOverlayAndReportingOrder();

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Equal("hide", order[0]);
        Assert.Contains("report", order);
    }

    [Fact]
    public async Task Build_TakesTheOverlayDownBeforeReportingAFailedPreFlightCheck()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.Prerequisites = new MsiPrerequisites
        {
            WorkerPath = @"C:\app\worker\Enigma.Msi.Worker.exe",
            Problems = ["The WiX CLI was not found. Run: dotnet tool install --global wix"]
        };
        List<string> order = RecordOverlayAndReportingOrder();

        await viewModel.BuildCommand.ExecuteAsync(null);

        Assert.Equal("hide", order[0]);
        Assert.Contains("report", order);
    }

    [Fact]
    public async Task Build_TakesTheOverlayDownWhenTheBuildIsCancelled()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.WaitForCancellation = true;
        List<string> order = RecordOverlayAndReportingOrder();

        Task build = viewModel.BuildCommand.ExecuteAsync(null);
        await _buildService.BuildStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken);
        viewModel.CancelBuildCommand.Execute(null);
        await build;

        Assert.Equal("hide", order[0]);
        Assert.Contains("report", order);
    }

    [Fact]
    public async Task Build_FeedsEveryStreamedLineToTheOverlaysMessage()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(CreateBuildablePackage());
        _buildService.LogLines = ["compiling", "linking", "done"];

        await viewModel.BuildCommand.ExecuteAsync(null);

        // The card shows the latest line, so every line has to reach it — in the same post that appends it
        // to the log, which is what keeps the marshalling to one hop.
        _buildProgress.Received(1).ReportMessage("compiling");
        _buildProgress.Received(1).ReportMessage("linking");
        _buildProgress.Received(1).ReportMessage("done");
    }

    [Fact]
    public void Constructor_RejectsAMissingBuildProgressService()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new PackageEditorViewModel(_pathPicker),
            new MsiPackageValidator(),
            _buildService,
            _pathPicker,
            _contentDialogService,
            _infoBarService,
            new InlineUiDispatcher(),
            null!,
            _aboutDialog,
            _quickStartDialog,
            NullLogger<MainWindowViewModel>.Instance));

        Assert.Equal("buildProgress", exception.ParamName);
    }

    [Fact]
    public void ClearLog_EmptiesTheLog()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.BuildLog.Add("something");

        viewModel.ClearLogCommand.Execute(null);

        Assert.Empty(viewModel.BuildLog);
    }

    // ---- about --------------------------------------------------------------------------------

    [Fact]
    public async Task ShowAbout_OpensTheAboutDialog()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        await viewModel.ShowAboutCommand.ExecuteAsync(null);

        _ = _aboutDialog.Received(1).ShowAsync();
    }

    [Fact]
    public void Constructor_RejectsAMissingAboutDialogService()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new PackageEditorViewModel(_pathPicker),
            new MsiPackageValidator(),
            _buildService,
            _pathPicker,
            _contentDialogService,
            _infoBarService,
            new InlineUiDispatcher(),
            _buildProgress,
            null!,
            _quickStartDialog,
            NullLogger<MainWindowViewModel>.Instance));

        Assert.Equal("aboutDialog", exception.ParamName);
    }

    // ---- quick start --------------------------------------------------------------------------

    [Fact]
    public async Task QuickStart_OpensTheDialogOnce()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        _quickStartDialog.ShowAsync().Returns((QuickStartSettings?)null);

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        _ = _quickStartDialog.Received(1).ShowAsync();
    }

    [Fact]
    public async Task QuickStart_Cancelled_ChangesNothingAndAsksNothing()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Widget";
        _quickStartDialog.ShowAsync().Returns((QuickStartSettings?)null);

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        Assert.Equal("Widget", viewModel.Package.AppName);
        Assert.Empty(viewModel.Package.Shortcuts);
        _ = _contentDialogService.DidNotReceive().ShowAsync(Arg.Any<Action<ContentDialog>>());
    }

    [Fact]
    public async Task QuickStart_OnAnEmptyForm_AppliesWithoutAskingForConfirmation()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        _quickStartDialog.ShowAsync().Returns(CreateSettings());

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        Assert.Equal("Widget", viewModel.Package.AppName);
        Assert.Equal(2, viewModel.Package.Shortcuts.Count);
        _ = _contentDialogService.DidNotReceive().ShowAsync(Arg.Any<Action<ContentDialog>>());
    }

    [Fact]
    public async Task QuickStart_OverAFormWithData_AsksFirstAndAppliesOnYes()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Something else";
        viewModel.CurrentFilePath = TempPath("open.msipkg.json");
        _quickStartDialog.ShowAsync().Returns(CreateSettings());
        _contentDialogService.ShowAsync(Arg.Any<Action<ContentDialog>>()).Returns(DialogResult.Primary);

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        _ = _contentDialogService.Received(1).ShowAsync(Arg.Any<Action<ContentDialog>>());
        Assert.Equal("Widget", viewModel.Package.AppName);
        Assert.Null(viewModel.CurrentFilePath);
    }

    [Fact]
    public async Task QuickStart_OverAFormWithData_LeavesItAloneOnNo()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Something else";
        _quickStartDialog.ShowAsync().Returns(CreateSettings());
        _contentDialogService.ShowAsync(Arg.Any<Action<ContentDialog>>()).Returns(DialogResult.Close);

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        Assert.Equal("Something else", viewModel.Package.AppName);
        Assert.Empty(viewModel.Package.Shortcuts);
    }

    [Fact]
    public async Task QuickStart_OverAFormWithData_TreatsADismissedConfirmationAsNo()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Something else";
        _quickStartDialog.ShowAsync().Returns(CreateSettings());
        _contentDialogService.ShowAsync(Arg.Any<Action<ContentDialog>>()).Returns(DialogResult.None);

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        Assert.Equal("Something else", viewModel.Package.AppName);
    }

    [Fact]
    public async Task QuickStart_Applied_ClearsTheProblemsAndTheOpenPath()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        await viewModel.ValidateCommand.ExecuteAsync(null);
        Assert.NotEmpty(viewModel.ValidationErrors);
        viewModel.CurrentFilePath = TempPath("open.msipkg.json");
        _quickStartDialog.ShowAsync().Returns(CreateSettings());

        await viewModel.QuickStartCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.ValidationErrors);
        Assert.Null(viewModel.CurrentFilePath);
    }

    [Fact]
    public async Task ShowQuickStartOnStartup_OpensTheDialogOnceAndNeverAgain()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        _quickStartDialog.ShowAsync().Returns((QuickStartSettings?)null);

        await viewModel.ShowQuickStartOnStartupCommand.ExecuteAsync(null);
        await viewModel.ShowQuickStartOnStartupCommand.ExecuteAsync(null);

        _ = _quickStartDialog.Received(1).ShowAsync();
    }

    [Fact]
    public async Task ShowQuickStartOnStartup_StaysOutOfTheWayOfAFormWithData()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Widget";

        await viewModel.ShowQuickStartOnStartupCommand.ExecuteAsync(null);

        _ = _quickStartDialog.DidNotReceive().ShowAsync();
    }

    [Fact]
    public async Task ShowQuickStartOnStartup_StaysOutOfTheWayOfAnOpenProfile()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.CurrentFilePath = TempPath("open.msipkg.json");

        await viewModel.ShowQuickStartOnStartupCommand.ExecuteAsync(null);

        _ = _quickStartDialog.DidNotReceive().ShowAsync();
    }

    // The headline promise of the whole feature: six answers in, a buildable package out. Run against
    // real folders so the *environment* rules are exercised too — those are the ones the derivations
    // exist to satisfy, and the only ones that can tell whether the output folder landed somewhere that
    // exists.
    [Fact]
    public async Task QuickStart_Applied_YieldsAPackageThatValidatesCleanlyAndEnablesBuild()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        _quickStartDialog.ShowAsync().Returns(CreateSettingsForARealFolder());

        await viewModel.QuickStartCommand.ExecuteAsync(null);
        await viewModel.ValidateCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.ValidationErrors);
        Assert.True(viewModel.BuildCommand.CanExecute(null));
    }

    [Fact]
    public void Constructor_RejectsAMissingQuickStartDialogService()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            new PackageEditorViewModel(_pathPicker),
            new MsiPackageValidator(),
            _buildService,
            _pathPicker,
            _contentDialogService,
            _infoBarService,
            new InlineUiDispatcher(),
            _buildProgress,
            _aboutDialog,
            null!,
            NullLogger<MainWindowViewModel>.Instance));

        Assert.Equal("quickStartDialog", exception.ParamName);
    }

    // ---- files --------------------------------------------------------------------------------

    [Fact]
    public async Task SaveAs_WritesTheProfileAndAdoptsThePath()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage package = TestPackages.CreateFull();
        viewModel.Package.LoadFrom(package);
        string path = TempPath("Widget.msipkg.json");
        _pathPicker.PickProfileToSaveAsync(Arg.Any<string?>()).Returns(path);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.True(File.Exists(path));
        Assert.Equal(MsiPackageJson.Serialize(package), File.ReadAllText(path));
        Assert.Equal(path, viewModel.CurrentFilePath);
        Assert.Contains("Widget.msipkg.json", viewModel.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAs_SuggestsAFileNameBuiltFromTheMsiFileName()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.MsiFilename = "ContosoWidget";
        _pathPicker.PickProfileToSaveAsync(Arg.Any<string?>()).Returns((string?)null);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        _ = _pathPicker.Received().PickProfileToSaveAsync("ContosoWidget" + MainWindowViewModel.ProfileExtension);
    }

    [Fact]
    public async Task SaveAs_Cancelled_WritesNothing()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        _pathPicker.PickProfileToSaveAsync(Arg.Any<string?>()).Returns((string?)null);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.Null(viewModel.CurrentFilePath);
        Assert.Equal("Ready.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Save_AsksForAPathOnlyTheFirstTime()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(TestPackages.CreateMinimalValid());
        string path = TempPath("Widget.msipkg.json");
        _pathPicker.PickProfileToSaveAsync(Arg.Any<string?>()).Returns(path);

        await viewModel.SaveCommand.ExecuteAsync(null);
        viewModel.Package.AppName = "Renamed";
        await viewModel.SaveCommand.ExecuteAsync(null);

        _ = _pathPicker.Received(1).PickProfileToSaveAsync(Arg.Any<string?>());
        Assert.Equal("Renamed", MsiPackageJson.Deserialize(File.ReadAllText(path)).AppName);
    }

    [Fact]
    public async Task Open_LoadsTheProfileIntoTheForm()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MsiPackage package = TestPackages.CreateFull();
        string path = TempPath("Widget.msipkg.json");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, MsiPackageJson.Serialize(package));
        _pathPicker.PickProfileToOpenAsync().Returns(path);

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Equal(package.AppName, viewModel.Package.AppName);
        Assert.Equal(MsiPackageJson.Serialize(package), MsiPackageJson.Serialize(viewModel.Package.ToPackage()));
        Assert.Equal(path, viewModel.CurrentFilePath);
    }

    [Fact]
    public async Task Open_ABrokenProfile_ReportsItAndLeavesTheFormAlone()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Untouched";
        string path = TempPath("broken.msipkg.json");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, "{ this is not JSON");
        _pathPicker.PickProfileToOpenAsync().Returns(path);

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Equal("Untouched", viewModel.Package.AppName);
        Assert.Null(viewModel.CurrentFilePath);
        _ = _contentDialogService.Received().ShowMessageAsync(
            "Could not open the profile",
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Open_Cancelled_ChangesNothing()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.AppName = "Untouched";
        _pathPicker.PickProfileToOpenAsync().Returns((string?)null);

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Equal("Untouched", viewModel.Package.AppName);
        Assert.Null(viewModel.CurrentFilePath);
    }

    [Fact]
    public void New_ClearsTheForm_TheLog_TheProblemsAndThePath()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Package.LoadFrom(TestPackages.CreateFull());
        viewModel.BuildLog.Add("stale");
        viewModel.ValidationErrors.Add(new MsiValidationError("appName", "stale"));

        viewModel.NewCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Package.AppName);
        Assert.Empty(viewModel.BuildLog);
        Assert.Empty(viewModel.ValidationErrors);
        Assert.Null(viewModel.CurrentFilePath);
        Assert.Equal("Enigma.Msi — new package", viewModel.Title);
    }

    // ---- helpers ------------------------------------------------------------------------------

    private MainWindowViewModel CreateViewModel() => new(
        new PackageEditorViewModel(_pathPicker),
        new MsiPackageValidator(),
        _buildService,
        _pathPicker,
        _contentDialogService,
        _infoBarService,
        new InlineUiDispatcher(),
        _buildProgress,
        _aboutDialog,
        _quickStartDialog,
        NullLogger<MainWindowViewModel>.Instance);

    // The overlay is modal, so *when* it comes down matters as much as that it does: an outcome reported
    // while it is still up is unreadable behind the dimming. Recording the two calls in one list is what
    // lets a test assert the order without pinning down how many times each is made.
    private List<string> RecordOverlayAndReportingOrder()
    {
        List<string> order = [];

        _buildProgress.When(progress => progress.HideAsync()).Do(_ => order.Add("hide"));
        _infoBarService
            .When(bar => bar.ShowAsync(Arg.Any<Action<InfoBar>>()))
            .Do(_ => order.Add("report"));

        return order;
    }

    // A package whose folders exist, so the environment rules pass too and a build can start. The
    // release folder needs a file in it: an empty one is a validation error, since a package with
    // nothing to install is not what anyone meant.
    private MsiPackage CreateBuildablePackage()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = Path.Combine(_tempDirectory, "release");
        package.Output.OutputPath = Path.Combine(_tempDirectory, "artifacts");
        Directory.CreateDirectory(package.Install.ReleasePath);
        Directory.CreateDirectory(package.Output.OutputPath);
        File.WriteAllText(Path.Combine(package.Install.ReleasePath, "Widget.exe"), "not really an executable");

        return package;
    }

    // Six answers the dialog would have validated before handing them over — the relative executable
    // path is what crosses the seam, not the absolute one.
    private static QuickStartSettings CreateSettings() => new(
        "Widget",
        "2.1.0",
        "Contoso AG",
        Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "payload", "release"),
        Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "art", "app.ico"),
        @"bin\Widget.exe");

    // A payload folder as a real build would leave it: the executable in a sub-folder, an icon beside it,
    // and a parent directory for the MSI to be written into.
    private QuickStartSettings CreateSettingsForARealFolder()
    {
        string release = Path.Combine(_tempDirectory, "payload", "release");
        Directory.CreateDirectory(Path.Combine(release, "bin"));
        File.WriteAllText(Path.Combine(release, "bin", "Widget.exe"), "not really an executable");
        string icon = Path.Combine(release, "app.ico");
        File.WriteAllText(icon, "not really an icon");

        return new QuickStartSettings("Widget", "2.1.0", "Contoso AG", release, icon, @"bin\Widget.exe");
    }

    private string TempPath(string fileName)
    {
        Directory.CreateDirectory(_tempDirectory);

        return Path.Combine(_tempDirectory, fileName);
    }
}
