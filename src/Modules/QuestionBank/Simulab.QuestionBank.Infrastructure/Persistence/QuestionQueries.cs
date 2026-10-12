using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question reads (F-95): the list, filtered by status and by the normalized statement so it ignores case
/// and accents, newest first; and one question with its options. Topic and subject names come from the catalog
/// through <see cref="ITopicDirectory"/>, one call per page, never a join across schemas. The origin of a
/// question (F-96) is read from the edition the same way and never copied onto the question (BR4).
/// </summary>
public sealed class QuestionQueries(
    QuestionBankModuleDbContext context,
    ITopicDirectory topics,
    ISubjectDirectory subjects,
    IExamEditionDirectory editions) : IQuestionQueries
{
    /// <summary>The most editions or subjects one lookup returns (F-96 BR9, B-28 BR1).</summary>
    private const int LookupMaxTake = 20;

    public async Task<QuestionPageResponse> ListAsync(QuestionListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var questions = context.Questions.AsNoTracking();

        if (sanitized.Status is { } status)
        {
            questions = questions.Where(question => question.Status == status);
        }

        var search = ComparableText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            questions = questions.Where(question => question.NormalizedStatement.Contains(search));
        }

        var total = await questions.CountAsync(cancellationToken);

        var rows = await questions
            .OrderByDescending(question => question.CreatedAt)
            .ThenByDescending(question => question.Id)
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .Select(question => new
            {
                question.Id,
                question.StatementText,
                question.TopicId,
                question.Difficulty,
                question.ContentLanguage,
                question.Status,
                question.ExamEditionId,
                question.PaperNumber,
                question.NoticeSubjectId
            })
            .ToListAsync(cancellationToken);

        var names = (await topics.FindManyAsync([.. rows.Select(row => row.TopicId).Distinct()], cancellationToken))
            .ToDictionary(entry => entry.Id);
        var origins = await OriginsAsync(
            rows.Select(row => (row.ExamEditionId, row.NoticeSubjectId)),
            cancellationToken);

        var items = rows
            .Select(row =>
            {
                names.TryGetValue(row.TopicId, out var topic);

                return new QuestionListItemResponse(
                    row.Id,
                    Excerpt(row.StatementText),
                    row.TopicId,
                    topic?.Name ?? string.Empty,
                    topic?.SubjectId ?? Guid.Empty,
                    topic?.SubjectName ?? string.Empty,
                    row.Difficulty,
                    row.ContentLanguage,
                    row.Status,
                    origins.Build(row.ExamEditionId, row.PaperNumber, row.NoticeSubjectId));
            })
            .ToList();

        return new QuestionPageResponse(items, total);
    }

    public async Task<QuestionResponse?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await context.Questions
            .AsNoTracking()
            .Include(row => row.Options)
            .FirstOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        var topic = await topics.FindAsync(question.TopicId, cancellationToken);
        var origins = await OriginsAsync([(question.ExamEditionId, question.NoticeSubjectId)], cancellationToken);

        return new QuestionResponse(
            question.Id,
            question.Type,
            question.Status,
            question.TopicId,
            topic?.Name ?? string.Empty,
            topic?.SubjectId ?? Guid.Empty,
            topic?.SubjectName ?? string.Empty,
            question.ContentLanguage,
            question.StatementHtml,
            question.BaseTextHtml,
            question.ExplanationHtml,
            question.Difficulty,
            [
                .. question.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new QuestionOptionResponse(option.Position, option.TextHtml, option.IsCorrect))
            ],
            origins.Build(question.ExamEditionId, question.PaperNumber, question.NoticeSubjectId));
    }

    public async Task<IReadOnlyList<ExamEditionLookupResponse>> SearchEditionsAsync(
        string? search,
        int take,
        CancellationToken cancellationToken) =>
        [
            .. (await editions.SearchAsync(search, Math.Clamp(take, 1, LookupMaxTake), cancellationToken))
                .Select(ToLookup)
        ];

    public async Task<IReadOnlyList<SubjectLookupResponse>> SearchSubjectsAsync(
        string? search,
        int take,
        CancellationToken cancellationToken) =>
        [
            .. (await subjects.SearchAsync(search, Math.Clamp(take, 1, LookupMaxTake), cancellationToken))
                .Select(subject => new SubjectLookupResponse(subject.Id, subject.Name))
        ];

    public async Task<IReadOnlyList<TopicLookupResponse>?> ListSubjectTopicsAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        var found = await subjects.ListTopicsAsync(subjectId, cancellationToken);

        return found is null
            ? null
            : [.. found.Select(topic => new TopicLookupResponse(topic.Id, topic.Name))];
    }

    public async Task<ExamEditionLookupResponse?> FindEditionAsync(
        Guid examEditionId,
        CancellationToken cancellationToken)
    {
        var edition = await editions.FindAsync(examEditionId, cancellationToken);

        return edition is null ? null : ToLookup(edition);
    }

    public async Task<IReadOnlyList<NoticeSubjectLookupResponse>?> ListNoticeSubjectsAsync(
        Guid examEditionId,
        CancellationToken cancellationToken)
    {
        if (await editions.FindAsync(examEditionId, cancellationToken) is null)
        {
            return null;
        }

        return
        [
            .. (await editions.ListNoticeSubjectsAsync(examEditionId, cancellationToken))
                .Select(subject => new NoticeSubjectLookupResponse(
                    subject.Id, subject.Group, subject.Label, subject.QuestionCount))
        ];
    }

    public async Task<EditionQuestionPageResponse?> ListEditionQuestionsAsync(
        Guid examEditionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (await editions.FindAsync(examEditionId, cancellationToken) is null)
        {
            return null;
        }

        var size = Math.Clamp(pageSize, 1, QuestionListQuery.MaxPageSize);
        var questions = context.Questions.AsNoTracking().Where(question => question.ExamEditionId == examEditionId);

        var total = await questions.CountAsync(cancellationToken);

        var rows = await questions
            .OrderBy(question => question.PaperNumber)
            .ThenBy(question => question.Id)
            .Skip(Math.Max(page, 0) * size)
            .Take(size)
            .Select(question => new
            {
                question.Id,
                question.PaperNumber,
                question.StatementText,
                question.NoticeSubjectId,
                question.Status,
                question.ContentLanguage
            })
            .ToListAsync(cancellationToken);

        var subjectIds = rows
            .Where(row => row.NoticeSubjectId is not null)
            .Select(row => row.NoticeSubjectId!.Value)
            .Distinct()
            .ToList();
        var labels = subjectIds.Count == 0
            ? []
            : (await editions.FindNoticeSubjectsAsync(subjectIds, cancellationToken))
                .ToDictionary(subject => subject.Id, subject => subject.Label);

        var items = rows
            .Select(row => new EditionQuestionListItemResponse(
                row.Id,
                row.PaperNumber ?? 0,
                Excerpt(row.StatementText),
                row.NoticeSubjectId,
                row.NoticeSubjectId is { } subjectId && labels.TryGetValue(subjectId, out var label) ? label : null,
                row.Status,
                row.ContentLanguage))
            .ToList();

        return new EditionQuestionPageResponse(items, total);
    }

    private static ExamEditionLookupResponse ToLookup(ExamEditionDirectoryEntry edition) =>
        new(
            edition.Id,
            edition.ExamName,
            edition.OrganizerAcronym,
            edition.NoticeYear,
            edition.Position,
            edition.Status.ToString());

    // One read of the editions and one of the notice subjects per page, never one per row (F-96, BR4).
    private async Task<Origins> OriginsAsync(
        IEnumerable<(Guid? ExamEditionId, Guid? NoticeSubjectId)> references,
        CancellationToken cancellationToken)
    {
        var pairs = references.ToList();
        var editionIds = pairs
            .Where(pair => pair.ExamEditionId is not null)
            .Select(pair => pair.ExamEditionId!.Value)
            .Distinct()
            .ToList();
        var subjectIds = pairs
            .Where(pair => pair.NoticeSubjectId is not null)
            .Select(pair => pair.NoticeSubjectId!.Value)
            .Distinct()
            .ToList();

        var editionEntries = editionIds.Count == 0
            ? []
            : await editions.FindManyAsync(editionIds, cancellationToken);
        var subjectEntries = subjectIds.Count == 0
            ? []
            : await editions.FindNoticeSubjectsAsync(subjectIds, cancellationToken);

        return new Origins(
            editionEntries.ToDictionary(entry => entry.Id),
            subjectEntries.ToDictionary(entry => entry.Id));
    }

    private static string Excerpt(string text) =>
        text.Length <= QuestionLimits.ExcerptLength ? text : text[..QuestionLimits.ExcerptLength];

    private sealed record Origins(
        Dictionary<Guid, ExamEditionDirectoryEntry> Editions,
        Dictionary<Guid, NoticeSubjectDirectoryEntry> Subjects)
    {
        // BR11: an edition that no longer resolves leaves the ids and the number and no names; it never throws.
        public QuestionOriginResponse? Build(Guid? examEditionId, int? paperNumber, Guid? noticeSubjectId)
        {
            if (examEditionId is not { } editionId)
            {
                return null;
            }

            Editions.TryGetValue(editionId, out var edition);
            NoticeSubjectDirectoryEntry? subject = null;
            if (noticeSubjectId is { } subjectId)
            {
                Subjects.TryGetValue(subjectId, out subject);
            }

            return new QuestionOriginResponse(
                editionId,
                edition?.ExamId ?? Guid.Empty,
                edition?.ExamName ?? string.Empty,
                edition?.OrganizerAcronym ?? string.Empty,
                edition?.OrganizerName ?? string.Empty,
                edition?.NoticeYear,
                edition?.Position,
                paperNumber ?? 0,
                noticeSubjectId,
                subject?.Label);
        }
    }
}
