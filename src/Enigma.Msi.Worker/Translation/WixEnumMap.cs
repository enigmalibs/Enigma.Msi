using System;
using Enigma.Msi.Model;
using Wix = WixSharp;

namespace Enigma.Msi.Worker.Translation;

/// <summary>
/// Maps the library's WixSharp-free enum mirrors onto the real WixSharp values. This one-way map is
/// what lets <c>Enigma.Msi</c> expose an installer model without exposing WixSharp: the mirrors are
/// the public contract, and the translation happens here, inside the net472 process.
/// </summary>
/// <remarks>
/// <para>
/// Every method is a pure function so the net472 suite can cover all of them without a WiX toolchain,
/// and every one <em>throws</em> on an input it does not know rather than falling back to a default:
/// a member added to a mirror but forgotten here must fail loudly, not quietly install per-user.
/// </para>
/// <para>
/// The WixSharp member sets these maps target are pinned by the drift guards in
/// <c>Enigma.Msi.Worker.UnitTests</c>, so a WixSharp upgrade that renames or adds a member breaks the
/// test suite instead of silently mis-mapping.
/// </para>
/// </remarks>
public static class WixEnumMap
{
    /// <summary>Maps the mirror installation scope onto WixSharp's <c>InstallScope</c>.</summary>
    /// <param name="scope">The mirror value.</param>
    /// <returns>The equivalent WixSharp value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scope"/> is not a known scope.</exception>
    public static Wix.InstallScope MapScope(InstallScope scope) => scope switch
    {
        InstallScope.PerMachine => Wix.InstallScope.perMachine,
        InstallScope.PerUser => Wix.InstallScope.perUser,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Not a known installation scope.")
    };

    /// <summary>Maps the mirror compression level onto WixSharp's <c>CompressionLevel</c>.</summary>
    /// <param name="level">The mirror value.</param>
    /// <returns>The equivalent WixSharp value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is not a known level.</exception>
    public static Wix.CompressionLevel MapCompression(CompressionLevel level) => level switch
    {
        CompressionLevel.None => Wix.CompressionLevel.none,
        CompressionLevel.Low => Wix.CompressionLevel.low,
        CompressionLevel.Medium => Wix.CompressionLevel.medium,
        CompressionLevel.High => Wix.CompressionLevel.high,
        CompressionLevel.MsZip => Wix.CompressionLevel.mszip,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Not a known compression level.")
    };

    /// <summary>Maps the mirror WixUI dialog set onto WixSharp's <c>WUI</c>.</summary>
    /// <param name="wui">The mirror value.</param>
    /// <returns>The equivalent WixSharp value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="wui"/> is not a known dialog set.</exception>
    public static Wix.WUI MapWui(Wui wui) => wui switch
    {
        Wui.WixUI_Minimal => Wix.WUI.WixUI_Minimal,
        Wui.WixUI_InstallDir => Wix.WUI.WixUI_InstallDir,
        Wui.WixUI_FeatureTree => Wix.WUI.WixUI_FeatureTree,
        Wui.WixUI_Mondo => Wix.WUI.WixUI_Mondo,
        Wui.WixUI_Advanced => Wix.WUI.WixUI_Advanced,
        Wui.WixUI_ProgressOnly => Wix.WUI.WixUI_ProgressOnly,
        Wui.WixUI_Common => Wix.WUI.WixUI_Common,
        _ => throw new ArgumentOutOfRangeException(nameof(wui), wui, "Not a known WixUI dialog set.")
    };

    /// <summary>
    /// Maps the mirror dialog onto the managed-dialog <see cref="Type"/> WixSharp's <c>Dialogs</c>
    /// helper exposes — WixSharp identifies managed dialogs by their Windows Forms type, not by an
    /// enum, which is why this map alone returns a <see cref="Type"/>.
    /// </summary>
    /// <param name="dialog">The mirror value.</param>
    /// <returns>The WixSharp dialog type.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dialog"/> is not a known dialog.</exception>
    public static Type MapDialog(Dialog dialog) => dialog switch
    {
        Dialog.Welcome => Wix.Forms.Dialogs.Welcome,
        Dialog.Licence => Wix.Forms.Dialogs.Licence,
        Dialog.Features => Wix.Forms.Dialogs.Features,
        Dialog.InstallDir => Wix.Forms.Dialogs.InstallDir,
        Dialog.SetupType => Wix.Forms.Dialogs.SetupType,
        Dialog.Progress => Wix.Forms.Dialogs.Progress,
        Dialog.MaintenanceType => Wix.Forms.Dialogs.MaintenanceType,
        Dialog.Exit => Wix.Forms.Dialogs.Exit,
        _ => throw new ArgumentOutOfRangeException(nameof(dialog), dialog, "Not a known dialog.")
    };
}
