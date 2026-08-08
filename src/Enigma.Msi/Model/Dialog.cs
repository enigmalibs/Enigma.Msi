namespace Enigma.Msi.Model;

/// <summary>
/// A standard managed-UI dialog. WixSharp-free mirror of the dialogs exposed by WixSharp's
/// <c>WixSharp.Forms.Dialogs</c> helper; the net472 worker maps it to the real WixSharp dialog type.
/// </summary>
/// <remarks>
/// <para>
/// The member names match WixSharp's <c>Dialogs</c> members <em>verbatim</em> so the worker's drift
/// guards can compare the two sets by name — including the British spelling <see cref="Licence"/>.
/// </para>
/// <para>
/// Deliberately a <em>subset</em>: WixSharp also exposes an <c>InstallScope</c> dialog, which only
/// makes sense for a dual-purpose package (per-user <em>or</em> per-machine chosen at install time)
/// and therefore has nothing to switch given <see cref="Model.InstallScope"/>'s v1 member set.
/// </para>
/// </remarks>
public enum Dialog
{
    /// <summary>Welcome / first dialog of the sequence.</summary>
    Welcome,

    /// <summary>Licence agreement (British spelling, matching WixSharp).</summary>
    Licence,

    /// <summary>Feature selection.</summary>
    Features,

    /// <summary>Installation directory selection.</summary>
    InstallDir,

    /// <summary>Setup type selection (typical / custom / complete).</summary>
    SetupType,

    /// <summary>Installation progress.</summary>
    Progress,

    /// <summary>Maintenance type selection (change / repair / remove).</summary>
    MaintenanceType,

    /// <summary>Final / exit dialog.</summary>
    Exit
}
