using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Msi.Model;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// Editor for the optional managed-UI block. <see cref="IsCustomized"/> is what
/// <see cref="MsiPackage.Ui"/> being non-<see langword="null"/> looks like in the UI: switched off,
/// the package carries no <c>ui</c> section and the build applies
/// <see cref="UiSettings.CreateDefault"/> itself.
/// </summary>
public sealed partial class UiSettingsViewModel : ObservableObject
{
    /// <summary>
    /// Whether the package pins its own managed UI. When <see langword="false"/>,
    /// <see cref="ToModel"/> returns <see langword="null"/> and the sequences below are shown only as
    /// what the build would apply anyway.
    /// </summary>
    [ObservableProperty]
    private bool _isCustomized;

    // Qualified: the generated property name shadows the type name inside this class, exactly as it
    // does in the model's own UiSettings.
    [ObservableProperty]
    private Wui _wui = Enigma.Msi.Model.Wui.WixUI_InstallDir;

    /// <summary>Every WixUI dialog set that can be selected.</summary>
    public IReadOnlyList<Wui> AvailableWuis { get; } = Enum.GetValues<Enigma.Msi.Model.Wui>();

    /// <summary>The dialogs shown during a fresh install, in order.</summary>
    public DialogSequenceViewModel InstallDialogs { get; } = new();

    /// <summary>The dialogs shown when modifying, repairing or removing, in order.</summary>
    public DialogSequenceViewModel ModifyDialogs { get; } = new();

    /// <summary>
    /// Creates an editor pre-filled with the sequences the build applies by default, switched off —
    /// so switching it on starts from something valid rather than from two empty lists.
    /// </summary>
    public UiSettingsViewModel()
    {
        LoadFrom(null);

        PropertyChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        InstallDialogs.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        ModifyDialogs.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised whenever anything in this block — including either sequence — changes.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Shows <paramref name="settings"/>, or the build's own defaults (switched off) when it is
    /// <see langword="null"/>.
    /// </summary>
    /// <param name="settings">The managed-UI block to show, or <see langword="null"/> for none.</param>
    public void LoadFrom(UiSettings? settings)
    {
        UiSettings source = settings ?? UiSettings.CreateDefault();

        IsCustomized = settings is not null;
        Wui = source.Wui;
        InstallDialogs.Load(source.InstallDialogs);
        ModifyDialogs.Load(source.ModifyDialogs);
    }

    /// <summary>Materializes the edited block.</summary>
    /// <returns>
    /// The managed-UI settings, or <see langword="null"/> when <see cref="IsCustomized"/> is
    /// <see langword="false"/> — which is how the package says "apply the default UI".
    /// </returns>
    public UiSettings? ToModel()
        => IsCustomized
            ? new UiSettings
            {
                Wui = Wui,
                InstallDialogs = InstallDialogs.ToModel(),
                ModifyDialogs = ModifyDialogs.ToModel()
            }
            : null;
}
