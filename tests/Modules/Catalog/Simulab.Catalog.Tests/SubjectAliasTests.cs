using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>F-77 BR1, BR2, BR4, BR5 and BR12: the rules an alias owns, answered with a Result and never thrown.</summary>
public class SubjectAliasTests
{
    [Fact]
    public void Create_ValidName_TrimsItAndNormalizesIt()
    {
        var subject = Guid.CreateVersion7();

        var result = SubjectAlias.Create(subject, "  Português ");

        result.IsSuccess.Should().BeTrue();
        result.Value.SubjectId.Should().Be(subject);
        result.Value.Name.Should().Be("Português");
        result.Value.NormalizedName.Should().Be("PORTUGUES");
        result.Value.TenantId.Should().BeNull("catalog rows are global");
    }

    // AC14: an empty text is a failed Result with the too-short code, and nothing throws.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_FailsWithTooShort_AndNothingThrows(string? name)
    {
        var act = () => SubjectAlias.Create(Guid.CreateVersion7(), name);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.SubjectAliasTooShort);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    // AC3: 1 character is too short, 151 too long; 2 and 150 are the edges that pass.
    [Fact]
    public void Create_LengthEdges_AreTwoToOneHundredFifty()
    {
        var subject = Guid.CreateVersion7();

        SubjectAlias.Create(subject, "a").Error!.Code.Should().Be(CatalogErrorCodes.SubjectAliasTooShort);
        SubjectAlias.Create(subject, new string('a', CatalogLimits.SubjectNameMaxLength + 1))
            .Error!.Code.Should().Be(CatalogErrorCodes.SubjectAliasTooLong);
        SubjectAlias.Create(subject, "ab").IsSuccess.Should().BeTrue();
        SubjectAlias.Create(subject, new string('a', CatalogLimits.SubjectNameMaxLength)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Rename_ChangesTheTypedFormOnly_AndFailsWithoutChangingAnything()
    {
        var alias = SubjectAlias.Create(Guid.CreateVersion7(), "Portugues").Value;

        alias.Rename("Português").IsSuccess.Should().BeTrue();
        var failed = alias.Rename("x");

        alias.Name.Should().Be("Português");
        alias.NormalizedName.Should().Be("PORTUGUES");
        failed.IsFailure.Should().BeTrue();
        failed.Error!.Code.Should().Be(CatalogErrorCodes.SubjectAliasTooShort);
    }

    // AC2 (BR4): a name typed twice with the same normalized form counts once, the first as typed kept.
    [Fact]
    public void Distinct_CollapsesTheSameNormalizedForm_KeepingTheFirstAsTyped()
    {
        var result = AliasRules.Distinct([" Português", "Portugues", "Gramática", "PORTUGUÊS"]);

        result.Select(alias => alias.Name).Should().Equal("Português", "Gramática");
        result.Select(alias => alias.NormalizedName).Should().Equal("PORTUGUES", "GRAMATICA");
    }

    [Fact]
    public void Distinct_NullIsEmpty_AndABlankEntryStaysForTheEntityToRefuse()
    {
        AliasRules.Distinct(null).Should().BeEmpty();
        AliasRules.Distinct(["  ", "Crase"]).Select(alias => alias.Name).Should().Equal(string.Empty, "Crase");
    }

    // AC4 (BR5): equal to the owner's own name once normalized.
    [Fact]
    public void IsSameAsName_ComparesTheNormalizedForms()
    {
        var alias = AliasRules.Distinct(["lingua portuguesa"]).Single();

        AliasRules.IsSameAsName(alias, "LINGUA PORTUGUESA").Should().BeTrue();
        AliasRules.IsSameAsName(alias, "PORTUGUES").Should().BeFalse();
    }
}
