using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>F-89 BR2 to BR4: the rules of the notice PDF that the edition owns, answered with a Result and never thrown.</summary>
public sealed class ExamEditionNoticeDocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static ExamEdition NewEdition() =>
        ExamEdition.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            2026,
            null,
            null,
            null,
            null,
            ExamEditionStatus.Draft,
            2027).Value;

    [Fact]
    public void Attach_ToAnEditionWithoutAFile_KeepsTheFileAndReplacesNothing()
    {
        var edition = NewEdition();

        var replaced = edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", "edital.pdf", 1234, Now);

        replaced.IsSuccess.Should().BeTrue();
        replaced.Value.Should().BeNull();
        edition.NoticeDocument.Should().Be(new NoticeDocument("exam-editions/a/notice/1.pdf", "edital.pdf", 1234, Now));
    }

    // BR2: the new file takes the place of the old one, and the caller learns which blob to delete.
    [Fact]
    public void Attach_ToAnEditionWithAFile_ReturnsTheBlobNameItReplaced()
    {
        var edition = NewEdition();
        edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", "old.pdf", 10, Now);

        var replaced = edition.AttachNoticeDocument("exam-editions/a/notice/2.pdf", "new.pdf", 20, Now.AddDays(1));

        replaced.Value.Should().Be("exam-editions/a/notice/1.pdf");
        edition.NoticeDocument!.BlobName.Should().Be("exam-editions/a/notice/2.pdf");
        edition.NoticeDocument.FileName.Should().Be("new.pdf");
    }

    [Theory]
    [InlineData(@"C:\Users\ana\Downloads\Edital 01-2026.pdf", "Edital 01-2026.pdf")]
    [InlineData("/home/ana/edital.pdf", "edital.pdf")]
    [InlineData("  edital.pdf  ", "edital.pdf")]
    public void Attach_KeepsOnlyTheFileNameWithoutAnyFolder(string typed, string kept)
    {
        var edition = NewEdition();

        edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", typed, 10, Now);

        edition.NoticeDocument!.FileName.Should().Be(kept);
    }

    [Fact]
    public void Attach_ALongFileName_IsCutToTheColumnWidth()
    {
        var edition = NewEdition();

        edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", new string('a', 300) + ".pdf", 10, Now);

        edition.NoticeDocument!.FileName.Should().HaveLength(CatalogLimits.ExamEditionNoticeDocumentFileNameMaxLength);
    }

    // BR4: exactly 20 MB is accepted, one byte more is not, and a refusal leaves the edition as it was.
    [Fact]
    public void Attach_ExactlyTwentyMegabytes_IsAccepted()
    {
        var edition = NewEdition();

        var result = edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", "a.pdf", 20L * 1024 * 1024, Now);

        result.IsSuccess.Should().BeTrue();
        CatalogLimits.ExamEditionNoticeDocumentMaxBytes.Should().Be(20L * 1024 * 1024);
    }

    [Fact]
    public void Attach_OneByteOverTwentyMegabytes_FailsWithTooLargeAndKeepsTheOldFile()
    {
        var edition = NewEdition();
        edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", "old.pdf", 10, Now);

        var result = edition.AttachNoticeDocument("exam-editions/a/notice/2.pdf", "big.pdf", (20L * 1024 * 1024) + 1, Now);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentTooLarge);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
        edition.NoticeDocument!.FileName.Should().Be("old.pdf");
    }

    [Fact]
    public void Attach_AnEmptyFile_FailsWithNotPdf()
    {
        var edition = NewEdition();

        var result = edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", "empty.pdf", 0, Now);

        result.Error!.Code.Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf);
        edition.NoticeDocument.Should().BeNull();
    }

    [Fact]
    public void Remove_ReturnsTheBlobNameAndLeavesNoFile()
    {
        var edition = NewEdition();
        edition.AttachNoticeDocument("exam-editions/a/notice/1.pdf", "a.pdf", 10, Now);

        var removed = edition.RemoveNoticeDocument();

        removed.Should().Be("exam-editions/a/notice/1.pdf");
        edition.NoticeDocument.Should().BeNull();
    }

    [Fact]
    public void Remove_WithoutAFile_ReturnsNullAndThrowsNothing()
    {
        var edition = NewEdition();

        var act = edition.RemoveNoticeDocument;

        act.Should().NotThrow().Which.Should().BeNull();
    }
}
