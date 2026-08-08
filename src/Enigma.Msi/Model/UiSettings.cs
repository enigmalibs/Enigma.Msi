using System.Collections.Generic;

namespace Enigma.Msi.Model;

/// <summary>
/// Managed-UI configuration: which built-in WixUI dialog set to host, and which dialogs to show
/// during a fresh install and during modify/repair/remove.
/// </summary>
/// <remarks>
/// Optional as a whole: when <see cref="MsiPackage.Ui"/> is <see langword="null"/> the build applies
/// <see cref="CreateDefault"/>.
/// </remarks>
public sealed class UiSettings
{
    /// <summary>The built-in WixUI dialog set hosting the managed dialogs.</summary>
    // Qualified: the property name shadows the type name inside this class.
    public Wui Wui { get; set; } = Model.Wui.WixUI_InstallDir;

    /// <summary>Ordered dialogs shown during a fresh install. Must not be empty.</summary>
    public List<Dialog> InstallDialogs { get; set; } = [];

    /// <summary>
    /// Ordered dialogs shown when modifying, repairing or removing an installed product. Must not be
    /// empty.
    /// </summary>
    public List<Dialog> ModifyDialogs { get; set; } = [];

    /// <summary>
    /// Creates the default managed UI — <see cref="Model.Wui.WixUI_InstallDir"/> with a
    /// Welcome/InstallDir/Progress/Exit install sequence and a
    /// Welcome/MaintenanceType/Progress/Exit modify sequence. This is what the build applies when
    /// <see cref="MsiPackage.Ui"/> is <see langword="null"/>; it is exposed so a UI can pre-fill the
    /// same values when the user starts customizing.
    /// </summary>
    /// <returns>A new <see cref="UiSettings"/> instance carrying the default dialog sequences.</returns>
    public static UiSettings CreateDefault() => new()
    {
        Wui = Model.Wui.WixUI_InstallDir,
        InstallDialogs = [Dialog.Welcome, Dialog.InstallDir, Dialog.Progress, Dialog.Exit],
        ModifyDialogs = [Dialog.Welcome, Dialog.MaintenanceType, Dialog.Progress, Dialog.Exit]
    };
}
