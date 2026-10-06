using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>F-79 BR3, BR4 and BR14: the rules the subject and the topic own, answered with a Result and never thrown.</summary>
public class SubjectTests
{
    [Fact]
    public void Create_ValidData_TrimsTheNameAndNormalizesIt()
    {
        var area = Guid.CreateVersion7();

        var result = Subject.Create("  Direito Constitucional  ", area);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Direito Constitucional");
        result.Value.NormalizedName.Should().Be("DIREITO CONSTITUCIONAL");
        result.Value.AreaId.Should().Be(area);
        result.Value.TenantId.Should().BeNull("catalog rows are global");
    }

    [Fact]
    public void Create_WithoutArea_IsAccepted()
    {
        Subject.Create("Português", null).Value.AreaId.Should().BeNull();
    }

    // AC14: an empty name is a failed Result, nothing is thrown.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Create_BlankOrTooShortName_FailsWithNameRequired(string? name)
    {
        var act = () => Subject.Create(name, null);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.SubjectNameRequired);
    }

    [Fact]
    public void Create_NameOver150Characters_FailsWithNameTooLong()
    {
        Subject.Create(new string('a', CatalogLimits.SubjectNameMaxLength + 1), null)
            .Error!.Code.Should().Be(CatalogErrorCodes.SubjectNameTooLong);
        Subject.Create(new string('a', CatalogLimits.SubjectNameMaxLength), null)
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Update_ChangesNameAndArea_AndFailsWithoutChangingAnything()
    {
        var subject = Subject.Create("Português", null).Value;
        var area = Guid.CreateVersion7();

        subject.Update("Língua Portuguesa", area).IsSuccess.Should().BeTrue();
        var failed = subject.Update("x", null);

        failed.Error!.Code.Should().Be(CatalogErrorCodes.SubjectNameRequired);
        subject.Name.Should().Be("Língua Portuguesa");
        subject.AreaId.Should().Be(area);
    }
}
