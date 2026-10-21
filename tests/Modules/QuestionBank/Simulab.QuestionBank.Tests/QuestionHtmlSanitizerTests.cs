using Simulab.QuestionBank.Application.Content;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-95 BR8, BR8b and BR10: what the sanitizer lets through, what it never lets through, and the plain text it
/// derives. The allow-list is pinned as one list: widening it must show up in this file.
/// </summary>
public sealed class QuestionHtmlSanitizerTests
{
    private readonly QuestionHtmlSanitizer _sanitizer = new();

    // BR8b: one list in one place; widening it is a visible change.
    [Fact]
    public void AllowedTags_AreExactlyTheListOfTheRule()
    {
        QuestionHtmlSanitizer.AllowedTags.Should().BeEquivalentTo(
            "p", "br", "strong", "b", "em", "i", "u", "s", "sub", "sup", "ul", "ol", "li", "blockquote",
            "table", "thead", "tbody", "tr", "th", "td", "a", "img");
    }

    // AC3: script, event attributes, javascript: links and data: images never survive.
    [Fact]
    public void Sanitize_Script_IsRemovedWithItsText()
    {
        var result = _sanitizer.Sanitize("<p>Hello</p><script>alert('x')</script>");

        result.Html.Should().NotContain("script").And.NotContain("alert");
        result.PlainText.Should().Be("Hello");
    }

    [Fact]
    public void Sanitize_EventAttribute_IsRemoved()
    {
        var result = _sanitizer.Sanitize("<p onclick=\"steal()\">Hello</p>");

        result.Html.Should().NotContain("onclick").And.NotContain("steal");
        result.Html.Should().Contain("Hello");
    }

    [Fact]
    public void Sanitize_JavascriptLink_LosesItsHref()
    {
        var result = _sanitizer.Sanitize("<a href=\"javascript:alert(1)\">click</a>");

        result.Html.Should().NotContain("javascript");
        result.PlainText.Should().Be("click");
    }

    [Fact]
    public void Sanitize_DataImage_IsRemoved()
    {
        var result = _sanitizer.Sanitize("<p>Look</p><img src=\"data:image/png;base64,AAAA\">");

        result.Html.Should().NotContain("img").And.NotContain("data:");
    }

    [Fact]
    public void Sanitize_StyleClassAndDataAttributes_AreRemoved()
    {
        var result = _sanitizer.Sanitize("<p style=\"color:red\" class=\"x\" data-a=\"1\">Hello</p>");

        result.Html.Should().NotContain("style").And.NotContain("class").And.NotContain("data-");
    }

    // AC3: the allowed markup survives.
    [Fact]
    public void Sanitize_AllowedMarkup_Survives()
    {
        const string html =
            "<p>E = mc<sup>2</sup> and H<sub>2</sub>O is <strong>very</strong> <em>important</em></p>" +
            "<ul><li>one</li></ul><table><tbody><tr><td>cell</td></tr></tbody></table>";

        var result = _sanitizer.Sanitize(html);

        result.Html.Should().Contain("<sup>2</sup>").And.Contain("<sub>2</sub>").And.Contain("<strong>very</strong>")
            .And.Contain("<li>one</li>").And.Contain("<td>cell</td>");
    }

    [Fact]
    public void Sanitize_HttpsLink_IsKeptWithRelAndWithoutTarget()
    {
        var result = _sanitizer.Sanitize("<a href=\"https://example.com/a\" target=\"_blank\">site</a>");

        result.Html.Should().Contain("href=\"https://example.com/a\"");
        result.Html.Should().Contain("rel=\"noopener noreferrer\"");
        result.Html.Should().NotContain("target");
    }

    // BR8: Quill 2 writes a bullet list as an ordered list with a data attribute; it must come out a bullet list.
    [Fact]
    public void Sanitize_QuillBulletList_BecomesAnUnorderedList()
    {
        var result = _sanitizer.Sanitize("<ol><li data-list=\"bullet\">one</li><li data-list=\"bullet\">two</li></ol>");

        result.Html.Should().Contain("<ul>").And.NotContain("<ol>").And.NotContain("data-list");
        result.PlainText.Should().Be("one two");
    }

    [Fact]
    public void Sanitize_QuillOrderedList_StaysOrdered()
    {
        var result = _sanitizer.Sanitize("<ol><li data-list=\"ordered\">one</li></ol>");

        result.Html.Should().Contain("<ol>").And.NotContain("data-list");
    }

    // AC2: only empty tags is empty; BR10: tags are removed and whitespace collapsed for the plain text.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p><br></p>")]
    [InlineData("<p>&nbsp;</p>")]
    public void Sanitize_NothingButEmptyTags_HasNoPlainText(string? html)
    {
        var result = _sanitizer.Sanitize(html);

        result.PlainText.Should().BeEmpty();
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_PlainText_RemovesTagsAndCollapsesWhitespace()
    {
        var result = _sanitizer.Sanitize("<p>Crase   é</p>\n<p><strong>fundamental</strong></p>");

        result.PlainText.Should().Be("Crase é fundamental");
    }

    // Wrappers it does not allow keep their text.
    [Fact]
    public void Sanitize_DisallowedWrapper_KeepsItsText()
    {
        var result = _sanitizer.Sanitize("<div><span>Hello</span></div><h1>Title</h1>");

        result.PlainText.Should().Be("Hello Title");
        result.Html.Should().NotContain("<div").And.NotContain("<span").And.NotContain("<h1");
    }
}
