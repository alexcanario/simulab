using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>F-74 BR2 to BR4: the rules the notice subject owns, answered with a Result and never thrown.</summary>
public class NoticeSubjectTests
{
    private static readonly Guid Edition = Guid.CreateVersion7();

    [Fact]
    public void Create_ValidData_TrimsTheTextAndNormalizesBoth()
    {
        var result = NoticeSubject.Create(Edition, "  Básicos  ", "  Língua Portuguesa ", 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Group.Should().Be("Básicos");
        result.Value.NormalizedGroup.Should().Be("BASICOS");
        result.Value.Label.Should().Be("Língua Portuguesa");
        result.Value.NormalizedLabel.Should().Be("LINGUA PORTUGUESA");
        result.Value.QuestionCount.Should().Be(10);
        result.Value.ExamEditionId.Should().Be(Edition);
        result.Value.TenantId.Should().BeNull("catalog rows are global");
    }

    // AC4: a blank, a 1-character and a 201-character label each have their own code.
    [Theory]
    [InlineData(null, CatalogErrorCodes.NoticeSubjectLabelRequired)]
    [InlineData("", CatalogErrorCodes.NoticeSubjectLabelRequired)]
    [InlineData("   ", CatalogErrorCodes.NoticeSubjectLabelRequired)]
    [InlineData("A", CatalogErrorCodes.NoticeSubjectLabelTooShort)]
    public void Create_BlankOrTooShortLabel_FailsWithItsOwnCode(string? label, string code)
    {
        var act = () => NoticeSubject.Create(Edition, null, label, null);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(code);
    }

    [Fact]
    public void Create_LabelOver200Characters_FailsWithLabelTooLong()
    {
        var result = NoticeSubject.Create(Edition, null, new string('x', CatalogLimits.NoticeSubjectLabelMaxLength + 1), null);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectLabelTooLong);
    }

    [Fact]
    public void Create_LabelOf200Characters_Succeeds()
    {
        NoticeSubject.Create(Edition, null, new string('x', CatalogLimits.NoticeSubjectLabelMaxLength), null)
            .IsSuccess.Should().BeTrue();
    }

    // AC5: a 101-character group is refused; a blank one is stored as null, "no group".
    [Fact]
    public void Create_GroupOver100Characters_FailsWithGroupTooLong()
    {
        var result = NoticeSubject.Create(Edition, new string('g', CatalogLimits.NoticeSubjectGroupMaxLength + 1), "Português", null);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectGroupTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankGroup_IsStoredAsNoGroup(string? group)
    {
        var result = NoticeSubject.Create(Edition, group, "Português", null);

        result.Value.Group.Should().BeNull();
        result.Value.NormalizedGroup.Should().BeEmpty();
    }

    // AC6: 0, 501 and a negative number are refused; none is stored as null.
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(501)]
    public void Create_QuestionCountOutsideTheRange_FailsWithQuestionCountInvalid(int count)
    {
        var result = NoticeSubject.Create(Edition, null, "Português", count);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectQuestionCountInvalid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void Create_QuestionCountAtTheLimits_Succeeds(int count)
    {
        NoticeSubject.Create(Edition, null, "Português", count).Value.QuestionCount.Should().Be(count);
    }

    [Fact]
    public void Create_NoQuestionCount_IsStoredAsNull()
    {
        NoticeSubject.Create(Edition, null, "Português", null).Value.QuestionCount.Should().BeNull();
    }

    [Fact]
    public void Update_ChangesTheFieldsButNotTheEdition()
    {
        var subject = NoticeSubject.Create(Edition, "Básicos", "Português", 10).Value;

        var result = subject.Update("Específicos", "Direito", null);

        result.IsSuccess.Should().BeTrue();
        subject.Group.Should().Be("Específicos");
        subject.Label.Should().Be("Direito");
        subject.QuestionCount.Should().BeNull();
        subject.ExamEditionId.Should().Be(Edition);
    }

    [Fact]
    public void Update_InvalidData_FailsAndKeepsTheOldValues()
    {
        var subject = NoticeSubject.Create(Edition, "Básicos", "Português", 10).Value;

        var result = subject.Update("Específicos", "A", 20);

        result.IsFailure.Should().BeTrue();
        subject.Group.Should().Be("Básicos");
        subject.Label.Should().Be("Português");
        subject.QuestionCount.Should().Be(10);
    }
}
