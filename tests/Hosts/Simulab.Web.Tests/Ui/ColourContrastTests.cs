namespace Simulab.Web.Tests.Ui;

public sealed class ColourContrastTests
{
    [Theory]
    [InlineData("#000000", "#FFFFFF")]
    [InlineData("rgba(0,0,0,1)", "rgb(255,255,255)")]
    public void Ratio_BlackOnWhite_IsTwentyOne(string foreground, string background)
    {
        ColourContrast.Ratio(foreground, background).Should().BeApproximately(21, 0.001);
    }

    /// <summary>F-17 BR7: half-transparent black on white is the grey it shows, not black (21:1).</summary>
    [Fact]
    public void Ratio_TranslucentForeground_IsCompositedOverTheBackground()
    {
        var translucent = ColourContrast.Ratio("rgba(0,0,0,0.5)", "#FFFFFF");

        translucent.Should().BeApproximately(ColourContrast.Ratio("rgb(127.5,127.5,127.5)", "#FFFFFF"), 1e-9);
        translucent.Should().BeLessThan(4.5);
    }

    [Fact]
    public void Ratio_HexWithAlpha_IsCompositedToo()
    {
        ColourContrast.Ratio("#00000080", "#FFFFFF").Should().BeApproximately(ColourContrast.Ratio("rgba(0,0,0,0.50196)", "#FFFFFF"), 1e-4);
    }
}
