namespace Enigma.Msi.Model;

/// <summary>
/// Cabinet compression level applied to the packaged files. WixSharp-free mirror of WixSharp's
/// <c>CompressionLevel</c> (whose members are camel-cased: <c>none</c>, <c>low</c>, …); the net472
/// worker maps it to the real WixSharp value.
/// </summary>
public enum CompressionLevel
{
    /// <summary>Store the files uncompressed — largest MSI, fastest build.</summary>
    None,

    /// <summary>Low compression.</summary>
    Low,

    /// <summary>Medium compression.</summary>
    Medium,

    /// <summary>Maximum compression — smallest MSI, slowest build. The library's default.</summary>
    High,

    /// <summary>MSZIP compression.</summary>
    MsZip
}
