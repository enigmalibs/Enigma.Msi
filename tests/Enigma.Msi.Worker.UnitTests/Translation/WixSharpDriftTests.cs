using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enigma.Msi.Model;
using Enigma.Msi.Worker.Translation;
using Xunit;
using Wix = WixSharp;

namespace Enigma.Msi.Worker.UnitTests.Translation;

/// <summary>
/// Drift guards against the pinned WixSharp version. The library's enums are hand-written mirrors of
/// WixSharp's, so a WixSharp upgrade that adds, removes or renames a member silently invalidates the
/// mirror — the predecessor project pinned its mirrors to WixSharp 2.12 by hand and had no way to
/// notice. These tests turn that into a build failure.
/// </summary>
/// <remarks>
/// When one of these fails after a WixSharp bump, the fix is never to edit the expected set alone:
/// decide what the new member means for the model (mirror it, or document the omission), update
/// <see cref="WixEnumMap"/>, and only then re-pin the set here.
/// </remarks>
public sealed class WixSharpDriftTests
{
    /// <summary>WixSharp's <c>InstallScope</c> members, as of WixSharp_wix4 2.14.1.</summary>
    private static readonly string[] WixSharpInstallScopes = ["perMachine", "perUser", "perUserOrMachine"];

    /// <summary>WixSharp's <c>CompressionLevel</c> members, as of WixSharp_wix4 2.14.1.</summary>
    private static readonly string[] WixSharpCompressionLevels = ["none", "low", "medium", "high", "mszip"];

    /// <summary>WixSharp's <c>WUI</c> members, as of WixSharp_wix4 2.14.1.</summary>
    private static readonly string[] WixSharpWuis =
    [
        "WixUI_Minimal",
        "WixUI_InstallDir",
        "WixUI_FeatureTree",
        "WixUI_Mondo",
        "WixUI_Advanced",
        "WixUI_ProgressOnly",
        "WixUI_Common"
    ];

    /// <summary>
    /// WixSharp's <c>Dialogs</c> members, as of WixSharp_wix4 2.14.1 — note the British
    /// <c>Licence</c> spelling, which the mirror reproduces verbatim.
    /// </summary>
    private static readonly string[] WixSharpDialogs =
    [
        "Welcome",
        "Licence",
        "Features",
        "InstallDir",
        "InstallScope",
        "Progress",
        "SetupType",
        "MaintenanceType",
        "Exit"
    ];

    /// <summary>
    /// The WixSharp scope the model deliberately does not mirror: a dual-purpose package needs its own
    /// install-scope dialog and ALLUSERS handling, which is out of scope at v1.
    /// </summary>
    private static readonly string[] DeliberatelyUnmirroredScopes = ["perUserOrMachine"];

    /// <summary>
    /// The WixSharp dialog the model deliberately does not mirror: with a per-machine/per-user model
    /// enum rather than a dual-purpose package, the install-scope dialog has nothing to switch.
    /// </summary>
    private static readonly string[] DeliberatelyUnmirroredDialogs = ["InstallScope"];

    [Fact]
    public void WixSharpInstallScope_StillHasThePinnedMembers()
        => AssertSameSet(WixSharpInstallScopes, Enum.GetNames(typeof(Wix.InstallScope)));

    [Fact]
    public void WixSharpCompressionLevel_StillHasThePinnedMembers()
        => AssertSameSet(WixSharpCompressionLevels, Enum.GetNames(typeof(Wix.CompressionLevel)));

    [Fact]
    public void WixSharpWui_StillHasThePinnedMembers()
        => AssertSameSet(WixSharpWuis, Enum.GetNames(typeof(Wix.WUI)));

    [Fact]
    public void WixSharpDialogs_StillHasThePinnedMembers()
        => AssertSameSet(WixSharpDialogs, WixSharpDialogNames());

