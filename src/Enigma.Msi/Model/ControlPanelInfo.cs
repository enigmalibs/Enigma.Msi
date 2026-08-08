namespace Enigma.Msi.Model;

/// <summary>
/// Optional product information surfaced in Windows' "Apps &amp; features" / Control Panel
/// "Programs and Features" entry. Every member is optional; unset members are left at the MSI
/// default.
/// </summary>
public sealed class ControlPanelInfo
{
    /// <summary>Path to the icon file shown next to the product entry.</summary>
    public string? ProductIcon { get; set; }

    /// <summary>Free-text comments shown for the product.</summary>
    public string? Comments { get; set; }

    /// <summary>Support contact shown for the product.</summary>
    public string? Contact { get; set; }

    /// <summary>URL of the product's support/help page.</summary>
    public string? HelpLink { get; set; }

    /// <summary>URL of the product's "about" page.</summary>
    public string? UrlInfoAbout { get; set; }
}
