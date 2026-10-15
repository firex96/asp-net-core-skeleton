using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.UnitTests.Enums;

public class EnumCodesTests
{
    [Fact]
    public void ToCode_UsesMemberNameAttributeNotCSharpName()
    {
        Assert.Equal("dark-red", EnumCodes.ToCode(ProbeColor.DarkRed));
    }

    [Fact]
    public void Parse_ReadsCodeBackToMember()
    {
        Assert.Equal(ProbeColor.LightBlue, EnumCodes.Parse<ProbeColor>("light-blue"));
    }

    [Theory]
    [InlineData("DarkRed")]
    [InlineData("DARK-RED")]
    [InlineData(null)]
    public void TryParse_UnknownCode_Fails(string? code)
    {
        Assert.False(EnumCodes.TryParse(typeof(ProbeColor), code, out _));
    }

    [Fact]
    public void MemberWithoutCode_FailsLoudly()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => EnumCodes.ToCode(UncodedProbe.Missing));

        Assert.Contains("UncodedProbe.Missing", exception.Message);
    }

    public enum ProbeColor
    {
        [JsonStringEnumMemberName("dark-red")]
        DarkRed,

        [JsonStringEnumMemberName("light-blue")]
        LightBlue,
    }

    public enum UncodedProbe
    {
        Missing,
    }
}
