using System.Globalization;
using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// WCAG 2.2 AA (ADR-0001 #29) on the colours a screen shows as text. Found in F-10: the dark palette had
/// kept the light red, so every error message on a dark card sat at 2.93:1. A number, not an opinion.
/// </summary>
public sealed class ThemeContrastTests
{
    private const double MinimumForText = 4.5;

    private static readonly MudTheme Theme = SimulabTheme.Create();

    public static TheoryData<string, string, string> ErrorTextOnItsSurfaces()
    {
        var light = Theme.PaletteLight;
        var dark = Theme.PaletteDark;
        return new TheoryData<string, string, string>
        {
            { "light on surface", light.Error.ToString(), light.Surface.ToString() },
            { "light on background", light.Error.ToString(), light.Background.ToString() },
            { "dark on surface", dark.Error.ToString(), dark.Surface.ToString() },
            { "dark on background", dark.Error.ToString(), dark.Background.ToString() }
        };
    }

    /// <summary>The error colour is the text of a field error, a danger zone and an outlined destructive button.</summary>
    [Theory]
    [MemberData(nameof(ErrorTextOnItsSurfaces))]
    public void ErrorText_ReadsAtAaOnItsSurface(string what, string foreground, string background)
    {
        Contrast(foreground, background).Should().BeGreaterThanOrEqualTo(MinimumForText, $"error text must be AA readable ({what})");
    }

    /// <summary>And the filled destructive button puts its contrast text on top of that same colour.</summary>
    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void ErrorContrastText_ReadsAtAaOnTheErrorColour(string palette)
    {
        var colours = palette == "light" ? (Theme.PaletteLight.Error, Theme.PaletteLight.ErrorContrastText) : (Theme.PaletteDark.Error, Theme.PaletteDark.ErrorContrastText);

        Contrast(colours.Item2.ToString(), colours.Item1.ToString()).Should().BeGreaterThanOrEqualTo(MinimumForText);
    }

    private static double Contrast(string foreground, string background)
    {
        var first = Luminance(foreground);
        var second = Luminance(background);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    /// <summary>The palette gives its colours as <c>rgba(r,g,b,a)</c>; a hex literal is accepted too.</summary>
    private static double Luminance(string colour)
    {
        var hex = colour.TrimStart('#');
        var channels = (colour.StartsWith('#')
            ? Enumerable.Range(0, 3).Select(index => int.Parse(hex.AsSpan(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            : colour[(colour.IndexOf('(', StringComparison.Ordinal) + 1)..colour.IndexOf(')', StringComparison.Ordinal)]
                .Split(',')
                .Take(3)
                .Select(part => int.Parse(part.Trim(), CultureInfo.InvariantCulture)))
            .Select(value => value / 255d)
            .Select(value => value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4))
            .ToArray();

        channels.Should().HaveCount(3, $"'{colour}' must be a colour");
        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
    }
}
