using AngleSharp.Dom;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-189: reads a gallery section as a matrix of control kind by state, so a test can say which kind lacks which state.</summary>
internal static class GalleryStateMatrix
{
    /// <summary>One state of one kind: its caption and the CSS classes of the element that follows the caption.</summary>
    internal sealed record Cell(string Caption, IReadOnlyList<string> ControlClasses);

    /// <summary>One control kind: the text of its title and the states listed under it, up to the next title.</summary>
    internal sealed record Block(string Title, IReadOnlyList<Cell> Cells);

    /// <summary>The expected shape of one kind: the title, the class of its control and the states that apply to it.</summary>
    internal sealed record Expectation(string Title, string ControlClass, IReadOnlyList<string> States);

    internal static IReadOnlyList<Block> Read(IElement section)
    {
        throw new NotImplementedException();
    }

    /// <summary>One line per problem, <c>&lt;kind&gt;: &lt;what is wrong&gt;</c>; empty when every expected kind holds every state.</summary>
    internal static IReadOnlyList<string> Problems(IElement section, IReadOnlyList<Expectation> expected)
    {
        throw new NotImplementedException();
    }
}
