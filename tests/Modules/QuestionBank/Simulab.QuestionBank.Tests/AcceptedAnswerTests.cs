using Simulab.QuestionBank.Domain;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-107 AC9, BR6: the one comparison rule for typed answers: case and extra white space are ignored, accents and every
/// other character count.
/// </summary>
public sealed class AcceptedAnswerTests
{
    [Theory]
    [InlineData("  Rio   de Janeiro ", "rio de janeiro")]
    [InlineData("BRASÍLIA", "Brasília")]
    [InlineData("são\tpaulo", "São Paulo")]
    [InlineData("1960", " 1960 ")]
    public void Matches_IgnoringCaseAndExtraWhiteSpace_IsTrue(string typed, string accepted)
    {
        AcceptedAnswer.Matches(typed, accepted).Should().BeTrue();
    }

    [Theory]
    [InlineData("Brasilia", "Brasília")]
    [InlineData("Sao Paulo", "São Paulo")]
    [InlineData("Rio-de-Janeiro", "Rio de Janeiro")]
    [InlineData("1960.", "1960")]
    public void Matches_WhenAnAccentOrAnyOtherCharacterDiffers_IsFalse(string typed, string accepted)
    {
        AcceptedAnswer.Matches(typed, accepted).Should().BeFalse();
    }

    [Fact]
    public void Normalize_TrimsCollapsesWhiteSpaceAndUppersCase_KeepingAccents()
    {
        AcceptedAnswer.Normalize("  Brasília \n é   a capital ").Should().Be("BRASÍLIA É A CAPITAL");
    }

    [Fact]
    public void Normalize_OfNothing_IsEmpty()
    {
        AcceptedAnswer.Normalize(null).Should().BeEmpty();
        AcceptedAnswer.Normalize("   ").Should().BeEmpty();
    }
}
