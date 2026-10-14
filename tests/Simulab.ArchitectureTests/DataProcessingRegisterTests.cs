using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-93: <c>docs/privacy/data-processing-register.md</c> lists the personal data Simulab processes and its processors, and
/// every row cites a file that exists. The tests check structure and citations, not the legal text.
/// </summary>
public class DataProcessingRegisterTests
{
    private const string RegisterPath = "docs/privacy/data-processing-register.md";

    private static readonly string[] DataColumns =
    [
        "Category", "Data fields", "Purpose", "Legal basis for processing", "Storage and region", "Processor",
        "Transfer basis", "Retention", "How the person exercises their rights", "Source",
    ];

    private static readonly string[] ProcessorColumns =
        ["Company", "Service used", "Personal data it receives", "Where it processes", "Transfer basis", "Contract", "Source"];

    private static readonly string[] RequiredCategories = ["Account", "Consent evidence", "Account events", "One-time tokens", "Sessions"];

    private static readonly Regex PathInBackticks = new(@"`([^`\s]+/[^`\s]+\.[A-Za-z]+)`", RegexOptions.CultureInvariant);

    private static string Root => SolutionAssemblies.RepositoryRoot();

    /// <summary>The header and rows of the first table under the heading, cells trimmed.</summary>
    private static (List<string> Header, List<List<string>> Rows) Table(string heading)
    {
        var lines = File.ReadAllLines(Path.Combine(Root, RegisterPath)).ToList();
        var start = lines.FindIndex(line => line == heading);
        start.Should().BeGreaterThanOrEqualTo(0, $"the register has a '{heading}' section");

        var tableLines = lines.Skip(start + 1).SkipWhile(line => !line.StartsWith('|')).TakeWhile(line => line.StartsWith('|')).ToList();
        tableLines.Count.Should().BeGreaterThan(2, $"'{heading}' holds a table with at least one row");

        return (Cells(tableLines[0]), tableLines.Skip(2).Select(Cells).ToList());
    }

    private static List<string> Cells(string line) => line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

    [Fact]
    public void Register_HasBothTablesAndEveryRowCitesItsSource()
    {
        var (dataHeader, dataRows) = Table("## Personal data");
        var (processorHeader, processorRows) = Table("## Processors");

        dataHeader.Should().Equal(DataColumns, "BR5 fixes the columns of the personal data table");
        processorHeader.Should().Equal(ProcessorColumns, "BR6 fixes the columns of the processors table");
        dataRows.Select(row => row[0]).Should().Contain(RequiredCategories, "AC5: the categories the code holds today");

        var tables = new[] { (Columns: DataColumns.Length, Rows: dataRows), (Columns: ProcessorColumns.Length, Rows: processorRows) };
        foreach (var (columns, row) in tables.SelectMany(table => table.Rows.Select(row => (table.Columns, row))))
        {
            row.Count.Should().Be(columns, $"row '{row[0]}' has every cell");
            row.Should().OnlyContain(cell => cell.Length > 0, $"row '{row[0]}' leaves no cell empty: a gap says 'not verified' or 'none defined'");

            var paths = PathInBackticks.Matches(row[^1]).Select(match => match.Groups[1].Value).ToList();
            paths.Should().NotBeEmpty($"AC5: row '{row[0]}' cites the file it came from");
            foreach (var path in paths)
            {
                File.Exists(Path.Combine(Root, path)).Should().BeTrue($"row '{row[0]}' cites {path}, which must exist");
            }
        }
    }

    [Fact]
    public void Processors_NameAzureAnthropicAndTheEmailServiceWithTheirRegion()
    {
        var (_, rows) = Table("## Processors");
        var byCompany = rows.ToDictionary(row => row[0]);

        byCompany.Keys.Should().Contain(["Microsoft Azure", "Microsoft Azure (Communication Services)", "Anthropic"], "AC6");
        byCompany["Microsoft Azure"][3].Should().Contain("Central US");
        byCompany["Microsoft Azure (Communication Services)"][3].Should().Contain("Brazil", "AC6: the email service's data location");
        byCompany["Anthropic"][3].Should().Contain("United States");
    }
}
