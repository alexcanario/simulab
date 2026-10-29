using System.Globalization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-181 AC1, BR1: the culture's decimal separator is the only separator a number accepts.</summary>
public sealed class AppNumberFormatTests
{
    private static readonly CultureInfo PtBr = new("pt-BR");
    private static readonly CultureInfo En = new("en");

    [Theory]
    [InlineData("pt-BR", "1,5", 1.5)]
    [InlineData("en", "1.5", 1.5)]
    [InlineData("pt-BR", "  60 ", 60)]
    [InlineData("pt-PT", "-2,25", -2.25)]
    [InlineData("en", "+3", 3)]
    public void ParseDecimal_TheCultureSeparator_IsAccepted(string culture, string text, double expected)
    {
        AppNumberFormat.TryParseDecimal(text, new CultureInfo(culture), out var value).Should().BeTrue();

        value.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("pt-BR", "1.5")]
    [InlineData("en", "1,5")]
    [InlineData("pt-BR", "1.000,5")]
    [InlineData("en", "1,000.5")]
    [InlineData("en", "abc")]
    [InlineData("en", "1e3")]
    [InlineData("en", "1.2.3")]
    public void ParseDecimal_TheOtherSeparatorOrLetters_IsRefused(string culture, string text)
    {
        AppNumberFormat.TryParseDecimal(text, new CultureInfo(culture), out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_Blank_IsNoValue(string? text)
    {
        AppNumberFormat.TryParseDecimal(text, En, out var decimalValue).Should().BeTrue();
        AppNumberFormat.TryParseInteger(text, En, out var integerValue).Should().BeTrue();

        decimalValue.Should().BeNull();
        integerValue.Should().BeNull();
    }

    [Theory]
    [InlineData("20", 20)]
    [InlineData(" 7 ", 7)]
    [InlineData("-3", -3)]
    public void ParseInteger_WholeNumber_IsAccepted(string text, int expected)
    {
        AppNumberFormat.TryParseInteger(text, PtBr, out var value).Should().BeTrue();

        value.Should().Be(expected);
    }

    [Theory]
    [InlineData("20,5")]
    [InlineData("2.026")]
    [InlineData("1 000")]
    [InlineData("abc")]
    [InlineData("99999999999")]
    public void ParseInteger_AnythingElse_IsRefused(string text)
    {
        AppNumberFormat.TryParseInteger(text, PtBr, out _).Should().BeFalse();
    }

    [Fact]
    public void Format_UsesTheCultureAndTheAskedDecimals()
    {
        AppNumberFormat.Format(1234.5m, 1, PtBr).Should().Be("1.234,5");
        AppNumberFormat.Format(1234.5m, 1, En).Should().Be("1,234.5");
        AppNumberFormat.Format(60m, 2, PtBr).Should().Be("60,00");
        AppNumberFormat.Format(null, 2, PtBr).Should().Be("—");
    }

    [Fact]
    public void FormatForInput_HasNoGroupSeparator_SoItParsesBack()
    {
        var text = AppNumberFormat.FormatForInput(1234.5m, 2, PtBr);

        text.Should().Be("1234,50");
        AppNumberFormat.TryParseDecimal(text, PtBr, out var back).Should().BeTrue();
        back.Should().Be(1234.5m);
        AppNumberFormat.FormatForInput(2026, PtBr).Should().Be("2026");
    }

    [Fact]
    public void Round_GoesAwayFromZeroToTheDecimals()
    {
        AppNumberFormat.Round(1.005m, 2).Should().Be(1.01m);
        AppNumberFormat.Round(2.5m, 0).Should().Be(3m);
    }
}
