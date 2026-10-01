using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Msi.Desktop.Services;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// The quick start's form: eight answers, and the rules that decide when they add up to a package.
/// </summary>
/// <remarks>
/// <para>
/// One page, every field visible — not a wizard. The field <em>order</em> is still a dependency order
/// rather than a presentation choice: the icon and the executable are both browsed for starting at the
/// release folder, so their browse buttons stay disabled until that folder is known. The output folder
/// sits next to the release folder because that is where it is thought about, not because it depends on
/// it — it is deliberately <em>not</em> derived from it any more, so its own browse button is ungated.
/// </para>
/// <para>
/// The MSI file name is the one answer that arrives pre-filled. It follows the application name and the
/// version as they are typed — see <see cref="MsiFilename"/> — and stops following the moment the user
/// types a name of their own, so a deliberate answer is never overwritten.
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

    /// <summary>What the dialog says when the MSI file name holds a character a file name cannot.</summary>
    public const string MsiFilenameInvalidCharactersMessage =
        "The MSI file name must be a plain file name — no folder separators, and no characters a file name cannot hold.";

    /// <summary>What the dialog says when the MSI file name carries its own extension.</summary>
    public const string MsiFilenameExtensionMessage =
        "Leave the .msi extension off — the build appends it.";

    private const char WindowsSeparator = '\\';

    private const char MsiFilenameSeparator = '.';

    private const string MsiExtension = ".msi";

    private readonly IPathPickerService _pathPicker;

    // What the MSI file name was last derived as. Blank to start with, which is also what an empty
    // application name derives — so the field and this agree before anything has been typed.
    private string _derivedMsiFilename = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _appName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _version = PackageEditorViewModel.DefaultVersion;

    /// <summary>
    /// The <c>.msi</c> file's base name, without the extension. Derived from <see cref="AppName"/> and
    /// <see cref="Version"/> — spaces become dots and the version is appended, so <c>Enigma Msi</c> at
    /// <c>1.4.0</c> gives <c>Enigma.Msi.1.4.0</c> — and kept in step with both for as long as it still holds
    /// what was last derived. A name typed by hand is left alone from then on; emptying the field hands it
    /// back to the derivation at the next edit of either.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(MsiFilenameError))]
    [NotifyPropertyChangedFor(nameof(HasMsiFilenameError))]
    private string _msiFilename = string.Empty;

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
    private string _outputPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string _iconPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(ExecutableRelativePath))]
    [NotifyPropertyChangedFor(nameof(ExecutableError))]
    [NotifyPropertyChangedFor(nameof(HasExecutableError))]
    private string _executablePath = string.Empty;

    /// <summary>Creates the form over the picker its four browse buttons drive.</summary>
    /// <param name="pathPicker">Answers the release-folder, output-folder, icon and executable questions.</param>
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
    /// The problem with the MSI file name, or <see langword="null"/> while there is none. These are the
    /// validator's own two rules on <c>output.msiFilename</c> — both are string rules, so the dialog can
    /// apply them without touching the disk. Blank is not reported, for the same reason as
    /// <see cref="ExecutableError"/>.
    /// </summary>
    public string? MsiFilenameError
    {
        get
        {
            string filename = MsiFilename.Trim();

            if (filename.Length == 0)
            {
                return null;
            }

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return MsiFilenameInvalidCharactersMessage;
            }

            return filename.EndsWith(MsiExtension, StringComparison.OrdinalIgnoreCase)
                ? MsiFilenameExtensionMessage
                : null;
        }
    }

    /// <summary>Whether <see cref="MsiFilenameError"/> has something to say — what shows the message.</summary>
    public bool HasMsiFilenameError => MsiFilenameError is not null;

    /// <summary>
    /// Whether the eight answers add up to a package: all present, the version parsing, the MSI file name
    /// a plain extension-less file name, and the executable inside the release folder.
    /// </summary>
    public bool CanApply
        => !string.IsNullOrWhiteSpace(AppName)
           && !string.IsNullOrWhiteSpace(Version)
           && !string.IsNullOrWhiteSpace(MsiFilename)
           && !string.IsNullOrWhiteSpace(Manufacturer)
           && !string.IsNullOrWhiteSpace(ReleasePath)
           && !string.IsNullOrWhiteSpace(OutputPath)
           && !string.IsNullOrWhiteSpace(IconPath)
           && !string.IsNullOrWhiteSpace(ExecutablePath)
           && System.Version.TryParse(Version, out _)
           && MsiFilenameError is null
           && ExecutableRelativePath is not null;

    /// <summary>Materializes the answers.</summary>
    /// <returns>
    /// The trimmed answers, carrying the executable's <em>relative</em> path. Only meaningful once
    /// <see cref="CanApply"/> is <see langword="true"/>.
    /// </returns>
    public QuickStartSettings ToSettings() => new(
        AppName.Trim(),
        Version.Trim(),
        MsiFilename.Trim(),
        Manufacturer.Trim(),
        ReleasePath.Trim(),
        OutputPath.Trim(),
        IconPath.Trim(),
        ExecutableRelativePath ?? string.Empty);

    partial void OnAppNameChanged(string value) => FollowDerivedMsiFilename();

    partial void OnVersionChanged(string value) => FollowDerivedMsiFilename();

    // The field follows the derivation only while it still shows what the derivation last produced (or
    // nothing at all): that is the whole difference between "not edited yet" and "edited by hand", and it
    // needs no flag to keep in sync with what the user does.
    private void FollowDerivedMsiFilename()
    {
        string derived = ToMsiFilename(AppName, Version);

        if (string.IsNullOrWhiteSpace(MsiFilename)
            || string.Equals(MsiFilename, _derivedMsiFilename, StringComparison.Ordinal))
        {
            MsiFilename = derived;
        }

        _derivedMsiFilename = derived;
    }

    [RelayCommand]
    private async Task BrowseReleasePathAsync()
    {
        if (await _pathPicker.PickFolderAsync("Select the folder to package", ReleasePath)
                .ConfigureAwait(true) is { } path)
        {
            ReleasePath = path;
        }
    }

    // Ungated, unlike the two below: the output folder is asked for in its own right, so there is
    // nothing to know before opening the picker at whatever has been typed so far.
    [RelayCommand]
    private async Task BrowseOutputPathAsync()
    {
        if (await _pathPicker.PickFolderAsync("Select the folder the .msi is written to", OutputPath)
                .ConfigureAwait(true) is { } path)
        {
            OutputPath = path;
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

    // Shaped for the validator's rule on output.msiFilename — a plain file name, no invalid characters, no
    // .msi extension — so a derived name never needs correcting. Blank while the application name yields
    // nothing, rather than a bare version: the dialog would otherwise open on "1.0.0".
    private static string ToMsiFilename(string appName, string version)
    {
        string name = ToFilenameSegment(appName);

        if (name.Length == 0)
        {
            return string.Empty;
        }

        string suffix = ToFilenameSegment(version);
        string filename = suffix.Length == 0 ? name : name + MsiFilenameSeparator + suffix;

        return filename.EndsWith(MsiExtension, StringComparison.OrdinalIgnoreCase)
            ? filename[..^MsiExtension.Length]
            : filename;
    }

    // Characters a file name cannot hold are dropped, and every run of whitespace between two kept
    // characters becomes a single dot — "Enigma  Msi" gives "Enigma.Msi", not "Enigma..Msi" — while
    // leading and trailing whitespace simply goes.
    private static string ToFilenameSegment(string text)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(text.Length);
        bool separatorPending = false;

        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                separatorPending = builder.Length > 0;
            }
            else if (Array.IndexOf(invalid, character) < 0)
            {
                if (separatorPending)
                {
                    _ = builder.Append(MsiFilenameSeparator);
                    separatorPending = false;
                }

                _ = builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
