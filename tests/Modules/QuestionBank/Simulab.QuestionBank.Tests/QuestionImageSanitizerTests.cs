using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-103 BR4, BR5 and BR8 on the sanitizer alone: which <c>img</c> survives, what it keeps, what it counts and what the
/// plain text of an image is. The pin of the allow-list itself stays in <see cref="QuestionHtmlSanitizerTests"/>.
/// </summary>
public sealed class QuestionImageSanitizerTests
{
    private static readonly Guid Id = Guid.Parse("0199a3b4-1111-7222-8333-444455556666");
    private readonly QuestionHtmlSanitizer _sanitizer = new();

    // AC7 (T7, BR5): an external, a data: and a javascript: address remove the img; text around it stays.
    [Theory]
    [InlineData("<img src=\"https://x.example/y.png\" alt=\"a\">")]
    [InlineData("<img src=\"http://x.example/y.png\" alt=\"a\">")]
    [InlineData("<img src=\"data:image/png;base64,iVBORw0KGgo=\" alt=\"a\">")]
    [InlineData("<img src=\"javascript:alert(1)\" alt=\"a\">")]
    [InlineData("<img src=\"/other/path.png\" alt=\"a\">")]
    [InlineData("<img src=\"/question-images/not-a-guid\" alt=\"a\">")]
    [InlineData("<img src=\"/question-images/0199a3b4-1111-7222-8333-444455556666/extra\" alt=\"a\">")]
    [InlineData("<img src=\"//evil.example/question-images/0199a3b4-1111-7222-8333-444455556666\" alt=\"a\">")]
    [InlineData("<img alt=\"no source\">")]
    public void Sanitize_AnImgWithAnAddressOtherThanAStoredImage_IsRemoved(string img)
    {
        // A stored image beside it must survive, so the test fails while no image is allowed at all.
        var result = _sanitizer.Sanitize($"<p>Look: {img} done <img src=\"/question-images/{Id}\" alt=\"kept\"></p>");

        result.ImageIds.Should().Equal(Id);
        result.Html.Split("<img").Should().HaveCount(2, "only the stored image is left");
        result.PlainText.Should().Be("Look: done kept");
    }

    // AC7 (T7, BR5): the stored address stays and keeps only src and alt.
    [Fact]
    public void Sanitize_AStoredImageWithExtraAttributes_KeepsOnlySrcAndAlt()
    {
        var result = _sanitizer.Sanitize(
            $"<p><img src=\"/question-images/{Id}\" alt=\"A map\" onerror=\"steal()\" onclick=\"x()\" style=\"width:9999px\" width=\"5000\" class=\"big\" data-x=\"1\" title=\"t\"></p>");

        result.Html.Should().Be($"<p><img src=\"/question-images/{Id}\" alt=\"A map\"></p>");
        result.ImageIds.Should().Equal(Id);
    }

    // AC7 / decisions: Quill puts an image inside a list item and a table cell, and an option holds one alone.
    [Theory]
    [InlineData("<ul><li>One <img src=\"/question-images/0199a3b4-1111-7222-8333-444455556666\" alt=\"a\"></li></ul>")]
    [InlineData("<table><tbody><tr><td><img src=\"/question-images/0199a3b4-1111-7222-8333-444455556666\" alt=\"a\"></td></tr></tbody></table>")]
    [InlineData("<img src=\"/question-images/0199a3b4-1111-7222-8333-444455556666\" alt=\"a\">")]
    public void Sanitize_AStoredImageInsideAListItemATableCellOrAlone_IsKept(string html)
    {
        var result = _sanitizer.Sanitize(html);

        result.Html.Should().Contain($"src=\"/question-images/{Id}\"");
        result.ImageIds.Should().Equal(Id);
    }

    // BR6: the ids come in order of appearance, every occurrence listed.
    [Fact]
    public void Sanitize_SeveralImages_ListsTheirIdsInOrder()
    {
        var other = Guid.CreateVersion7();

        var result = _sanitizer.Sanitize($"<p><img src=\"/question-images/{other}\" alt=\"b\"></p><p><img src=\"/question-images/{Id}\" alt=\"a\"></p>");

        result.ImageIds.Should().Equal(other, Id);
    }

    // AC11 (BR4): no alt attribute is counted; an empty alt means decorative and is not.
    [Fact]
    public void Sanitize_AnImgWithNoAlt_IsCountedAsWithoutAlt()
    {
        var result = _sanitizer.Sanitize($"<p>Text <img src=\"/question-images/{Id}\"></p>");

        result.ImagesWithoutAlt.Should().Be(1);
        result.AltTooLong.Should().Be(0);
    }

    [Fact]
    public void Sanitize_AnImgWithAnEmptyAlt_IsDecorativeAndNotCounted()
    {
        var result = _sanitizer.Sanitize($"<p>Text <img src=\"/question-images/{Id}\" alt=\"\"></p>");

        result.ImagesWithoutAlt.Should().Be(0);
        result.AltTooLong.Should().Be(0);
        result.ImageIds.Should().Equal(Id);
    }

    // AC11 (BR4): 250 characters is the longest alt.
    [Theory]
    [InlineData(250, 0)]
    [InlineData(251, 1)]
    public void Sanitize_AnAltOverTheLimit_IsCountedAsTooLong(int length, int expectedTooLong)
    {
        var result = _sanitizer.Sanitize($"<p><img src=\"/question-images/{Id}\" alt=\"{new string('a', length)}\"></p>");

        result.ImageIds.Should().Equal(Id);
        result.AltTooLong.Should().Be(expectedTooLong);
        QuestionLimits.ImageAltMaxLength.Should().Be(250);
    }

    // AC12 (BR8): a text that holds only an image is not empty, even a decorative one.
    [Theory]
    [InlineData("alt=\"A graph of the rates\"")]
    [InlineData("alt=\"\"")]
    public void Sanitize_ATextWithOnlyAnImage_IsNotEmpty(string alt)
    {
        var result = _sanitizer.Sanitize($"<p><img src=\"/question-images/{Id}\" {alt}></p>");

        result.IsEmpty.Should().BeFalse();
        result.Html.Should().Contain("<img");
    }

    // BR8: the plain text (excerpt, search) carries the alt of a picture, not of a decorative one.
    [Fact]
    public void Sanitize_PlainText_CarriesTheAltOfANonDecorativeImageOnly()
    {
        var other = Guid.CreateVersion7();

        var result = _sanitizer.Sanitize(
            $"<p>See <img src=\"/question-images/{Id}\" alt=\"the map of Brazil\"> and <img src=\"/question-images/{other}\" alt=\"\"> now</p>");

        result.PlainText.Should().Be("See the map of Brazil and now");
    }
}
