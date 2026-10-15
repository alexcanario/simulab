using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-77 BR4 (review finding): replacing a stored set by a requested one is a diff by normalized form, shared by the
/// subject and the topic: a name that stays keeps its row, and a name is never removed and added in one save.
/// </summary>
public class AliasRulesDiffTests
{
    private static SubjectAlias Stored(string name) => SubjectAlias.Create(Guid.CreateVersion7(), name).Value;

    [Fact]
    public void Diff_SplitsTheSetIntoRemovedRenamedAndAdded()
    {
        var gramatica = Stored("Gramática");
        var portugues = Stored("Portugues");
        var wanted = AliasRules.Distinct(["PORTUGUÊS", "Crase"]);

        var change = AliasRules.Diff([gramatica, portugues], wanted);

        change.Remove.Should().Equal(gramatica);
        change.Rename.Should().ContainSingle().Which.Should().Be((portugues, "PORTUGUÊS"));
        change.Add.Select(text => text.Name).Should().Equal("Crase");
    }

    [Fact]
    public void Diff_SameSet_ChangesNothing()
    {
        var change = AliasRules.Diff([Stored("Crase")], AliasRules.Distinct(["Crase"]));

        change.Remove.Should().BeEmpty();
        change.Rename.Should().BeEmpty();
        change.Add.Should().BeEmpty();
    }

    [Fact]
    public void Diff_EmptyRequest_RemovesEverything_AndEmptyStoreAddsEverything()
    {
        var crase = Stored("Crase");

        AliasRules.Diff([crase], []).Remove.Should().Equal(crase);
        AliasRules.Diff(Array.Empty<SubjectAlias>(), AliasRules.Distinct(["Crase"])).Add.Should().ContainSingle();
    }
}
