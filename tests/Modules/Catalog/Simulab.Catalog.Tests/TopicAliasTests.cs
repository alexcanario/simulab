using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>F-77 BR1, BR2 and BR12 for a topic's other names: 2 to 200 characters, a Result and never an exception.</summary>
public class TopicAliasTests
{
    [Fact]
    public void Create_ValidName_TrimsItAndNormalizesIt()
    {
        var topic = Guid.CreateVersion7();

        var result = TopicAlias.Create(topic, "  Compreensão ");

        result.IsSuccess.Should().BeTrue();
        result.Value.TopicId.Should().Be(topic);
        result.Value.Name.Should().Be("Compreensão");
        result.Value.NormalizedName.Should().Be("COMPREENSAO");
        result.Value.TenantId.Should().BeNull("catalog rows are global");
    }

    // AC14: an empty text is a failed Result with the too-short code, and nothing throws.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_FailsWithTooShort_AndNothingThrows(string? name)
    {
        var act = () => TopicAlias.Create(Guid.CreateVersion7(), name);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.TopicAliasTooShort);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    // AC3: 201 characters is too long for a topic; 200 passes.
    [Fact]
    public void Create_LengthEdges_AreTwoToTwoHundred()
    {
        var topic = Guid.CreateVersion7();

        TopicAlias.Create(topic, "a").Error!.Code.Should().Be(CatalogErrorCodes.TopicAliasTooShort);
        TopicAlias.Create(topic, new string('a', CatalogLimits.TopicNameMaxLength + 1))
            .Error!.Code.Should().Be(CatalogErrorCodes.TopicAliasTooLong);
        TopicAlias.Create(topic, new string('a', CatalogLimits.TopicNameMaxLength)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Rename_ChangesTheTypedFormOnly_AndFailsWithoutChangingAnything()
    {
        var alias = TopicAlias.Create(Guid.CreateVersion7(), "Compreensao").Value;

        alias.Rename("Compreensão").IsSuccess.Should().BeTrue();
        var failed = alias.Rename(" ");

        alias.Name.Should().Be("Compreensão");
        alias.NormalizedName.Should().Be("COMPREENSAO");
        failed.Error!.Code.Should().Be(CatalogErrorCodes.TopicAliasTooShort);
    }
}
