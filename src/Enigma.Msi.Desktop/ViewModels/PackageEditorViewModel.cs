using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// The form: every member of <see cref="MsiPackage"/>, edited as text and selections.
/// </summary>
/// <remarks>
/// <para>
/// There is no DTO layer between this and the model — <see cref="ToPackage"/> and
/// <see cref="LoadFrom"/> are the only two crossings, and they happen only at the file and build
/// boundaries. What the ViewModel adds over the model is text: <see cref="MsiPackage.Version"/> is a
/// <see cref="System.Version"/> and the two identifiers are <see cref="Guid"/>s, none of which a text
/// box can hold half-typed.
/// </para>
/// <para>
/// That is also why <see cref="GetInputErrors"/> exists. A value the model cannot represent at all
/// cannot reach <see cref="IMsiPackageValidator"/> — it would arrive as <see langword="null"/> or
/// <see cref="Guid.Empty"/> and be reported as "required", which is the wrong complaint about a typo.
/// The unrepresentable cases are reported here instead, in the validator's own
/// <c>{ Path, Message }</c> shape so both sets display as one list.
/// </para>
/// </remarks>
public sealed partial class PackageEditorViewModel : ObservableObject
{
    /// <summary>The version a new package starts at.</summary>
    public const string DefaultVersion = "1.0.0";

    private readonly IPathPickerService _pathPicker;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _version = DefaultVersion;

    [ObservableProperty]
    private string _productId = string.Empty;

    [ObservableProperty]
    private string _upgradeCode = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private InstallScope _scope = InstallScope.PerMachine;

    [ObservableProperty]
    private CompressionLevel _compression = CompressionLevel.High;

    [ObservableProperty]
    private string _installPath = string.Empty;

    [ObservableProperty]
    private string _releasePath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _msiFilename = string.Empty;

    [ObservableProperty]
    private bool _hasControlPanelInfo;

    [ObservableProperty]
    private string _productIcon = string.Empty;

    [ObservableProperty]
    private string _comments = string.Empty;

    [ObservableProperty]
    private string _contact = string.Empty;

    [ObservableProperty]
    private string _helpLink = string.Empty;

