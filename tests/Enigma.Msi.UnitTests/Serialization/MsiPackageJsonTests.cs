using System;
using System.Collections.Generic;
using System.Text.Json;
using Enigma.Msi.Build;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;
using Xunit;

namespace Enigma.Msi.UnitTests.Serialization;

/// <summary>
/// Covers the <c>.msipkg.json</c> contract: round-trip fidelity, the wire shape (camelCase, enums as
/// names, nulls omitted), forward compatibility, and the worker's result document.
/// </summary>
public sealed class MsiPackageJsonTests
{
    [Fact]
    public void RoundTrip_FullPackage_PreservesEveryField()
    {
        MsiPackage original = TestPackages.CreateFull();

        string json = MsiPackageJson.Serialize(original);
        MsiPackage restored = MsiPackageJson.Deserialize(json);

        // Re-serializing must produce byte-identical JSON: any member lost on the way back would
        // change the document, whatever it is — including members added to the model later.
        Assert.Equal(json, MsiPackageJson.Serialize(restored));

        // Explicit assertions on the interesting conversions, so a failure says which one broke.
        Assert.Equal(MsiPackage.CurrentSchemaVersion, restored.SchemaVersion);
        Assert.Equal(original.AppName, restored.AppName);
        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.ProductId, restored.ProductId);
        Assert.Equal(original.UpgradeCode, restored.UpgradeCode);
        Assert.Equal(original.Manufacturer, restored.Manufacturer);
        Assert.Equal(InstallScope.PerUser, restored.Scope);
        Assert.Equal(CompressionLevel.MsZip, restored.Compression);
        Assert.Equal(original.Install.InstallPath, restored.Install.InstallPath);
        Assert.Equal(original.Install.ReleasePath, restored.Install.ReleasePath);
        Assert.Equal(original.Output.OutputPath, restored.Output.OutputPath);
        Assert.Equal(original.Output.MsiFilename, restored.Output.MsiFilename);
        Assert.NotNull(restored.ControlPanel);
        Assert.Equal(original.ControlPanel!.ProductIcon, restored.ControlPanel.ProductIcon);
        Assert.Equal(original.ControlPanel.Comments, restored.ControlPanel.Comments);
        Assert.Equal(original.ControlPanel.Contact, restored.ControlPanel.Contact);
        Assert.Equal(original.ControlPanel.HelpLink, restored.ControlPanel.HelpLink);
        Assert.Equal(original.ControlPanel.UrlInfoAbout, restored.ControlPanel.UrlInfoAbout);
        Assert.Equal(2, restored.Shortcuts.Count);
        Assert.Equal(original.Shortcuts[1].ShortcutPath, restored.Shortcuts[1].ShortcutPath);
        Assert.Equal(original.Shortcuts[1].ShortcutName, restored.Shortcuts[1].ShortcutName);
        Assert.Equal(original.Shortcuts[1].TargetPath, restored.Shortcuts[1].TargetPath);
        Assert.Equal(original.Shortcuts[1].IconPath, restored.Shortcuts[1].IconPath);
        Assert.Equal(original.Shortcuts[1].Arguments, restored.Shortcuts[1].Arguments);
        Assert.NotNull(restored.Ui);
        Assert.Equal(Wui.WixUI_Mondo, restored.Ui.Wui);
        Assert.Equal(original.Ui!.InstallDialogs, restored.Ui.InstallDialogs);
        Assert.Equal(original.Ui.ModifyDialogs, restored.Ui.ModifyDialogs);
    }

    [Fact]
    public void RoundTrip_MinimalPackage_PreservesFieldsAndDefaults()
    {
        MsiPackage original = TestPackages.CreateMinimalValid();

        string json = MsiPackageJson.Serialize(original);
        MsiPackage restored = MsiPackageJson.Deserialize(json);

        Assert.Equal(json, MsiPackageJson.Serialize(restored));
        Assert.Equal(InstallScope.PerMachine, restored.Scope);
        Assert.Equal(CompressionLevel.High, restored.Compression);
        Assert.Empty(restored.Shortcuts);
        Assert.Null(restored.ControlPanel);
        Assert.Null(restored.Ui);
    }

    [Fact]
    public void Serialize_OmitsNullOptionalMembers()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Shortcuts.Add(new Shortcut
        {
            ShortcutPath = "%Desktop%",
            ShortcutName = "Widget",
            TargetPath = @"[INSTALLDIR]\Widget.exe"
        });

        string json = MsiPackageJson.Serialize(package);

        Assert.DoesNotContain("controlPanel", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ui\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("iconPath", json, StringComparison.Ordinal);
        Assert.DoesNotContain("arguments", json, StringComparison.Ordinal);
        Assert.Contains("shortcutName", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_UsesCamelCaseNamesAndAlwaysWritesSchemaVersion()
    {
        string json = MsiPackageJson.Serialize(TestPackages.CreateFull());

        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"appName\": \"Contoso Widget\"", json, StringComparison.Ordinal);
        Assert.Contains("\"installPath\"", json, StringComparison.Ordinal);
        Assert.Contains("\"msiFilename\"", json, StringComparison.Ordinal);
        Assert.Contains("\"urlInfoAbout\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_WritesEnumsAsTheirMemberNames()
    {
        string json = MsiPackageJson.Serialize(TestPackages.CreateFull());

        Assert.Contains("\"scope\": \"PerUser\"", json, StringComparison.Ordinal);
        Assert.Contains("\"compression\": \"MsZip\"", json, StringComparison.Ordinal);
        Assert.Contains("\"wui\": \"WixUI_Mondo\"", json, StringComparison.Ordinal);
        Assert.Contains("\"MaintenanceType\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_WritesVersionAndGuidsAsStrings()
    {
        string json = MsiPackageJson.Serialize(TestPackages.CreateFull());

        Assert.Contains("\"version\": \"1.2.3.4\"", json, StringComparison.Ordinal);
        Assert.Contains("\"productId\": \"9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f\"", json, StringComparison.Ordinal);
        Assert.Contains("\"upgradeCode\": \"2f1e0d9c-8b7a-6f5e-4b3c-2d1e0f9a8b7c\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_IgnoresUnknownMembers()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "appName": "Widget",
              "version": "1.0.0",
              "somethingAddedLater": { "nested": [1, 2, 3] },
              "install": { "installPath": "%ProgramFiles%\\Widget", "releasePath": "C:\\release", "futureFlag": true }
            }
            """;

        MsiPackage package = MsiPackageJson.Deserialize(json);

        Assert.Equal("Widget", package.AppName);
        Assert.Equal(new Version(1, 0, 0), package.Version);
        Assert.Equal(@"C:\release", package.Install.ReleasePath);
    }

    [Fact]
    public void Deserialize_EmptyObject_LeavesModelDefaults()
    {
        MsiPackage package = MsiPackageJson.Deserialize("{}");

        Assert.Equal(MsiPackage.CurrentSchemaVersion, package.SchemaVersion);
        Assert.Equal(InstallScope.PerMachine, package.Scope);
        Assert.Equal(CompressionLevel.High, package.Compression);
        Assert.NotNull(package.Install);
        Assert.NotNull(package.Output);
        Assert.Empty(package.Shortcuts);
        Assert.Null(package.Version);
    }

    [Fact]
    public void Deserialize_IsCaseInsensitiveOnMemberNames()
    {
        MsiPackage package = MsiPackageJson.Deserialize("""{ "AppName": "Widget", "COMPRESSION": "Low" }""");

        Assert.Equal("Widget", package.AppName);
        Assert.Equal(CompressionLevel.Low, package.Compression);
    }

    [Fact]
    public void Deserialize_JsonNull_Throws()
        => Assert.Throws<JsonException>(() => MsiPackageJson.Deserialize("null"));

    [Fact]
    public void Deserialize_Malformed_Throws()
        => Assert.Throws<JsonException>(() => MsiPackageJson.Deserialize("{ \"appName\": "));

    [Fact]
    public void RoundTrip_SuccessfulBuildResult_PreservesFields()
    {
        MsiBuildResult original = MsiBuildResult.Succeeded(@"C:\artifacts\Widget.msi");

        string json = MsiPackageJson.SerializeResult(original);
        MsiBuildResult restored = MsiPackageJson.DeserializeResult(json);

        Assert.True(restored.Success);
        Assert.Equal(@"C:\artifacts\Widget.msi", restored.MsiPath);
        Assert.Empty(restored.Errors);
        Assert.Contains("\"success\": true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void RoundTrip_FailedBuildResult_PreservesEveryError()
    {
        MsiBuildResult original = MsiBuildResult.Failed(new List<string>
        {
            "install.releasePath: Directory does not exist.",
            "output.msiFilename: Required."
        });

        MsiBuildResult restored = MsiPackageJson.DeserializeResult(MsiPackageJson.SerializeResult(original));

        Assert.False(restored.Success);
        Assert.Null(restored.MsiPath);
        Assert.Equal(original.Errors, restored.Errors);
    }

    [Fact]
    public void DeserializeResult_JsonNull_Throws()
        => Assert.Throws<JsonException>(() => MsiPackageJson.DeserializeResult("null"));
}
