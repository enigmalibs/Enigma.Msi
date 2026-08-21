using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Msi.Desktop.Services;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// The quick start's form: six answers, and the rules that decide when they add up to a package.
/// </summary>
/// <remarks>
/// <para>
/// One page, every field visible — not a wizard. The field <em>order</em> is a dependency order rather
/// than a presentation choice: the icon and the executable are both browsed for starting at the release
/// folder, so their browse buttons stay disabled until that folder is known.
/// </para>
/// <para>
/// <see cref="CanApply"/> applies string and parse rules only, and never touches the disk. That mirrors
/// the split the app already lives by — <c>IsPackageValid</c> runs the in-memory rules on every
/// keystroke and leaves the file-system rules to Validate and Build — so a path that does not exist is
/// reported once, by the main form's Problems pane, instead of half-reported here as well.
/// </para>
/// </remarks>
public sealed partial class QuickStartViewModel : ObservableObject
{
    /// <summary>What the dialog says when the chosen executable sits outside the release folder.</summary>
    public const string ExecutableOutsideReleaseFolderMessage =
        "The executable must be inside the release folder — it is what [INSTALLDIR] becomes after installation.";

    private const char WindowsSeparator = '\\';

    private readonly IPathPickerService _pathPicker;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _appName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _version = PackageEditorViewModel.DefaultVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(ExecutableRelativePath))]
    [NotifyPropertyChangedFor(nameof(ExecutableError))]
    [NotifyPropertyChangedFor(nameof(HasExecutableError))]
    [NotifyCanExecuteChangedFor(nameof(BrowseIconCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseExecutableCommand))]
    private string _releasePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _iconPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(ExecutableRelativePath))]
    [NotifyPropertyChangedFor(nameof(ExecutableError))]
    [NotifyPropertyChangedFor(nameof(HasExecutableError))]
    private string _executablePath = string.Empty;

    /// <summary>Creates the form over the picker its three browse buttons drive.</summary>
    /// <param name="pathPicker">Answers the release-folder, icon and executable questions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pathPicker"/> is <see langword="null"/>.</exception>
    public QuickStartViewModel(IPathPickerService pathPicker)
        => _pathPicker = pathPicker ?? throw new ArgumentNullException(nameof(pathPicker));

    /// <summary>
    /// <see cref="ExecutablePath"/> expressed relative to <see cref="ReleasePath"/>, or
    /// <see langword="null"/> when it is not under it.
    /// </summary>
    /// <remarks>
    /// The relative form is what a shortcut target needs: the release folder's contents are what ends up
    /// under <c>[INSTALLDIR]</c>, so a nested executable has to keep its sub-folder. The comparison is
    /// ordinal, case-insensitive and separator-normalized, and the result always uses <c>\</c> — an MSI
    /// path is a Windows path whichever machine builds it.
    /// </remarks>
    public string? ExecutableRelativePath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ExecutablePath) || string.IsNullOrWhiteSpace(ReleasePath))
            {
                return null;
            }

            string root = NormalizeSeparators(ReleasePath) + WindowsSeparator;
            string executable = NormalizeSeparators(ExecutablePath);

            if (!executable.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string relative = executable[root.Length..];

            return relative.Length == 0 ? null : relative;
        }
    }

    /// <summary>
    /// The problem with the chosen executable, or <see langword="null"/> while there is none. Blank is
    /// not a problem to report — an unanswered question is what <see cref="CanApply"/> is for.
    /// </summary>
    public string? ExecutableError
        => !string.IsNullOrWhiteSpace(ExecutablePath) && ExecutableRelativePath is null
            ? ExecutableOutsideReleaseFolderMessage
            : null;

    /// <summary>Whether <see cref="ExecutableError"/> has something to say — what shows the message.</summary>
    public bool HasExecutableError => ExecutableError is not null;

    /// <summary>
    /// Whether the six answers add up to a package: all present, the version parsing, and the
    /// executable inside the release folder.
    /// </summary>
    public bool CanApply
        => !string.IsNullOrWhiteSpace(AppName)
           && !string.IsNullOrWhiteSpace(Version)
           && !string.IsNullOrWhiteSpace(Manufacturer)
           && !string.IsNullOrWhiteSpace(ReleasePath)
           && !string.IsNullOrWhiteSpace(IconPath)
           && !string.IsNullOrWhiteSpace(ExecutablePath)
           && System.Version.TryParse(Version, out _)
           && ExecutableRelativePath is not null;

    /// <summary>Materializes the answers.</summary>
    /// <returns>
    /// The trimmed answers, carrying the executable's <em>relative</em> path. Only meaningful once
    /// <see cref="CanApply"/> is <see langword="true"/>.
    /// </returns>
    public QuickStartSettings ToSettings() => new(
        AppName.Trim(),
        Version.Trim(),
        Manufacturer.Trim(),
        ReleasePath.Trim(),
        IconPath.Trim(),
        ExecutableRelativePath ?? string.Empty);

    [RelayCommand]
    private async Task BrowseReleasePathAsync()
    {
        if (await _pathPicker.PickFolderAsync("Select the folder to package", ReleasePath)
                .ConfigureAwait(true) is { } path)
        {
            ReleasePath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(HasReleasePath))]
    private async Task BrowseIconAsync()
    {
        if (await _pathPicker.PickIconAsync(ReleasePath).ConfigureAwait(true) is { } path)
        {
            IconPath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(HasReleasePath))]
    private async Task BrowseExecutableAsync()
    {
        if (await _pathPicker.PickExecutableAsync(ReleasePath).ConfigureAwait(true) is { } path)
        {
            ExecutablePath = path;
        }
    }

    // Both file pickers open at the release folder, so asking for either before it is known would put
    // the dialog somewhere arbitrary and then compare the answer against nothing.
    private bool HasReleasePath => !string.IsNullOrWhiteSpace(ReleasePath);

    // '/' covers both platforms' other separator — Windows' alternative one and Unix's primary one — so
    // folding it into '\' is all the normalization a comparison of two entered paths needs.
    private static string NormalizeSeparators(string path)
        => path.Replace('/', WindowsSeparator).TrimEnd(WindowsSeparator);
}
