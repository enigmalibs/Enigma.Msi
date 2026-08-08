using System;
using System.Linq;
using Enigma.Msi.Model;
using Enigma.Msi.Worker.Translation;
using Xunit;
using Wix = WixSharp;

namespace Enigma.Msi.Worker.UnitTests.Translation;

/// <summary>
/// Pins every mirror → WixSharp pairing. The per-member theories catch a wrong pairing (a swap of
/// per-user and per-machine would otherwise ship silently), while the coverage facts catch a mirror
/// member nobody remembered to map.
/// </summary>
public sealed class WixEnumMapTests
{
    [Theory]
    [InlineData(InstallScope.PerMachine, Wix.InstallScope.perMachine)]
    [InlineData(InstallScope.PerUser, Wix.InstallScope.perUser)]
    public void MapScope_MapsTheMirrorMember(InstallScope mirror, Wix.InstallScope expected)
        => Assert.Equal(expected, WixEnumMap.MapScope(mirror));

    [Theory]
    [InlineData(CompressionLevel.None, Wix.CompressionLevel.none)]
    [InlineData(CompressionLevel.Low, Wix.CompressionLevel.low)]
    [InlineData(CompressionLevel.Medium, Wix.CompressionLevel.medium)]
    [InlineData(CompressionLevel.High, Wix.CompressionLevel.high)]
    [InlineData(CompressionLevel.MsZip, Wix.CompressionLevel.mszip)]
    public void MapCompression_MapsTheMirrorMember(CompressionLevel mirror, Wix.CompressionLevel expected)
        => Assert.Equal(expected, WixEnumMap.MapCompression(mirror));

    [Theory]
    [InlineData(Wui.WixUI_Minimal, Wix.WUI.WixUI_Minimal)]
    [InlineData(Wui.WixUI_InstallDir, Wix.WUI.WixUI_InstallDir)]
    [InlineData(Wui.WixUI_FeatureTree, Wix.WUI.WixUI_FeatureTree)]
    [InlineData(Wui.WixUI_Mondo, Wix.WUI.WixUI_Mondo)]
    [InlineData(Wui.WixUI_Advanced, Wix.WUI.WixUI_Advanced)]
    [InlineData(Wui.WixUI_ProgressOnly, Wix.WUI.WixUI_ProgressOnly)]
    [InlineData(Wui.WixUI_Common, Wix.WUI.WixUI_Common)]
    public void MapWui_MapsTheMirrorMember(Wui mirror, Wix.WUI expected)
        => Assert.Equal(expected, WixEnumMap.MapWui(mirror));

    [Fact]
    public void MapDialog_MapsWelcomeToWixSharpsWelcomeDialog()
        => Assert.Same(Wix.Forms.Dialogs.Welcome, WixEnumMap.MapDialog(Dialog.Welcome));

    [Fact]
    public void MapDialog_MapsLicenceToWixSharpsBritishSpelledDialog()
        => Assert.Same(Wix.Forms.Dialogs.Licence, WixEnumMap.MapDialog(Dialog.Licence));

    [Fact]
    public void MapScope_MapsEveryMirrorMemberToADistinctValue()
        => AssertTotalAndDistinct(EnumMembers<InstallScope>(), WixEnumMap.MapScope);

    [Fact]
    public void MapCompression_MapsEveryMirrorMemberToADistinctValue()
        => AssertTotalAndDistinct(EnumMembers<CompressionLevel>(), WixEnumMap.MapCompression);

    [Fact]
    public void MapWui_MapsEveryMirrorMemberToADistinctValue()
        => AssertTotalAndDistinct(EnumMembers<Wui>(), WixEnumMap.MapWui);

    [Fact]
    public void MapDialog_MapsEveryMirrorMemberToADistinctValue()
        => AssertTotalAndDistinct(EnumMembers<Dialog>(), WixEnumMap.MapDialog);

    [Fact]
    public void MapScope_RejectsAValueOutsideTheMirror()
        => Assert.Throws<ArgumentOutOfRangeException>(() => WixEnumMap.MapScope((InstallScope)42));

    [Fact]
    public void MapCompression_RejectsAValueOutsideTheMirror()
        => Assert.Throws<ArgumentOutOfRangeException>(() => WixEnumMap.MapCompression((CompressionLevel)42));

    [Fact]
    public void MapWui_RejectsAValueOutsideTheMirror()
        => Assert.Throws<ArgumentOutOfRangeException>(() => WixEnumMap.MapWui((Wui)42));

    [Fact]
    public void MapDialog_RejectsAValueOutsideTheMirror()
        => Assert.Throws<ArgumentOutOfRangeException>(() => WixEnumMap.MapDialog((Dialog)42));

    /// <summary>
    /// Asserts the map is total over <paramref name="members"/> (it throws on anything it forgot) and
    /// injective (no two mirror members collapse onto the same WixSharp value, which would make one of
    /// them a lie).
    /// </summary>
    private static void AssertTotalAndDistinct<TMirror, TWix>(TMirror[] members, Func<TMirror, TWix> map)
    {
        TWix[] mapped = members.Select(map).ToArray();

        Assert.Equal(members.Length, mapped.Distinct().Count());
    }

    private static TEnum[] EnumMembers<TEnum>()
        where TEnum : struct, Enum
        => (TEnum[])Enum.GetValues(typeof(TEnum));
}
