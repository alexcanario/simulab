using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>F-34 BR8, BR9, BR10, BR15: the rules the entity owns, answered with a Result and never thrown.</summary>
public class ExamTests
{
    private static readonly Guid Authority = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Result<Exam> Create(
        Guid? issuingAuthorityId = null,
        string? name = "Agente de Policia Federal",
        AssessmentType assessmentType = AssessmentType.PublicServiceExam,
        ExamScope scope = ExamScope.National,
        string? scopeDetail = null,
        string? contentLanguage = "pt-BR") =>
        Exam.Create(issuingAuthorityId ?? Authority, name, assessmentType, scope, scopeDetail, contentLanguage);

    [Fact]
    public void Create_ValidData_TrimsTheNameAndKeepsTheCatalogGlobal()
    {
        var result = Create(name: "  Agente de Policia Federal  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Agente de Policia Federal");
        result.Value.IssuingAuthorityId.Should().Be(Authority);
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.TenantId.Should().BeNull("catalog rows are global in v1 (BR1)");
    }

    // AC19: a blank name fails with its code instead of throwing.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Create_BlankOrTooShortName_FailsWithNameRequired(string? name)
    {
        var result = Create(name: name);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamNameRequired);
    }

    [Fact]
    public void Create_NameLongerThanTheColumn_FailsWithNameTooLong()
    {
        var result = Create(name: new string('a', CatalogLimits.ExamNameMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamNameTooLong);
    }

    [Fact]
    public void Create_NameExactlyTheColumnWidth_IsAccepted()
    {
        var result = Create(name: new string('a', CatalogLimits.ExamNameMaxLength));

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    public void Create_WithoutAnIssuingAuthority_FailsWithItsOwnCode(Guid? authority)
    {
        var result = Exam.Create(authority, "Agente", AssessmentType.PublicServiceExam, ExamScope.National, null, "pt-BR");

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamIssuingAuthorityRequired);
    }

    [Fact]
    public void Create_WithAnEmptyIssuingAuthority_FailsWithItsOwnCode()
    {
        var result = Create(issuingAuthorityId: Guid.Empty);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamIssuingAuthorityRequired);
    }

    [Fact]
    public void Create_AssessmentTypeThatIsNotOneOfTheFour_FailsWithAssessmentTypeInvalid()
    {
        var result = Create(assessmentType: (AssessmentType)99);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamAssessmentTypeInvalid);
    }

    [Fact]
    public void Create_ScopeThatIsNotOneOfTheThree_FailsWithScopeInvalid()
    {
        var result = Create(scope: (ExamScope)99);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamScopeInvalid);
    }

    // BR8: the two scopes that need a place must say which one.
    [Theory]
    [InlineData(ExamScope.State, null)]
    [InlineData(ExamScope.State, "   ")]
    [InlineData(ExamScope.Municipal, null)]
    [InlineData(ExamScope.Municipal, "")]
    public void Create_StateOrMunicipalWithoutItsDetail_FailsWithScopeDetailRequired(ExamScope scope, string? detail)
    {
        var result = Create(scope: scope, scopeDetail: detail);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamScopeDetailRequired);
    }

    [Fact]
    public void Create_ScopeDetailLongerThanTheColumn_FailsWithScopeDetailTooLong()
    {
        var result = Create(
            scope: ExamScope.Municipal,
            scopeDetail: new string('a', CatalogLimits.ExamScopeDetailMaxLength + 1));

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamScopeDetailTooLong);
    }

    [Fact]
    public void Create_MunicipalWithItsDetail_TrimsIt()
    {
        var result = Create(scope: ExamScope.Municipal, scopeDetail: "  Guarulhos (SP)  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.ScopeDetail.Should().Be("Guarulhos (SP)");
    }

    // BR8: a national exam has no detail, whatever the request sent.
    [Fact]
    public void Create_NationalWithADetail_DropsIt()
    {
        var result = Create(scope: ExamScope.National, scopeDetail: "Sao Paulo");

        result.IsSuccess.Should().BeTrue();
        result.Value.ScopeDetail.Should().BeNull();
    }

    // AC11, BR9: the stored language is the canonical form of the app's list.
    [Theory]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("PT-BR", "pt-BR")]
    [InlineData(" pt-PT ", "pt-PT")]
    [InlineData("EN", "en")]
    public void Create_ALanguageOfTheApp_StoresItCanonically(string given, string stored)
    {
        var result = Create(contentLanguage: given);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentLanguage.Should().Be(stored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("es")]
    [InlineData("pt")]
    [InlineData("portugues")]
    public void Create_ALanguageTheAppDoesNotShipIn_FailsWithContentLanguageInvalid(string? language)
    {
        var result = Create(contentLanguage: language);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamContentLanguageInvalid);
    }

    // BR10: the comparable form is what the unique index and the search read.
    [Fact]
    public void Create_StoresTheNormalizedNameTheIndexReads()
    {
        var result = Create(name: "Guarda Municipal de Guarulhos");

        result.Value.NormalizedName.Should().Be(CatalogText.Normalize("GUARDA MUNICIPAL DE GUARULHOS"));
    }

    [Fact]
    public void Update_NewValues_ReplacesThemAndTheNormalizedName()
    {
        var exam = Create().Value;
        var other = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = exam.Update(other, "FUVEST", AssessmentType.UniversityEntranceExam, ExamScope.State, "SP", "pt-BR");

        result.IsSuccess.Should().BeTrue();
        exam.IssuingAuthorityId.Should().Be(other);
        exam.Name.Should().Be("FUVEST");
        exam.AssessmentType.Should().Be(AssessmentType.UniversityEntranceExam);
        exam.Scope.Should().Be(ExamScope.State);
        exam.ScopeDetail.Should().Be("SP");
        exam.NormalizedName.Should().Be(CatalogText.Normalize("FUVEST"));
    }

    // F-42 BR1, BR2, AC3, AC5: a State exam names its state by acronym.
    [Theory]
    [InlineData("SP")]
    [InlineData("sp")]
    [InlineData("  Sp  ")]
    public void Create_StateWithAnAcronymOfTheList_StoresItInUpperCaseAndSearchesByNameAndAcronym(string detail)
    {
        var result = Create(scope: ExamScope.State, scopeDetail: detail);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScopeDetail.Should().Be("SP");
        result.Value.NormalizedScopeDetail.Should().Be("SAO PAULO SP");
    }

    [Theory]
    [InlineData("Sampa")]
    [InlineData("São Paulo")]
    [InlineData("SPP")]
    [InlineData("XX")]
    public void Create_StateWithTextOffTheList_FailsWithUnknownState(string detail)
    {
        var result = Create(scope: ExamScope.State, scopeDetail: detail);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamScopeDetailUnknownState);
    }

    [Fact]
    public void Update_StateWithTextOffTheList_Fails_AndKeepsTheStateItHad()
    {
        var exam = Create(scope: ExamScope.State, scopeDetail: "SP").Value;

        var result = exam.Update(Authority, exam.Name, exam.AssessmentType, ExamScope.State, "Sampa", "pt-BR");

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamScopeDetailUnknownState);
        exam.ScopeDetail.Should().Be("SP");
        exam.NormalizedScopeDetail.Should().Be("SAO PAULO SP");
    }

    // F-42 AC4 (BR3): the same text a State exam refuses is fine for a Municipal one.
    [Fact]
    public void Create_MunicipalWithFreeText_KeepsItAsTyped()
    {
        var result = Create(scope: ExamScope.Municipal, scopeDetail: "Sampa");

        result.IsSuccess.Should().BeTrue();
        result.Value.ScopeDetail.Should().Be("Sampa");
        result.Value.NormalizedScopeDetail.Should().Be("SAMPA");
    }

    // BR15: a refused update leaves the entity as it was.
    [Fact]
    public void Update_Refused_ChangesNothing()
    {
        var exam = Create(name: "Agente de Policia Federal").Value;

        var result = exam.Update(Authority, "A", AssessmentType.PublicServiceExam, ExamScope.National, null, "pt-BR");

        result.IsFailure.Should().BeTrue();
        exam.Name.Should().Be("Agente de Policia Federal");
    }
}