    [ObservableProperty]
    private string _urlInfoAbout = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveShortcutCommand))]
    private ShortcutViewModel? _selectedShortcut;

    /// <summary>Creates the form over the picker its browse buttons drive.</summary>
    /// <param name="pathPicker">Answers the folder and icon browse buttons.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pathPicker"/> is <see langword="null"/>.</exception>
    public PackageEditorViewModel(IPathPickerService pathPicker)
    {
        _pathPicker = pathPicker ?? throw new ArgumentNullException(nameof(pathPicker));

        Shortcuts.CollectionChanged += OnShortcutsChanged;
        Ui.Changed += (_, _) => RaiseChanged();
        PropertyChanged += (_, _) => RaiseChanged();

        Reset();
    }

    /// <summary>
    /// Raised whenever <em>anything</em> the package is made of changes — a field, a shortcut's field, a
    /// shortcut added or removed, the managed-UI block. This is what lets the window gate its Build
    /// command on validity as the user types: <see cref="ObservableObject"/> alone reports neither
    /// collection edits nor changes inside a child ViewModel.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>Every install scope that can be selected.</summary>
    public IReadOnlyList<InstallScope> AvailableScopes { get; } = Enum.GetValues<InstallScope>();

    /// <summary>Every compression level that can be selected.</summary>
    public IReadOnlyList<CompressionLevel> AvailableCompressionLevels { get; } =
        Enum.GetValues<CompressionLevel>();

    /// <summary>The shortcuts the installer will create.</summary>
    public ObservableCollection<ShortcutViewModel> Shortcuts { get; } = [];

    /// <summary>Whether any shortcut is defined — drives the list's empty-state hint.</summary>
    public bool HasShortcuts => Shortcuts.Count > 0;

    /// <summary>The optional managed-UI block.</summary>
    public UiSettingsViewModel Ui { get; } = new();

    /// <summary>
    /// The schema version the edited package carries. Preserved across a load/save round-trip rather
    /// than silently rewritten, so a profile from a future schema is reported by the validator instead
    /// of being downgraded on save.
    /// </summary>
    public int SchemaVersion { get; private set; } = MsiPackage.CurrentSchemaVersion;

    /// <summary>Returns the form to a new, empty package with freshly generated identifiers.</summary>
    public void Reset()
    {
        SchemaVersion = MsiPackage.CurrentSchemaVersion;
        AppName = string.Empty;
        Version = DefaultVersion;
        ProductId = Guid.NewGuid().ToString();
        UpgradeCode = Guid.NewGuid().ToString();
        Manufacturer = string.Empty;
        Scope = InstallScope.PerMachine;
        Compression = CompressionLevel.High;
        InstallPath = string.Empty;
        ReleasePath = string.Empty;
        OutputPath = string.Empty;
        MsiFilename = string.Empty;
        HasControlPanelInfo = false;
        ProductIcon = string.Empty;
        Comments = string.Empty;
        Contact = string.Empty;
        HelpLink = string.Empty;
        UrlInfoAbout = string.Empty;
        SelectedShortcut = null;
        Shortcuts.Clear();
        Ui.LoadFrom(null);
    }

    /// <summary>Shows <paramref name="package"/> in the form, replacing whatever was there.</summary>
    /// <param name="package">The package to edit.</param>
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    public void LoadFrom(MsiPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        SchemaVersion = package.SchemaVersion;
        AppName = package.AppName;
        Version = package.Version?.ToString() ?? string.Empty;
        ProductId = FormatGuid(package.ProductId);
        UpgradeCode = FormatGuid(package.UpgradeCode);
        Manufacturer = package.Manufacturer;
        Scope = package.Scope;
        Compression = package.Compression;
        InstallPath = package.Install.InstallPath;
        ReleasePath = package.Install.ReleasePath;
        OutputPath = package.Output.OutputPath;
        MsiFilename = package.Output.MsiFilename;

        HasControlPanelInfo = package.ControlPanel is not null;
        ProductIcon = package.ControlPanel?.ProductIcon ?? string.Empty;
        Comments = package.ControlPanel?.Comments ?? string.Empty;
        Contact = package.ControlPanel?.Contact ?? string.Empty;
        HelpLink = package.ControlPanel?.HelpLink ?? string.Empty;
        UrlInfoAbout = package.ControlPanel?.UrlInfoAbout ?? string.Empty;

        SelectedShortcut = null;
        Shortcuts.Clear();

        foreach (Shortcut shortcut in package.Shortcuts)
        {
            Shortcuts.Add(ShortcutViewModel.FromModel(shortcut));
        }

        Ui.LoadFrom(package.Ui);
    }

    /// <summary>Materializes the form as a package.</summary>
    /// <returns>
    /// The edited package. Text that the model cannot represent becomes its empty value —
    /// <see langword="null"/> for the version, <see cref="Guid.Empty"/> for an identifier — and is
    /// reported by <see cref="GetInputErrors"/>.
    /// </returns>
    public MsiPackage ToPackage()
    {
        var package = new MsiPackage
        {
            SchemaVersion = SchemaVersion,
            AppName = AppName,
            Version = ParseVersion(Version),
            ProductId = ParseGuid(ProductId),
            UpgradeCode = ParseGuid(UpgradeCode),
            Manufacturer = Manufacturer,
            Scope = Scope,
            Compression = Compression,
            Install = new InstallSettings
            {
                InstallPath = InstallPath,
                ReleasePath = ReleasePath
            },
            Output = new OutputSettings
            {
                OutputPath = OutputPath,
                MsiFilename = MsiFilename
            },
            ControlPanel = HasControlPanelInfo
                ? new ControlPanelInfo
                {
                    ProductIcon = NullIfBlank(ProductIcon),
                    Comments = NullIfBlank(Comments),
                    Contact = NullIfBlank(Contact),
                    HelpLink = NullIfBlank(HelpLink),
                    UrlInfoAbout = NullIfBlank(UrlInfoAbout)
                }
                : null,
            Ui = Ui.ToModel()
        };

        foreach (ShortcutViewModel shortcut in Shortcuts)
        {
            package.Shortcuts.Add(shortcut.ToModel());
        }

        return package;
    }

    /// <summary>
    /// Reports the entered text that no <see cref="MsiPackage"/> can hold — a malformed version or
    /// identifier. Blank is not an input error: that is the validator's "required" rule.
    /// </summary>
    /// <returns>One error per unrepresentable field, in form order; empty when every field parses.</returns>
    public IReadOnlyList<MsiValidationError> GetInputErrors()
    {
        var errors = new List<MsiValidationError>();

        if (!string.IsNullOrWhiteSpace(Version) && ParseVersion(Version) is null)
        {
            errors.Add(new MsiValidationError(
                "version",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' is not a version number (expected e.g. 1.2.3).",
                    Version)));
        }

        AddGuidError(errors, "productId", ProductId);
        AddGuidError(errors, "upgradeCode", UpgradeCode);

        return errors;
    }

    [RelayCommand]
    private void NewProductId() => ProductId = Guid.NewGuid().ToString();

    [RelayCommand]
    private void NewUpgradeCode() => UpgradeCode = Guid.NewGuid().ToString();

    [RelayCommand]
    private void AddShortcut()
    {
        var shortcut = new ShortcutViewModel { ShortcutName = AppName };
        Shortcuts.Add(shortcut);
        SelectedShortcut = shortcut;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedShortcut))]
    private void RemoveShortcut()
    {
        if (SelectedShortcut is not { } shortcut)
        {
            return;
        }

        _ = Shortcuts.Remove(shortcut);
        SelectedShortcut = null;
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

    [RelayCommand]
    private async Task BrowseOutputPathAsync()
    {
        if (await _pathPicker.PickFolderAsync("Select where to write the MSI", OutputPath)
                .ConfigureAwait(true) is { } path)
        {
            OutputPath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseProductIconAsync()
    {
        if (await _pathPicker.PickIconAsync(ReleasePath).ConfigureAwait(true) is { } path)
        {
            ProductIcon = path;
        }
    }

    private bool HasSelectedShortcut => SelectedShortcut is not null;

    // Rows come and go, so their subscriptions have to follow the collection rather than be taken once.
    private void OnShortcutsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (ShortcutViewModel removed in e.OldItems?.OfType<ShortcutViewModel>() ?? [])
        {
            removed.PropertyChanged -= OnShortcutPropertyChanged;
        }

        foreach (ShortcutViewModel added in e.NewItems?.OfType<ShortcutViewModel>() ?? [])
        {
            added.PropertyChanged += OnShortcutPropertyChanged;
        }

        OnPropertyChanged(nameof(HasShortcuts));
        RaiseChanged();
    }

    private void OnShortcutPropertyChanged(object? sender, PropertyChangedEventArgs e) => RaiseChanged();

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private static void AddGuidError(List<MsiValidationError> errors, string path, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !Guid.TryParse(value, out _))
        {
            errors.Add(new MsiValidationError(
                path,
                string.Format(CultureInfo.InvariantCulture, "'{0}' is not a GUID.", value)));
        }
    }

    private static System.Version? ParseVersion(string value)
        => System.Version.TryParse(value, out System.Version? version) ? version : null;

    private static Guid ParseGuid(string value)
        => Guid.TryParse(value, out Guid guid) ? guid : Guid.Empty;

    private static string FormatGuid(Guid value)
        => value == Guid.Empty ? string.Empty : value.ToString();

    private static string? NullIfBlank(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
