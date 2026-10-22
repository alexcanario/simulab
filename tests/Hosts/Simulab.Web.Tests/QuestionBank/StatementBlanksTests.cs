using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-107 BR3 (review #1): the form reads the markers of the editor's HTML as the Api does, from the text between tags
/// only; a marker inside an attribute is never one, even when the attribute holds a '>' the editor does not encode.
/// </summary>
public sealed class StatementBlanksTests
{
    [Fact]
    public void Markers_InsideAnAttributeThatHoldsAGreaterThanSign_AreNotMarkers()
    {
        const string html = "<p>[[1]] <img src=\"/question-images/x\" alt=\"a > [[3]]\"> e <a href='https://e.com/?q=1>2&[[4]]'>[[2]]</a></p>";

        StatementBlanks.Markers(html).Should().Equal(1, 2);
    }

    [Fact]
    public void Replace_LeavesTheMarkersOfAnAttributeAsTheyAre()
    {
        const string html = "<p>[[1]] <img alt=\"x > [[3]]\"></p>";

        StatementBlanks.Replace(html, number => $"<b>{number}</b>").Should().Be("<p><b>1</b> <img alt=\"x > [[3]]\"></p>");
    }
}
