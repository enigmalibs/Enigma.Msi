using System;
using System.Collections.Generic;

namespace Enigma.Msi.Model;

/// <summary>
/// Declarative description of an MSI package — the single input the whole library is built around:
/// the build client sends it to the worker, the worker translates it to WixSharp, and a saved
/// <c>.msipkg.json</c> profile is exactly its serialization. There is deliberately no mirrored DTO
/// layer.
/// </summary>
/// <remarks>
/// The type is a mutable data carrier with permissive defaults rather than a set of
/// <c>required</c> members, precisely so a half-filled package can be deserialized, edited and
/// reported on. Required-ness is a <em>validation</em> concern: run
/// <see cref="Validation.IMsiPackageValidator"/> to obtain every violation with its member path.
/// </remarks>
public sealed class MsiPackage
{
    /// <summary>
    /// The <c>.msipkg.json</c> schema version this library reads and writes. Bumped only on a
    /// breaking change to the on-disk format.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Version of the profile schema this package was written with. Always serialized; a value above
    /// <see cref="CurrentSchemaVersion"/> is reported as a validation error rather than silently
    /// misread.
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Product name, as shown by the installer and in Control Panel. Required.</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Product version. Required. Serialized as a string (e.g. <c>1.2.3</c>); only the first three
    /// components are significant to Windows Installer.
    /// </summary>
    public System.Version? Version { get; set; }

    /// <summary>
    /// GUID identifying this specific product version. Required (must not be
    /// <see cref="Guid.Empty"/>).
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// GUID identifying the product line. Required (must not be <see cref="Guid.Empty"/>) and must
    /// stay constant across versions, otherwise Windows Installer cannot detect upgrades.
    /// </summary>
    public Guid UpgradeCode { get; set; }

    /// <summary>Publisher shown in Control Panel. Required.</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>Per-machine or per-user installation. Defaults to per-machine.</summary>
    public InstallScope Scope { get; set; } = InstallScope.PerMachine;

    /// <summary>Install target directory and the source folder to package. Required.</summary>
    public InstallSettings Install { get; set; } = new();

    /// <summary>Where the generated MSI is written, and under which name. Required.</summary>
    public OutputSettings Output { get; set; } = new();

    /// <summary>Cabinet compression level. Defaults to <see cref="CompressionLevel.High"/>.</summary>
    public CompressionLevel Compression { get; set; } = CompressionLevel.High;

    /// <summary>
    /// Optional Control Panel information (icon, comments, contact, links). When
    /// <see langword="null"/>, MSI defaults apply.
    /// </summary>
    public ControlPanelInfo? ControlPanel { get; set; }

    /// <summary>Shortcuts the installer creates. May be empty.</summary>
    public List<Shortcut> Shortcuts { get; set; } = [];

    /// <summary>
    /// Optional managed-UI configuration. When <see langword="null"/>, the build applies
    /// <see cref="UiSettings.CreateDefault"/>.
    /// </summary>
    public UiSettings? Ui { get; set; }
}
