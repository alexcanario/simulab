using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-68: <c>docs/infra.md</c> lists every URI of the project in its <c>## Access</c> table, with how Claude signs in and where
/// the credential is kept (a pointer, never a value: the repository is public), and declares one <c>## Cloud accounts</c> row
/// per non-local environment, which <c>/agile:publish</c> reads before it deploys.
/// </summary>
public class InfraAccessTests
{
    private const string InfraPath = "docs/infra.md";
    private const string RulesPath = ".claude/rules/agile/project.md";

    private static readonly string[] AccessColumns = ["What", "Status", "URI", "Claude signs in with", "Check", "Credential kept in"];

    private static readonly string[] CloudColumns =
        ["Environment", "Client", "Cloud", "Tenant", "Subscription or account", "Resource group", "Region"];

    /// <summary>The rows of the approved list (F-68 "Access rows"), in order. The file may not drop or reorder one.</summary>
    private static readonly string[] ApprovedAccessRows =
    [
        "Repository",
        "Board: issues",
        "Board: project",
        "Releases",
        "CI pipeline (GitHub Actions)",
        "Deploy pipeline (GitHub Actions)",
        "Azure portal and subscription",
        "Key Vault",
        "Staging Web",
        "Staging Api",
        "Production Web",
        "Production Api",
        "Local Web",
        "Local UI kit gallery",
        "Local Api",
        "Local OpenAPI document",
        "Local token endpoint",
        "Local Aspire dashboard",
        "Local Mailpit",
        "Local PostgreSQL",
        "Local Redis",
        "Claude API console",
        "Google Cloud console (OAuth client, F-20)",
        "Former board (Azure Boards)",
    ];

    private static readonly string[] AccessStatuses = ["provisioned", "planned", "retired"];

    private static readonly string[] Clouds = ["azure", "aws", "gcp", "other", "none"];

