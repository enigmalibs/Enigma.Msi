using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Avalonia.Desktop.Controls.InfoBar;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Msi.Build;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;
using Enigma.Msi.Validation;
using Microsoft.Extensions.Logging;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// The window: the file commands around the form, the validate/build/cancel cycle, and the live build
/// log.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>Extension of a saved package profile, including the leading dot.</summary>
    public const string ProfileExtension = ".msipkg.json";

    private readonly IMsiPackageValidator _validator;
    private readonly IMsiBuildService _buildService;
    private readonly IPathPickerService _pathPicker;
    private readonly IContentDialogService _contentDialogService;
    private readonly IInfoBarService _infoBarService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<MainWindowViewModel> _logger;

    private CancellationTokenSource? _buildCancellation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private string? _currentFilePath;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelBuildCommand))]
    private bool _isBuilding;

    /// <summary>Creates the window's ViewModel.</summary>
    /// <param name="package">The form this window edits.</param>
    /// <param name="validator">Checks the package before a build, and on demand.</param>
    /// <param name="buildService">Runs the pre-flight check and the build.</param>
    /// <param name="pathPicker">Answers the open/save file questions.</param>
    /// <param name="contentDialogService">Reports a failed file operation.</param>
    /// <param name="infoBarService">Reports the outcome of validate and build.</param>
    /// <param name="uiDispatcher">Marshals streamed log lines onto the UI thread.</param>
    /// <param name="logger">Records what the user did and what failed.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public MainWindowViewModel(
        PackageEditorViewModel package,
        IMsiPackageValidator validator,
        IMsiBuildService buildService,
        IPathPickerService pathPicker,
        IContentDialogService contentDialogService,
        IInfoBarService infoBarService,
        IUiDispatcher uiDispatcher,
        ILogger<MainWindowViewModel> logger)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _buildService = buildService ?? throw new ArgumentNullException(nameof(buildService));
        _pathPicker = pathPicker ?? throw new ArgumentNullException(nameof(pathPicker));
        _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));
        _infoBarService = infoBarService ?? throw new ArgumentNullException(nameof(infoBarService));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ValidationErrors.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasValidationErrors));

        // As-you-type gating: only the in-memory rules, never the disk ones — that separation is
        // exactly what IMsiPackageValidator's two methods exist for.
        Package.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(IsPackageValid));
            BuildCommand.NotifyCanExecuteChanged();
        };
    }

    /// <summary>The form holding the package being edited.</summary>
    public PackageEditorViewModel Package { get; }

    /// <summary>
    /// The build log, one line per entry. Append-only by construction: a line costs one
    /// <see cref="ObservableCollection{T}.Add(T)"/>, never a re-concatenation of everything logged so
    /// far — which is how the predecessor UI turned a long build into quadratic work.
    /// </summary>
    public ObservableCollection<string> BuildLog { get; } = [];

    /// <summary>
    /// Everything wrong with the package as of the last validate or build attempt — input errors
    /// first, then the validator's, each with its member path.
    /// </summary>
    public ObservableCollection<MsiValidationError> ValidationErrors { get; } = [];

    /// <summary>Whether the last validate or build attempt found anything to report.</summary>
    public bool HasValidationErrors => ValidationErrors.Count > 0;

    /// <summary>
    /// Whether the package passes the in-memory rules — what gates <c>BuildCommand</c>. Deliberately
    /// not the environment rules: those touch the disk, so running them on every keystroke would make
    /// typing a path hit the file system. They run when a build actually starts.
    /// </summary>
    public bool IsPackageValid
        => Package.GetInputErrors().Count == 0 && _validator.Validate(Package.ToPackage()).IsValid;

    /// <summary>The window title, showing the open profile.</summary>
    public string Title => CurrentFilePath is null
        ? "Enigma.Msi — new package"
        : $"Enigma.Msi — {Path.GetFileName(CurrentFilePath)}";

    /// <summary>Starts a new, empty package. Discards whatever is in the form.</summary>
    [RelayCommand]
    private void New()
    {
        Package.Reset();
        ValidationErrors.Clear();
        BuildLog.Clear();
        CurrentFilePath = null;
        StatusMessage = "New package.";
    }

    /// <summary>Opens a <c>.msipkg.json</c> profile into the form.</summary>
    [RelayCommand]
    private async Task OpenAsync()
    {
        if (await _pathPicker.PickProfileToOpenAsync().ConfigureAwait(true) is not { } path)
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            MsiPackage package = MsiPackageJson.Deserialize(json);

            Package.LoadFrom(package);
            ValidationErrors.Clear();
            CurrentFilePath = path;
            StatusMessage = $"Opened {Path.GetFileName(path)}.";
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or JsonException)
        {
            _logger.LogError(exception, "Could not open the profile {Path}.", path);
            await ReportFileFailureAsync("Could not open the profile", path, exception).ConfigureAwait(true);
        }
    }

    /// <summary>Saves to the open profile, asking for a path the first time.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentFilePath is not { } path)
        {
            await SaveAsAsync().ConfigureAwait(true);
            return;
        }

        await WriteProfileAsync(path).ConfigureAwait(true);
    }

    /// <summary>Saves to a profile path the user chooses.</summary>
    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (await _pathPicker.PickProfileToSaveAsync(SuggestedFileName()).ConfigureAwait(true) is not { } path)
        {
            return;
        }

        await WriteProfileAsync(path).ConfigureAwait(true);
    }

    /// <summary>Reports every problem with the package, without touching the worker.</summary>
    [RelayCommand]
    private async Task ValidateAsync()
    {
        int count = RefreshValidationErrors();

        if (count == 0)
        {
            StatusMessage = "The package is valid.";
            await ShowInfoBarAsync("Valid", "The package passed every check.", InfoBarSeverity.Success)
                .ConfigureAwait(true);
            return;
        }

        StatusMessage = FormatProblemCount(count);
        await ShowInfoBarAsync("Not valid", FormatProblemCount(count), InfoBarSeverity.Error)
            .ConfigureAwait(true);
    }

    /// <summary>Validates, runs the pre-flight check, then builds the MSI through the worker.</summary>
    [RelayCommand(CanExecute = nameof(CanBuild))]
    private async Task BuildAsync()
    {
        int problems = RefreshValidationErrors();

        if (problems > 0)
        {
            StatusMessage = FormatProblemCount(problems);
            await ShowInfoBarAsync(
                    "Cannot build",
                    $"{FormatProblemCount(problems)} Fix them and try again.",
                    InfoBarSeverity.Error)
                .ConfigureAwait(true);
            return;
        }

        _buildCancellation = new CancellationTokenSource();
        IsBuilding = true;

        try
        {
            CancellationToken cancellationToken = _buildCancellation.Token;

            MsiPrerequisites prerequisites = await _buildService
                .CheckPrerequisitesAsync(cancellationToken)
                .ConfigureAwait(true);

            if (!prerequisites.IsSatisfied)
            {
                foreach (string problem in prerequisites.Problems)
                {
                    AppendLog(problem);
                }

                StatusMessage = "The build environment is not ready.";
                await ShowInfoBarAsync(
                        "Cannot build",
                        string.Join(" ", prerequisites.Problems),
                        InfoBarSeverity.Error)
                    .ConfigureAwait(true);
                return;
            }

            StatusMessage = "Building…";
            AppendLog($"Building with worker {prerequisites.WorkerPath}.");

            MsiBuildResult result = await _buildService
                .BuildAsync(Package.ToPackage(), new LogSink(AppendLog), cancellationToken)
                .ConfigureAwait(true);

            await ReportBuildResultAsync(result).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Build cancelled.");
            StatusMessage = "Build cancelled.";
            await ShowInfoBarAsync("Cancelled", "The build was cancelled.", InfoBarSeverity.Warning)
                .ConfigureAwait(true);
        }
        finally
        {
            IsBuilding = false;
            _buildCancellation.Dispose();
            _buildCancellation = null;
        }
    }

    /// <summary>Cancels the running build, killing the worker and its children.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelBuild))]
    private void CancelBuild()
    {
        StatusMessage = "Cancelling…";
        _buildCancellation?.Cancel();
    }

    /// <summary>Empties the build log.</summary>
    [RelayCommand]
    private void ClearLog() => BuildLog.Clear();

    private bool CanBuild => !IsBuilding && IsPackageValid;

    private bool CanCancelBuild => IsBuilding;

    private async Task ReportBuildResultAsync(MsiBuildResult result)
    {
        if (result.Success)
        {
            AppendLog($"Built {result.MsiPath}.");
            StatusMessage = $"Built {result.MsiPath}.";
            await ShowInfoBarAsync("Build succeeded", result.MsiPath ?? string.Empty, InfoBarSeverity.Success)
                .ConfigureAwait(true);
            return;
        }

        foreach (string error in result.Errors)
        {
            AppendLog(error);
        }

        _logger.LogWarning("The build failed with {ErrorCount} error(s).", result.Errors.Count);
        StatusMessage = "The build failed.";
        await ShowInfoBarAsync(
                "Build failed",
                result.Errors.Count == 0 ? "The worker reported no reason." : result.Errors[0],
                InfoBarSeverity.Error)
            .ConfigureAwait(true);
    }

    private async Task WriteProfileAsync(string path)
    {
        try
        {
            File.WriteAllText(path, MsiPackageJson.Serialize(Package.ToPackage()));

            CurrentFilePath = path;
            StatusMessage = $"Saved {Path.GetFileName(path)}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(exception, "Could not save the profile {Path}.", path);
            await ReportFileFailureAsync("Could not save the profile", path, exception).ConfigureAwait(true);
        }
    }

    // Both rule sets plus the form's own unrepresentable-text errors, which the validator cannot see.
    private int RefreshValidationErrors()
    {
        ValidationErrors.Clear();

        IEnumerable<MsiValidationError> errors = Package
            .GetInputErrors()
            .Concat(_validator.ValidateAll(Package.ToPackage()).Errors);

        foreach (MsiValidationError error in errors)
        {
            ValidationErrors.Add(error);
        }

        return ValidationErrors.Count;
    }

    private string SuggestedFileName()
    {
        if (CurrentFilePath is { } path)
        {
            return Path.GetFileName(path);
        }

        string stem = string.IsNullOrWhiteSpace(Package.MsiFilename)
            ? string.IsNullOrWhiteSpace(Package.AppName) ? "package" : Package.AppName
            : Package.MsiFilename;

        return stem + ProfileExtension;
    }

    private Task ReportFileFailureAsync(string title, string path, Exception exception)
        => _contentDialogService.ShowMessageAsync(
            title,
            $"{path}{Environment.NewLine}{Environment.NewLine}{exception.Message}",
            "OK");

    private Task ShowInfoBarAsync(string title, string message, InfoBarSeverity severity)
        => _infoBarService.ShowAsync(bar =>
        {
            bar.Title = title;
            bar.Message = message;
            bar.Severity = severity;
        });

    private void AppendLog(string line)
        => _uiDispatcher.Post(() => BuildLog.Add(line));

    private static string FormatProblemCount(int count)
        => string.Format(
            CultureInfo.CurrentCulture,
            count == 1 ? "{0} problem found." : "{0} problems found.",
            count);

    /// <summary>
    /// Hands each streamed worker line straight to <see cref="AppendLog"/>. Deliberately not
    /// <see cref="Progress{T}"/>: that type posts to the synchronization context captured at
    /// construction, which makes the hand-off asynchronous even when the caller is already on the UI
    /// thread — untestable without a dispatcher, and needless when <see cref="IUiDispatcher"/> already
    /// owns the marshalling decision.
    /// </summary>
    private sealed class LogSink : IProgress<string>
    {
        private readonly Action<string> _append;

        public LogSink(Action<string> append) => _append = append;

        public void Report(string value) => _append(value);
    }
}
