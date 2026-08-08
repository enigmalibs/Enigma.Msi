namespace Enigma.Msi.Build;

/// <summary>
/// Settings for <see cref="MsiBuildService"/>. Every member has a working default, so the common case
/// is <c>new MsiBuildService()</c>; the options exist for hosts whose layout differs from the one the
/// <c>Enigma.Msi</c> package produces.
/// </summary>
public sealed class MsiBuildServiceOptions
{
    /// <summary>The worker executable's file name, as shipped.</summary>
    public const string WorkerFileName = "Enigma.Msi.Worker.exe";

    /// <summary>
    /// Name of the folder next to the host application that holds the worker — the layout
    /// <c>build/Enigma.Msi.targets</c> produces for package consumers.
    /// </summary>
    public const string WorkerFolderName = "worker";

    /// <summary>The WiX CLI probed by <see cref="IMsiBuildService.CheckPrerequisitesAsync"/> by default.</summary>
    public const string DefaultWixToolPath = "wix";

    /// <summary>
    /// Full path of the worker executable. When <see langword="null"/> or blank (the default), the
    /// worker is discovered at
    /// <c>AppContext.BaseDirectory/<see cref="WorkerFolderName"/>/<see cref="WorkerFileName"/></c>.
    /// Set it only when the worker does not ship beside the host application.
    /// </summary>
    public string? WorkerPath { get; set; }

    /// <summary>
    /// The WiX CLI to probe during the pre-flight check. Defaults to
    /// <see cref="DefaultWixToolPath"/>, which resolves through <c>PATH</c> — where
    /// <c>dotnet tool install --global wix</c> puts it. Set it to a full path when the tool is
    /// installed somewhere <c>PATH</c> does not cover.
    /// </summary>
    public string WixToolPath { get; set; } = DefaultWixToolPath;
}
