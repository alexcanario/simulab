using Simulab.QuestionBank.Application.Content;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-107 BR3 and BR9: the sanitizer reads the blank markers of a fill-in-the-blanks statement from the text between
/// tags only, in order and with repeats, and the plain text shows each marker as four underscores. Without the flag
/// nothing changes for the other types.
/// </summary>
public sealed class BlankMarkerSanitizerTests
{
    private readonly QuestionHtmlSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_ReadingMarkers_ReturnsTheirNumbersInOrderWithRepeats()
    {
        var result = _sanitizer.Sanitize("<p>[[3]] veio antes de <strong>[[1]]</strong> e de [[3]].</p>", readBlankMarkers: true);

        result.BlankMarkers.Should().Equal(3, 1, 3);
    }

    [Fact]
    public void Sanitize_ReadingMarkers_ShowsEachMarkerAsFourUnderscoresInThePlainText()
    {
        var result = _sanitizer.Sanitize("<p>A capital é [[1]] e foi fundada em [[2]].</p>", readBlankMarkers: true);

        result.PlainText.Should().Be("A capital é ____ e foi fundada em ____.");
        result.Html.Should().Contain("[[1]]").And.Contain("[[2]]");
    }

    [Theory]
    [InlineData("<p>[[<strong>1</strong>]] e [[7]]</p>")]
    [InlineData("<p>[<em>[1]</em>] e [[7]]</p>")]
    [InlineData("<p>[[0]] [[01]] [[100]] [[ 1]] [1] e [[7]]</p>")]
    public void Sanitize_ReadingMarkers_AMarkerSplitByFormattingOrOutOfRange_IsPlainText(string html)
    {
        _sanitizer.Sanitize(html, readBlankMarkers: true).BlankMarkers.Should().Equal(7);
    }

    [Fact]
    public void Sanitize_ReadingMarkers_AMarkerInsideALinkAddressOrAPictureAlt_IsNotAMarker()
    {
        var html = "<p><a href=\"https://example.com/[[1]]\">site</a> " +
                   "<img src=\"/question-images/0198f5a0-0000-7000-8000-000000000001\" alt=\"[[2]]\"> e [[3]]</p>";

        _sanitizer.Sanitize(html, readBlankMarkers: true).BlankMarkers.Should().Equal(3);
    }
}
