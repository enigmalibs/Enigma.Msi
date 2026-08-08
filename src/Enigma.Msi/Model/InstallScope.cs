namespace Enigma.Msi.Model;

/// <summary>
/// Installation scope of the package. WixSharp-free mirror of WixSharp's <c>InstallScope</c>; the
/// net472 worker maps it to the real WixSharp value.
/// </summary>
/// <remarks>
/// Deliberately a <em>subset</em> of WixSharp's member set: WixSharp also offers
/// <c>perUserOrMachine</c> (a dual-purpose package whose scope is chosen at install time), which is
/// out of scope at v1 because it needs its own install-scope dialog and ALLUSERS handling. The
/// worker's drift guards assert WixSharp's full member set, so a WixSharp upgrade that changes it
/// fails loudly rather than mis-mapping.
/// </remarks>
public enum InstallScope
{
    /// <summary>Install for all users of the machine (requires elevation).</summary>
    PerMachine,

    /// <summary>Install for the current user only (no elevation).</summary>
    PerUser
}
