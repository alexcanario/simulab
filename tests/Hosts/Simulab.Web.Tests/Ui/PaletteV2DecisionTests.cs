namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-148 AC12: the palette v2 decisions are recorded in ADR-0007, and ADR-0006 says its palette is superseded in part and
/// points to it, so a reader of the identity finds the current values.
/// </summary>
public sealed class PaletteV2DecisionTests
{
    private static readonly string Decisions = Path.Combine(FindRoot(), "docs", "decisions");

    [Fact]
    public void Adr0007_RecordsThePaletteV2Decisions()
    {
        var adr = File.ReadAllText(Path.Combine(Decisions, "ADR-0007-control-alt-palette-v2.md"));

        adr.Should().Contain("adr: 0007").And.Contain("status: accepted");
        adr.Should().Contain("#F5A020", "the amber action").And.Contain("#0F3266", "the navy primary").And.Contain("#1D64A8", "the link role");
        adr.Should().Contain("F-125").And.Contain("ADR-0006");
        adr.Should().Contain("## Context").And.Contain("## Decision").And.Contain("## Consequences");
    }

    [Fact]
    public void Adr0006_PointsToAdr0007()
    {
        var adr = File.ReadAllText(Path.Combine(Decisions, "ADR-0006-control-alt-identity.md"));

        adr.Should().Contain("ADR-0007").And.Contain("superseded", "its palette is superseded in part");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Simulab.slnx not found above the test output.");
    }
}
