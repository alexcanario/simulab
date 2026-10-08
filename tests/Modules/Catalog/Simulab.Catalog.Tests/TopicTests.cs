using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>F-79 BR6 and BR14: the rules the topic owns, answered with a Result and never thrown.</summary>
public class TopicTests
{
    [Fact]
    public void Create_ValidData_TrimsTheNameAndKeepsTheSubject()
    {
        var subject = Guid.CreateVersion7();

        var result = Topic.Create(subject, "  Crase  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Crase");
        result.Value.NormalizedName.Should().Be("CRASE");
        result.Value.SubjectId.Should().Be(subject);
        result.Value.TenantId.Should().BeNull("catalog rows are global");
    }

    // AC14: an empty name is a failed Result, nothing is thrown.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Create_BlankOrTooShortName_FailsWithNameRequired(string? name)
    {
        var act = () => Topic.Create(Guid.CreateVersion7(), name);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.TopicNameRequired);
    }

    [Fact]
    public void Create_NameOver200Characters_FailsWithNameTooLong()
    {
        Topic.Create(Guid.CreateVersion7(), new string('a', CatalogLimits.TopicNameMaxLength + 1))
            .Error!.Code.Should().Be(CatalogErrorCodes.TopicNameTooLong);
        Topic.Create(Guid.CreateVersion7(), new string('a', CatalogLimits.TopicNameMaxLength))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Update_MovesToAnotherSubject()
    {
        var topic = Topic.Create(Guid.CreateVersion7(), "Crase").Value;
        var other = Guid.CreateVersion7();

        topic.Update(other, "Crase").IsSuccess.Should().BeTrue();

        topic.SubjectId.Should().Be(other);
    }
}
