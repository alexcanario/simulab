using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-93: ADR-0004 records the data region of record (Azure Central US) and the legal basis of each user population, and
/// ADR-0002 and ADR-0003 name it. These tests check the structure and the wording markers of the documents, not the truth
/// of the legal text: a lawyer reviews that (F-93 D4).
/// </summary>
public class DataRegionDocumentsTests
{
    private const string AdrPath = "docs/decisions/ADR-0004-data-region-central-us.md";
    private const string RegisterPath = "docs/privacy/data-processing-register.md";
    private const string DraftNotice = "Draft: not reviewed by a lawyer.";

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(SolutionAssemblies.RepositoryRoot(), relativePath));

    [Fact]
    public void Adr0004_NamesTheRegionTheOptionsAndTheChoice()
    {
        var adr = Read(AdrPath);

        adr.Should().Contain("`centralus`", "AC1: the region of record");
        adr.Should().Contain("**A. Brazil South for staging and production.**", "AC1: option A");
        adr.Should().Contain("**B. Central US for staging only", "AC1: option B");
        adr.Should().Contain("**C. Central US for staging and production", "AC1: option C");
        adr.Should().Contain("7.2 against 5.2", "AC1: the owner's choice against the recommendation, with the averages");
        adr.Should().Contain("The decision is reopened when any of these happens", "AC1: the reopen triggers");
    }

    [Fact]
    public void Adr0004_HasALegalBasisBlockPerPopulationWithSourceAndDate()
    {
        var adr = Read(AdrPath);

        adr.Should().Contain("**Users in Brazil.**");
        adr.Should().Contain("LGPD Art. 33");
        adr.Should().Contain("Resolution CD/ANPD 19/2024");
        adr.Should().Contain("**Users in the EU.**");
        adr.Should().Contain("GDPR Art. 45");
        adr.Should().Contain("GDPR Art. 46");
        adr.Should().Contain("Data Privacy Framework");

        var sources = Regex.Matches(adr, @"\]\(https://[^)]+\)[^.]*?(read|dated) \d{4}-\d{2}-\d{2}", RegexOptions.CultureInvariant);
        sources.Count.Should().BeGreaterThanOrEqualTo(3, "AC2: every source carries the date it was read");
    }

    [Fact]
    public void UnverifiedBasis_IsMarkedAsAGapWithActionAndOwner()
    {
        var adr = Read(AdrPath);
        var register = Read(RegisterPath);

        Regex.Count(adr, "basis not in place").Should().BeGreaterThanOrEqualTo(2, "AC3: Microsoft and Anthropic for users in Brazil");
        Regex.Count(adr, "Action: ").Should().BeGreaterThanOrEqualTo(2, "AC3: each gap names its action");
        Regex.Count(adr, "Owner: ").Should().BeGreaterThanOrEqualTo(2, "AC3: and its owner");
        adr.Should().NotContain("basis is in place", "AC3: a basis nobody verified is never written as in place");
        register.Should().Contain("basis not in place");
    }

    [Fact]
    public void Adr0002AndAdr0003_NameAdr0004()
    {
        var adr0002 = Read("docs/decisions/ADR-0002-host.md");
        var adr0003 = Read("docs/decisions/ADR-0003-data-region.md");

        adr0002.Should().Contain("ADR-0004-data-region-central-us.md", "AC4");
        adr0003.Should().Contain("ADR-0004-data-region-central-us.md", "AC4");
        adr0003.Should().Contain("the adequacy decision for Brazil is suspended, amended or repealed", "AC4: decision 3 stays");
        adr0003.Should().Contain("an EU representative (Art. 27)", "AC4: decision 4 stays");
        adr0003.Should().Contain("**Portugal comes after v1** (owner, 2026-10-04)", "AC4: decision 5 stays");
    }

    [Theory]
    [InlineData(AdrPath)]
    [InlineData(RegisterPath)]
    public void AdrAndRegister_OpenWithTheDraftNotice(string path)
    {
        var firstLines = string.Join('\n', Read(path).Split('\n').Take(12));

        firstLines.Should().Contain(DraftNotice, "AC7");
        firstLines.Should().Contain("not legal advice", "AC7");
    }
}