    /// <summary>Exactly the pointers BR3 allows. A password typed in the cell matches none of them.</summary>
    private static readonly Regex CredentialPointer = new(
        @"^(none|gh keyring|az login|app host|owner only|Key Vault: \S+|user secrets: \S+)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex GuidShape = new(
        @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal sealed record MarkdownTable(IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows);

    /// <summary>The text between <c>## {heading}</c> and the next <c>## </c> heading, or null when the heading is absent.</summary>
    internal static string? Section(string markdown, string heading)
    {
        var match = Regex.Match(
            markdown,
            @"^## " + Regex.Escape(heading) + @"[ \t]*\n(?<body>.*?)(?=^## |\z)",
            RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["body"].Value : null;
    }

    /// <summary>The first pipe table of a section: header cells, then one list of cells per row. Backticks are dropped.</summary>
    internal static MarkdownTable FirstTable(string section)
    {
        var lines = section.Split('\n')
            .Select(line => line.Trim())
            .SkipWhile(line => !line.StartsWith('|'))
            .TakeWhile(line => line.StartsWith('|'))
            .ToList();
        if (lines.Count < 2)
        {
            return new MarkdownTable([], []);
        }

        return new MarkdownTable(Cells(lines[0]), [.. lines.Skip(2).Select(Cells)]);
    }

    private static List<string> Cells(string line) =>
        [.. line.Trim('|').Split('|').Select(cell => cell.Trim().Replace("`", string.Empty, StringComparison.Ordinal))];

    /// <summary>One line per row whose credential cell is not one of the allowed pointers; it names the row, not the cell's text.</summary>
    internal static IReadOnlyList<string> CredentialProblems(MarkdownTable access)
    {
        var column = access.Header.ToList().IndexOf("Credential kept in");
        return
        [
            .. access.Rows.Where(row => !CredentialPointer.IsMatch(row[column]))
                .Select(row => $"row '{row[0]}': 'Credential kept in' is not a pointer (none, gh keyring, az login, Key Vault: <name>, user secrets: <key>, app host, owner only)"),
        ];
    }

    /// <summary>One line per cloud account row whose Cloud is unknown or whose Tenant or Subscription is neither empty nor a GUID.</summary>
    internal static IReadOnlyList<string> CloudAccountProblems(MarkdownTable accounts)
    {
        var problems = new List<string>();
        foreach (var row in accounts.Rows)
        {
            var cell = (string name) => row[accounts.Header.ToList().IndexOf(name)];
            if (!Clouds.Contains(cell("Cloud"), StringComparer.Ordinal))
            {
                problems.Add($"row '{row[0]}': Cloud '{cell("Cloud")}' is not one of {string.Join(", ", Clouds)}");
            }

            foreach (var name in new[] { "Tenant", "Subscription or account" })
            {
                if (cell(name).Length > 0 && !GuidShape.IsMatch(cell(name)))
                {
                    problems.Add($"row '{row[0]}': {name} '{cell(name)}' is neither empty nor a GUID");
                }
            }
        }

        return problems;
    }

    private static string ReadRepositoryFile(string relativePath) =>
        File.ReadAllText(Path.Combine(SolutionAssemblies.RepositoryRoot(), relativePath)).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Infra() => ReadRepositoryFile(InfraPath);

    private static MarkdownTable AccessTable() => FirstTable(Section(Infra(), "Access") ?? string.Empty);

    private static MarkdownTable CloudAccountsTable() => FirstTable(Section(Infra(), "Cloud accounts") ?? string.Empty);

    [Fact]
    public void Infra_AccessSection_HasTheSixColumnsInOrder()
    {
        Section(Infra(), "Access").Should().NotBeNull("docs/infra.md has an '## Access' section (F-68 BR1)");
        AccessTable().Header.Should().Equal(AccessColumns, "BR1 fixes the six columns and their order");
    }

    [Fact]
    public void Infra_AccessTable_HasEveryApprovedRowInOrderWithAKnownStatus()
    {
        var table = AccessTable();

        table.Rows.Select(row => row[0]).Should().Equal(ApprovedAccessRows, "BR2: the rows of the approved list, in that order");
        table.Rows.Select(row => row[1]).Should().OnlyContain(
            status => AccessStatuses.Contains(status), "Status is provisioned, planned or retired");
    }

    [Fact]
    public void Infra_AccessTable_CredentialCellsAreOnlyPointers()
    {
        string.Join("\n", CredentialProblems(AccessTable())).Should().BeEmpty(
            "the repository is public: the file points to where a credential is kept and never holds one (F-68 BR3)");
    }

    [Fact]
    public void CredentialProblems_ALiteralPassword_NamesTheRowAndNotTheValue()
    {
        var table = new MarkdownTable(AccessColumns, [["Local PostgreSQL", "provisioned", "127.0.0.1:5432", "none", "—", "postgres"]]);

        var problems = CredentialProblems(table);

        problems.Should().ContainSingle().Which.Should().Contain("row 'Local PostgreSQL'").And.NotContain("postgres'");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("gh keyring")]
    [InlineData("az login")]
    [InlineData("app host")]
    [InlineData("owner only")]
    [InlineData("Key Vault: deploy--PostgresAdminPassword")]
    [InlineData("user secrets: Ai:ApiKey")]
    public void CredentialProblems_EachAllowedPointer_IsAccepted(string form)
    {
        var table = new MarkdownTable(AccessColumns, [["Any", "provisioned", "—", "none", "—", form]]);

        CredentialProblems(table).Should().BeEmpty();
    }

    [Fact]
    public void Infra_AccessTable_APlannedRowHasNoUri()
    {
        var planned = AccessTable().Rows.Where(row => row[1] == "planned").ToList();

        planned.Should().NotBeEmpty("a parser that finds no planned row would let this test pass for free");
        planned.Where(row => row[2] != "—").Select(row => row[0]).Should().BeEmpty(
            "a planned row has — as URI until its item fills it; a URI is never invented (F-68 BR2)");
    }

    [Fact]
    public void ProjectRules_StateTheFailedCheckRuleAndTheNoCredentialRule()
    {
        var rules = ReadRepositoryFile(RulesPath);

        rules.Should().Contain("runs the row's `Check` first", "BR5: a step that needs an access checks it first");
        rules.Should().Contain("ask the owner to run the row's `Claude signs in with` command", "BR5: a failed check stops the step");
        rules.Should().Contain("`docs/infra.md` never holds a credential value", "BR6: the repository is public");
    }

    [Fact]
    public void Infra_CloudAccounts_HasTheSevenColumnsAndOneRowPerNonLocalEnvironment()
    {
        var environments = FirstTable(Section(Infra(), "Environments") ?? string.Empty);
        var expected = environments.Rows.Select(row => row[0]).Where(name => name != "local").ToList();

        Section(Infra(), "Cloud accounts").Should().NotBeNull("docs/infra.md has a '## Cloud accounts' section (F-68 BR8)");
        CloudAccountsTable().Header.Should().Equal(CloudColumns, "BR8 fixes the seven columns and their order");
        expected.Should().NotBeEmpty("a parser that finds no environment would let the row check pass for free");
        CloudAccountsTable().Rows.Select(row => row[0]).Should().Equal(
            expected, "exactly one row per non-local environment of '## Environments', in the same order");
    }

    [Fact]
    public void Infra_CloudAccounts_CloudIsKnownAndIdsAreEmptyOrGuids()
    {
        string.Join("\n", CloudAccountProblems(CloudAccountsTable())).Should().BeEmpty(
            "an unknown id stays empty so /agile:publish asks for it by name; a name or a placeholder fails (F-68 BR9)");
    }

    [Theory]
    [InlineData("TBD")]
    [InlineData("—")]
    [InlineData("Azure subscription 1")]
    public void CloudAccountProblems_APlaceholderOrNameAsTenant_NamesTheRow(string tenant)
    {
        var table = new MarkdownTable(CloudColumns, [["staging", "Simulab", "azure", tenant, string.Empty, string.Empty, "brazilsouth"]]);

        CloudAccountProblems(table).Should().ContainSingle().Which.Should().Contain("row 'staging'").And.Contain("Tenant");
    }

    [Fact]
    public void CloudAccountProblems_AnEmptyIdAndAGuid_AreAccepted()
    {
        var table = new MarkdownTable(
            CloudColumns,
            [
                ["staging", "Simulab", "azure", string.Empty, string.Empty, string.Empty, "brazilsouth"],
                ["production", "Simulab", "azure", "11111111-2222-3333-4444-555555555555", "AAAAAAAA-bbbb-cccc-dddd-eeeeeeeeeeee", "rg-simulab", "brazilsouth"],
            ]);

        CloudAccountProblems(table).Should().BeEmpty();
    }

    [Fact]
    public void CloudAccountProblems_AnUnknownCloud_NamesTheRow()
    {
        var table = new MarkdownTable(CloudColumns, [["staging", "Simulab", "heroku", string.Empty, string.Empty, string.Empty, "brazilsouth"]]);

        CloudAccountProblems(table).Should().ContainSingle().Which.Should().Contain("row 'staging'").And.Contain("heroku");
    }
}
