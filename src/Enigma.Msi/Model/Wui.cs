namespace Enigma.Msi.Model;

/// <summary>
/// Built-in WixUI dialog set hosting the managed UI. WixSharp-free mirror of WixSharp's <c>WUI</c>;
/// the net472 worker maps it to the real WixSharp value.
/// </summary>
/// <remarks>
/// The member names match WixSharp's <c>WUI</c> members <em>verbatim</em> (underscores included) so
/// the worker's drift guards can compare the two sets by name. This is why they deviate from the
/// house PascalCase naming.
/// </remarks>
public enum Wui
{
    /// <summary>Single-dialog UI with no options.</summary>
    WixUI_Minimal,

    /// <summary>Lets the user choose the installation directory. The library's default.</summary>
    WixUI_InstallDir,

    /// <summary>Feature-selection tree.</summary>
    WixUI_FeatureTree,

    /// <summary>Typical/Custom/Complete setup types plus a feature tree.</summary>
    WixUI_Mondo,

    /// <summary>Per-user vs per-machine choice plus directory selection.</summary>
    WixUI_Advanced,

    /// <summary>Progress dialog only — no user interaction.</summary>
    WixUI_ProgressOnly,

    /// <summary>The shared dialog set the other WixUI sets are built from.</summary>
    WixUI_Common
}
