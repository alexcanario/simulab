using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Tests;

/// <summary>F-36 BR4 and AC4: the entity keeps the comparable form of its scope detail on every save.</summary>
public class ExamNormalizedScopeDetailTests
{
    private static readonly Guid Authority = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData("Goiânia", "GOIANIA")]
    [InlineData("  São Paulo  ", "SAO PAULO")]
    [InlineData("Ceará", "CEARA")]
    public void Create_WithScopeDetail_StoresItWithoutCaseOrAccents(string detail, string expected)
    {
        var result = Exam.Create(Authority, "Guarda Municipal", AssessmentType.PublicServiceExam, ExamScope.Municipal, detail, "pt-BR");

        result.IsSuccess.Should().BeTrue();
        result.Value.NormalizedScopeDetail.Should().Be(expected);
    }

    [Fact]
    public void Create_NationalExam_HasAnEmptyNormalizedScopeDetail()
    {
        var result = Exam.Create(Authority, "ENEM", AssessmentType.Enem, ExamScope.National, "Brasil", "pt-BR");

        result.IsSuccess.Should().BeTrue();
        result.Value.ScopeDetail.Should().BeNull();
        result.Value.NormalizedScopeDetail.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithANewScopeDetail_RefreshesTheNormalizedForm()
    {
        var exam = Exam.Create(Authority, "Guarda Municipal", AssessmentType.PublicServiceExam, ExamScope.Municipal, "Goiânia", "pt-BR").Value;

        var result = exam.Update(Authority, "Guarda Municipal", AssessmentType.PublicServiceExam, ExamScope.State, "PA", "pt-BR");

        result.IsSuccess.Should().BeTrue();
        exam.NormalizedScopeDetail.Should().Be("PARA PA", "a State exam is found by its state's name and by its acronym (F-42 BR5)");
    }

    [Fact]
    public void Update_ToNational_ClearsTheNormalizedForm()
    {
        var exam = Exam.Create(Authority, "Guarda Municipal", AssessmentType.PublicServiceExam, ExamScope.Municipal, "Goiânia", "pt-BR").Value;

        exam.Update(Authority, "Guarda Municipal", AssessmentType.PublicServiceExam, ExamScope.National, null, "pt-BR");

        exam.NormalizedScopeDetail.Should().BeEmpty();
    }
}
