namespace Enigma.Msi.Model;

/// <summary>
/// Where the generated <c>.msi</c> is written, and under which name.
/// </summary>
public sealed class OutputSettings
{
    /// <summary>Directory the generated MSI is written to.</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// MSI file name <em>without</em> directory and <em>without</em> the <c>.msi</c> extension — the
    /// extension is appended by the build.
    /// </summary>
    public string MsiFilename { get; set; } = string.Empty;
}
