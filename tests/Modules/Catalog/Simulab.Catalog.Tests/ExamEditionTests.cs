using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>F-35 BR3 to BR11 and BR15: the rules the entity owns, answered with a Result and never thrown.</summary>
public class ExamEditionTests
{
    private const int MaxYear = 2027;
    private static readonly Guid Exam = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Board = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Result<ExamEdition> Create(
        Guid? organizerId = null,
        int? noticeYear = 2026,
        string? position = null,
        string? noticeReference = null,
        string? noticeUrl = null,
        DateOnly? appliedOn = null,
        ExamEditionStatus status = ExamEditionStatus.Draft) =>
        ExamEdition.Create(Exam, organizerId ?? Board, noticeYear, position, noticeReference, noticeUrl, appliedOn, status, MaxYear);

    // AC18: a notice year below 1990 fails with its code instead of throwing.
    [Fact]
    public void Create_NoticeYear1989_FailsWithNoticeYearInvalidAndThrowsNothing()
    {
        var act = () => Create(noticeYear: 1989);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionNoticeYearInvalid);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    // AC6: the ceiling is the year passed in, which the handler takes from the clock plus one.
    [Theory]
    [InlineData(null)]
    [InlineData(1989)]
    [InlineData(MaxYear + 1)]
    public void Create_NoticeYearOutsideTheRange_FailsWithNoticeYearInvalid(int? year)
    {
        var result = Create(noticeYear: year);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionNoticeYearInvalid);
    }

    [Theory]
    [InlineData(1990)]
    [InlineData(MaxYear)]
    public void Create_NoticeYearAtTheEdges_IsAccepted(int year)
    {
        Create(noticeYear: year).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_WithoutABoard_FailsWithOrganizerRequired(string? organizer)
    {
        var result = ExamEdition.Create(
            Exam,
            organizer is null ? null : Guid.Parse(organizer),
            2026,
            null,
            null,
            null,
            null,
            ExamEditionStatus.Draft,
            MaxYear);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionOrganizerRequired);
    }

    [Fact]
    public void Create_ValidData_KeepsTheExamTheBoardAndTheCatalogGlobal()
    {
        var result = Create();

        result.IsSuccess.Should().BeTrue();
        result.Value.ExamId.Should().Be(Exam);
        result.Value.OrganizerId.Should().Be(Board);
        result.Value.Status.Should().Be(ExamEditionStatus.Draft);
        result.Value.TenantId.Should().BeNull("catalog rows are global in v1 (BR1)");
    }

    // BR5 and BR10: the position is trimmed, blank is stored as null, and the normalized form ignores case and accents.
    [Fact]
    public void Create_Position_IsTrimmedAndNormalized()
    {
        var edition = Create(position: "  Guarda Municipal de 3ª Classe  ").Value;

        edition.Position.Should().Be("Guarda Municipal de 3ª Classe");
        edition.NormalizedPosition.Should().Be(Create(position: "GUARDA MUNICIPAL DE 3ª CLASSE").Value.NormalizedPosition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankPosition_IsNullAndNormalizesToEmpty(string? position)
    {
        var edition = Create(position: position).Value;

        edition.Position.Should().BeNull();
        edition.NormalizedPosition.Should().BeEmpty("no position collides with no position (BR10)");
    }

    [Fact]
    public void Create_PositionOf201Characters_FailsWithPositionTooLong()
    {
        Create(position: new string('a', 201)).Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionPositionTooLong);
        Create(position: new string('a', 200)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_NoticeReferenceOf101Characters_FailsWithNoticeReferenceTooLong()
    {
        Create(noticeReference: new string('a', 101)).Error!.Code
            .Should().Be(CatalogErrorCodes.ExamEditionNoticeReferenceTooLong);

        var kept = Create(noticeReference: "  Edital nº 01/2026  ").Value;
        kept.NoticeReference.Should().Be("Edital nº 01/2026");
    }

    // AC7: only an absolute http or https address of at most 300 characters is a notice link.
    [Theory]
    [InlineData("edital.pdf")]
    [InlineData("/editais/2026")]
    [InlineData("mailto:banca@example.org")]
    [InlineData("ftp://example.org/edital.pdf")]
    public void Create_NoticeUrlThatIsNotAnAbsoluteWebAddress_FailsWithNoticeUrlInvalid(string url)
    {
        Create(noticeUrl: url).Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionNoticeUrlInvalid);
    }

    [Fact]
    public void Create_NoticeUrlLongerThan300Characters_FailsWithNoticeUrlInvalid()
    {
        var url = "https://example.org/" + new string('a', 290);

        Create(noticeUrl: url).Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionNoticeUrlInvalid);
    }

    [Theory]
    [InlineData("https://example.org/edital.pdf")]
    [InlineData("http://example.org/edital")]
    public void Create_NoticeUrlThatIsAWebAddress_IsAccepted(string url)
    {
        Create(noticeUrl: url).Value.NoticeUrl.Should().Be(url);
    }

    // AC7 and BR8: the application date is optional, and never before 1 January of the notice year.
    [Fact]
    public void Create_AppliedOnBeforeTheNoticeYear_FailsWithAppliedOnBeforeNoticeYear()
    {
        var result = Create(noticeYear: 2026, appliedOn: new DateOnly(2025, 12, 31));

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionAppliedOnBeforeNoticeYear);
    }

    [Fact]
    public void Create_AppliedOnOnTheFirstDayOfTheNoticeYear_IsAccepted()
    {
        Create(noticeYear: 2026, appliedOn: new DateOnly(2026, 1, 1)).Value.AppliedOn
            .Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void Create_WithoutAppliedOn_IsAcceptedAlsoWhenPublished()
    {
        var result = Create(status: ExamEditionStatus.Published);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedOn.Should().BeNull();
    }

    // AC15: a status outside the enum is refused by the entity as well.
    [Fact]
    public void Create_StatusThatIsNotDefined_FailsWithStatusInvalid()
    {
        Create(status: (ExamEditionStatus)99).Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionStatusInvalid);
    }

    // BR2: the exam never changes; everything else does, in both directions of the status (BR9, AC11).
    [Fact]
    public void Update_ChangesTheFieldsAndTheStatusBothWaysButNeverTheExam()
    {
        var edition = Create().Value;
        var otherBoard = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var published = edition.Update(otherBoard, 2025, "Agente", "Edital 1", null, null, ExamEditionStatus.Published, MaxYear);

        published.IsSuccess.Should().BeTrue();
        edition.ExamId.Should().Be(Exam);
        edition.OrganizerId.Should().Be(otherBoard);
        edition.NoticeYear.Should().Be(2025);
        edition.Status.Should().Be(ExamEditionStatus.Published);

        edition.Update(otherBoard, 2025, "Agente", "Edital 1", null, null, ExamEditionStatus.Draft, MaxYear);
        edition.Status.Should().Be(ExamEditionStatus.Draft);
    }

    [Fact]
    public void Update_WithInvalidData_FailsAndKeepsWhatWasStored()
    {
        var edition = Create(noticeYear: 2026, position: "Agente").Value;

        var result = edition.Update(Board, 1800, "Outro", null, null, null, ExamEditionStatus.Draft, MaxYear);

        result.IsFailure.Should().BeTrue();
        edition.NoticeYear.Should().Be(2026);
        edition.Position.Should().Be("Agente");
    }

    // BR11: a published edition cannot be deleted; a draft can.
    [Fact]
    public void CanBeDeleted_PublishedEdition_FailsWithPublishedConflict()
    {
        var result = Create(status: ExamEditionStatus.Published).Value.CanBeDeleted();

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionPublished);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public void CanBeDeleted_DraftEdition_Succeeds()
    {
        Create().Value.CanBeDeleted().IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("Draft", ExamEditionStatus.Draft)]
    [InlineData("published", ExamEditionStatus.Published)]
    [InlineData("PUBLISHED", ExamEditionStatus.Published)]
    public void ParseStatus_KnownNames_IgnoreCase(string value, ExamEditionStatus expected)
    {
        new SaveExamEditionRequest(Status: value).ParseStatus().Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("InReview")]
    [InlineData("99")]
    public void ParseStatus_BlankOrUnknown_IsNull(string? value)
    {
        new SaveExamEditionRequest(Status: value).ParseStatus().Should().BeNull();
    }
}
