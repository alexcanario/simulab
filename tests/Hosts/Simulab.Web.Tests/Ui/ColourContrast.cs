using System.Globalization;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// WCAG 2.2 contrast ratio between two theme colours. The palette gives its colours as <c>rgba(r,g,b,a)</c>,
/// <c>rgb(r,g,b)</c> or a hex literal; a translucent foreground is composited over the background first (B-9),
/// or the raw channels would read as a false failure — or a false pass.
/// </summary>
internal static class ColourContrast
{
    public const double MinimumForText = 4.5;

    /// <summary>WCAG 2.2 1.4.11: an icon or a control's boundary, with no text of its own.</summary>
    public const double MinimumForNonText = 3.0;

    public static double Ratio(string foreground, string background)
    {
        var under = Parse(background);
        var over = Composite(Parse(foreground), under);
        var first = Luminance(over);
        var second = Luminance(under);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    internal static (double Red, double Green, double Blue, double Alpha) Parse(string colour)
    {
        if (colour.StartsWith('#'))
        {
            var hex = colour[1..];
            hex.Length.Should().BeOneOf([6, 8], $"'{colour}' must be a colour");
            var channel = (int index) => (double)int.Parse(hex.AsSpan(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return (channel(0), channel(1), channel(2), hex.Length == 8 ? channel(3) / 255d : 1d);
        }

        var parts = colour[(colour.IndexOf('(', StringComparison.Ordinal) + 1)..colour.IndexOf(')', StringComparison.Ordinal)]
            .Split(',')
            .Select(part => double.Parse(part.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
        parts.Length.Should().BeOneOf([3, 4], $"'{colour}' must be a colour");
        return (parts[0], parts[1], parts[2], parts.Length == 4 ? parts[3] : 1d);
    }

    private static (double Red, double Green, double Blue, double Alpha) Composite(
        (double Red, double Green, double Blue, double Alpha) over,
        (double Red, double Green, double Blue, double Alpha) under)
    {
        under.Alpha.Should().Be(1d, "a background must be opaque to be measured");
        var alpha = over.Alpha;
        return (
            (over.Red * alpha) + (under.Red * (1 - alpha)),
            (over.Green * alpha) + (under.Green * (1 - alpha)),
            (over.Blue * alpha) + (under.Blue * (1 - alpha)),
            1d);
    }

    private static double Luminance((double Red, double Green, double Blue, double Alpha) colour)
    {
        static double Linear(double value)
        {
            var channel = value / 255d;
            return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Linear(colour.Red)) + (0.7152 * Linear(colour.Green)) + (0.0722 * Linear(colour.Blue));
    }
}
