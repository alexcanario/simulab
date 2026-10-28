using Simulab.Catalog.Contracts;
using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.QuestionBank.Contracts;

namespace Simulab.ExamEngine.Infrastructure;

/// <summary>
/// The adapter over the catalog's and the question bank's contracts (F-166): it is the one place that knows both, and turns
/// their types into the module's own. The question type is mapped by name over <see cref="Enum.GetValues{TEnum}()"/>.
/// </summary>
public sealed class AttemptSource(
    IExamEditionDirectory editions,
    IPublishedExamQueries exams,
    IPublishedQuestionDirectory questions) : IAttemptSource
{
    public async Task<IReadOnlyList<QuestionContent>> ListContentAsync(
        IReadOnlyCollection<AttemptVersion> versions,
        CancellationToken cancellationToken) =>
        [.. (await questions.ListVersionContentAsync(KeysOf(versions), cancellationToken))
            .Select(content => new QuestionContent(
                content.QuestionId,
                content.VersionNumber,
                content.ContentLanguage,
                content.StatementHtml,
                [.. content.Options.Select(option => new AttemptOptionResponse(option.Position, option.TextHtml))],
                content.BaseTextHtml,
                content.NoticeSubjectId))];

    public async Task<AttemptEditionResponse?> FindEditionLabelAsync(Guid examEditionId, CancellationToken cancellationToken)
    {
        var edition = await editions.FindAsync(examEditionId, cancellationToken);
        if (edition is null)
        {
            return null;
        }

        var exam = await exams.FindAsync(edition.ExamId, cancellationToken);

        return new AttemptEditionResponse(
            edition.ExamId,
            edition.ExamName,
            edition.OrganizerName,
            edition.OrganizerAcronym,
            edition.NoticeYear,
            edition.Position,
            exam?.ContentLanguage);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> FindNoticeSubjectLabelsAsync(
        IReadOnlyCollection<Guid> noticeSubjectIds,
        CancellationToken cancellationToken) =>
        noticeSubjectIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await editions.FindNoticeSubjectsAsync(noticeSubjectIds, cancellationToken))
                .ToDictionary(subject => subject.Id, subject => subject.Label);

    public async Task<AttemptImage?> OpenImageAsync(
        Guid imageId,
        IReadOnlyCollection<AttemptVersion> versions,
        CancellationToken cancellationToken)
    {
        var file = await questions.OpenVersionImageAsync(imageId, KeysOf(versions), cancellationToken);

        return file is null ? null : new AttemptImage(file.Content, file.ContentType);
    }

    private static List<QuestionVersionKey> KeysOf(IEnumerable<AttemptVersion> versions) =>
        [.. versions.Select(version => new QuestionVersionKey(version.QuestionId, version.VersionNumber))];

    public async Task<EditionState?> FindEditionAsync(Guid examEditionId, CancellationToken cancellationToken)
    {
        var edition = await editions.FindAsync(examEditionId, cancellationToken);

        return edition is null ? null : new EditionState(edition.Id, edition.Status == ExamEditionStatus.Published);
    }

    public async Task<IReadOnlyList<AttemptQuestion>> ListPublishedQuestionsAsync(Guid examEditionId, CancellationToken cancellationToken) =>
        [.. (await questions.ListPublishedByEditionAsync(examEditionId, cancellationToken))
            .Select(question => new AttemptQuestion(question.QuestionId, question.PaperNumber, question.VersionNumber, TypeOf(question.Type)))];

    public async Task<AnswerShape?> FindShapeAsync(Guid questionId, int versionNumber, CancellationToken cancellationToken)
    {
        var shape = await questions.FindVersionAsync(questionId, versionNumber, cancellationToken);

        return shape is null
            ? null
            : new AnswerShape(TypeOf(shape.Type), shape.OptionPositions, shape.MatchItemPositions, shape.BlankNumbers, shape.AnswerSizeMax);
    }

    private static AttemptQuestionType TypeOf(QuestionType type) =>
        Enum.GetValues<AttemptQuestionType>().Single(candidate => candidate.ToString() == type.ToString());
}
