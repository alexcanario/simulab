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

    /// <summary>Walks each title's siblings up to the next title: a caption of one kind never counts for another (BR3).</summary>
    internal static IReadOnlyList<Block> Read(IElement section)
    {
        var blocks = new List<Block>();

        foreach (var title in section.QuerySelectorAll(".gallery-kind-title"))
        {
            var cells = new List<Cell>();

            for (var node = title.NextElementSibling;
                 node is not null && !node.ClassList.Contains("gallery-kind-title");
                 node = node.NextElementSibling)
            {
                if (node.ClassList.Contains("gallery-state-caption"))
                {
                    var control = node.NextElementSibling;
                    cells.Add(new Cell(node.TextContent.Trim(), control?.ClassList.ToList() ?? []));
                }
            }

            blocks.Add(new Block(title.TextContent.Trim(), cells));
        }

        return blocks;
    }

    /// <summary>One line per problem, <c>&lt;kind&gt;: &lt;what is wrong&gt;</c>; empty when every expected kind holds every state.</summary>
    internal static IReadOnlyList<string> Problems(IElement section, IReadOnlyList<Expectation> expected)
    {
        var blocks = Read(section);
        var problems = new List<string>();

        foreach (var kind in expected)
        {
            var block = blocks.FirstOrDefault(candidate => candidate.Title == kind.Title);
            if (block is null)
            {
                problems.Add($"{kind.Title}: no block with that title");
                continue;
            }

            foreach (var state in kind.States)
            {
                var cell = block.Cells.FirstOrDefault(candidate => candidate.Caption == state);
                if (cell is null)
                {
                    problems.Add($"{kind.Title}: missing state {state}");
                }
                else if (!cell.ControlClasses.Contains(kind.ControlClass))
                {
                    problems.Add($"{kind.Title}: state {state} is not followed by an {kind.ControlClass}");
                }
            }
        }

        return problems;
    }
}