    [Fact]
    public void InstallScopeMirror_NamesAWixSharpMember()
        => AssertMirrorNamesExist<InstallScope>(WixSharpInstallScopes, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void CompressionLevelMirror_NamesAWixSharpMember()
        => AssertMirrorNamesExist<CompressionLevel>(WixSharpCompressionLevels, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void WuiMirror_NamesAWixSharpMemberVerbatim()
        => AssertMirrorNamesExist<Wui>(WixSharpWuis, StringComparer.Ordinal);

    [Fact]
    public void DialogMirror_NamesAWixSharpMemberVerbatim()
        => AssertMirrorNamesExist<Dialog>(WixSharpDialogs, StringComparer.Ordinal);

    [Fact]
    public void InstallScopeMirror_OmitsOnlyTheDocumentedMembers()
        => AssertUnmirrored<InstallScope>(WixSharpInstallScopes, DeliberatelyUnmirroredScopes, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void CompressionLevelMirror_OmitsNothing()
        => AssertUnmirrored<CompressionLevel>(WixSharpCompressionLevels, [], StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void WuiMirror_OmitsNothing()
        => AssertUnmirrored<Wui>(WixSharpWuis, [], StringComparer.Ordinal);

    [Fact]
    public void DialogMirror_OmitsOnlyTheDocumentedDialogs()
        => AssertUnmirrored<Dialog>(WixSharpDialogs, DeliberatelyUnmirroredDialogs, StringComparer.Ordinal);

    [Fact]
    public void MapScope_ResolvesToTheSameNamedWixSharpMember()
    {
        foreach (InstallScope mirror in EnumMembers<InstallScope>())
        {
            Assert.Equal(mirror.ToString(), WixEnumMap.MapScope(mirror).ToString(), ignoreCase: true);
        }
    }

    [Fact]
    public void MapCompression_ResolvesToTheSameNamedWixSharpMember()
    {
        foreach (CompressionLevel mirror in EnumMembers<CompressionLevel>())
        {
            Assert.Equal(mirror.ToString(), WixEnumMap.MapCompression(mirror).ToString(), ignoreCase: true);
        }
    }

    [Fact]
    public void MapWui_ResolvesToTheSameNamedWixSharpMember()
    {
        foreach (Wui mirror in EnumMembers<Wui>())
        {
            Assert.Equal(mirror.ToString(), WixEnumMap.MapWui(mirror).ToString());
        }
    }

    [Fact]
    public void MapDialog_ResolvesToTheSameNamedWixSharpDialogsField()
    {
        foreach (Dialog mirror in EnumMembers<Dialog>())
        {
            FieldInfo? field = typeof(Wix.Forms.Dialogs).GetField(
                mirror.ToString(),
                BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(field);
            Assert.Same(field.GetValue(null), WixEnumMap.MapDialog(mirror));
        }
    }

    /// <summary>
    /// The dialogs WixSharp exposes, identified the way WixSharp identifies them: public static fields
    /// of type <see cref="Type"/> on the <c>Dialogs</c> helper.
    /// </summary>
    private static string[] WixSharpDialogNames()
        => typeof(Wix.Forms.Dialogs)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Type))
            .Select(field => field.Name)
            .ToArray();

    private static void AssertSameSet(IEnumerable<string> expected, IEnumerable<string> actual)
        => Assert.Equal(
            expected.OrderBy(name => name, StringComparer.Ordinal),
            actual.OrderBy(name => name, StringComparer.Ordinal));

    /// <summary>Every mirror member must name a member WixSharp actually has.</summary>
    private static void AssertMirrorNamesExist<TMirror>(string[] wixSharpNames, StringComparer comparer)
        where TMirror : struct, Enum
    {
        string[] missing = Enum.GetNames(typeof(TMirror))
            .Where(name => !wixSharpNames.Contains(name, comparer))
            .ToArray();

        Assert.Empty(missing);
    }

    /// <summary>
    /// Every WixSharp member the mirror does not carry must be one we decided to leave out on purpose —
    /// so a member a WixSharp upgrade adds shows up here instead of quietly becoming unreachable.
    /// </summary>
    private static void AssertUnmirrored<TMirror>(
        string[] wixSharpNames,
        string[] documentedOmissions,
        StringComparer comparer)
        where TMirror : struct, Enum
    {
        string[] mirrored = Enum.GetNames(typeof(TMirror));

        string[] unmirrored = wixSharpNames
            .Where(name => !mirrored.Contains(name, comparer))
            .ToArray();

        AssertSameSet(documentedOmissions, unmirrored);
    }

    private static TEnum[] EnumMembers<TEnum>()
        where TEnum : struct, Enum
        => (TEnum[])Enum.GetValues(typeof(TEnum));
}
