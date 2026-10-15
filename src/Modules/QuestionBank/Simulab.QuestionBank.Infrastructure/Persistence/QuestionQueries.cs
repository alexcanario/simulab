using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;
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

        // F-100 BR1: AND between filters, OR among the values of one. A list with values always restricts;
        // the predicate is never skipped because "every value was picked" (a status may be added later).
        if (sanitized.Statuses is { Count: > 0 } statuses)
        {
            var wanted = statuses.ToList();
            questions = questions.Where(question => wanted.Contains(question.Status));
        }

        if (sanitized.Types is { Count: > 0 } types)
        {
            var wanted = types.ToList();
            questions = questions.Where(question => wanted.Contains(question.Type));
        }

        questions = ByDifficulty(questions, sanitized);

        // The catalog resolves what the question bank only holds by id (BR2, BR3, BR6): a subject is its topics
        // as they are now, an organizer is its editions, and an id nothing has is dropped, not "matches nothing".
        var topicIds = await TopicsOfAsync(sanitized, cancellationToken);
        if (topicIds is { Count: 0 })
        {
            return new QuestionPageResponse([], 0);
        }

        if (topicIds is not null)
        {
            questions = questions.Where(question => topicIds.Contains(question.TopicId));
        }

        var editionFilter = await EditionsOfAsync(sanitized, cancellationToken);
        if (editionFilter is { Count: 0 })
        {
            return new QuestionPageResponse([], 0);
        }

        if (editionFilter is not null)
        {
            questions = questions.Where(question =>
                question.ExamEditionId != null && editionFilter.Contains(question.ExamEditionId.Value));
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
                question.Type,
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
                    row.Type,
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

        // F-98 BR11: the latest version is the highest number; there is no column for it on the question. One
        // row is read, never the JSON options, and F-101 takes the annulment (BR2) from it.
        var latest = await context.QuestionVersions
            .AsNoTracking()
            .Where(version => version.QuestionId == question.Id)
            .OrderByDescending(version => version.Number)
            .Select(version => new { version.Number, version.Annulled, version.Reason, version.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        var annulment = question.Status == QuestionStatus.Annulled && latest is { Annulled: true, Reason: not null }
            ? new QuestionAnnulmentResponse(latest.Reason, latest.CreatedAt)
            : null;

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
            origins.Build(question.ExamEditionId, question.PaperNumber, question.NoticeSubjectId),
            question.TrueFalseKey,
            latest?.Number,
            annulment);
    }

    public async Task<IReadOnlyList<QuestionVersionListItemResponse>?> ListVersionsAsync(
        Guid questionId,
        CancellationToken cancellationToken)
    {
        // The question's own filter hides a deleted one, so its history is not read through the API either.
        if (!await context.Questions.AsNoTracking().AnyAsync(question => question.Id == questionId, cancellationToken))
        {
            return null;
        }

        var rows = await context.QuestionVersions
            .AsNoTracking()
            .Where(version => version.QuestionId == questionId)
            .OrderByDescending(version => version.Number)
            .Select(version => new
            {
                version.Id,
                version.Number,
                version.CreatedAt,
                version.AnswerKeyChanged,
                version.Reason,
                version.Annulled
            })
            .ToListAsync(cancellationToken);

        // F-101 BR6: the rows come newest first, so the version below one is its predecessor; a version that
        // is not annulled and follows an annulled one is the reinstatement.
        return
        [
            .. rows.Select((row, index) => new QuestionVersionListItemResponse(
                row.Id,
                row.Number,
                row.CreatedAt,
                row.AnswerKeyChanged,
                row.Reason,
                index == 0,
                row.Annulled,
                !row.Annulled && index + 1 < rows.Count && rows[index + 1].Annulled))
        ];
    }

    public async Task<Result<QuestionVersionResponse>> FindVersionAsync(
        Guid questionId,
        int number,
        CancellationToken cancellationToken)
    {
        if (!await context.Questions.AsNoTracking().AnyAsync(question => question.Id == questionId, cancellationToken))
        {
            return Result.Failure<QuestionVersionResponse>(
                new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var version = await context.QuestionVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.QuestionId == questionId && row.Number == number, cancellationToken);
        if (version is null)
        {
            return Result.Failure<QuestionVersionResponse>(
                new Error(QuestionBankErrorCodes.QuestionVersionNotFound, ErrorKind.NotFound));
        }

        return Result.Success(new QuestionVersionResponse(
            version.Id,
            version.QuestionId,
            version.Number,
            version.Type,
            version.ContentLanguage,
            version.StatementHtml,
            version.BaseTextHtml,
            version.ExplanationHtml,
            [
                .. version.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new QuestionOptionResponse(option.Position, option.TextHtml, option.IsCorrect))
            ],
            version.TrueFalseKey,
            version.AnswerKeyChanged,
            version.Reason,
            version.CreatedAt,
            version.Annulled));
    }

    public async Task<IReadOnlyList<QuestionTaxonomySubjectResponse>> GetTaxonomyAsync(CancellationToken cancellationToken) =>
        [
            .. (await subjects.ListTaxonomyAsync(null, cancellationToken))
                .Select(subject => new QuestionTaxonomySubjectResponse(
                    subject.Id,
                    subject.Name,
                    [.. subject.Topics.Select(topic => new TopicLookupResponse(topic.Id, topic.Name))]))
        ];

    public async Task<IReadOnlyList<OrganizerLookupResponse>> ListOrganizersAsync(CancellationToken cancellationToken) =>
        [
            .. (await editions.ListOrganizersAsync(cancellationToken))
                .Select(organizer => new OrganizerLookupResponse(organizer.Id, organizer.Acronym, organizer.Name))
        ];

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

    public Task<int?> GetNextPaperNumberAsync(Guid examEditionId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

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

    // F-100 BR4: the numbers and "not set" are one filter; with both it is an OR.
    private static IQueryable<Question> ByDifficulty(IQueryable<Question> questions, QuestionListQuery query)
    {
        var numbers = (query.Difficulties ?? []).ToList();

        return (numbers.Count > 0, query.DifficultyNotSet) switch
        {
            (true, true) => questions.Where(question => question.Difficulty == null || numbers.Contains(question.Difficulty.Value)),
            (true, false) => questions.Where(question => question.Difficulty != null && numbers.Contains(question.Difficulty.Value)),
            (false, true) => questions.Where(question => question.Difficulty == null),
            _ => questions
        };
    }

    // The topics the subject and topic picks mean, or null when nothing was picked or nothing picked exists
    // (the filter is dropped, AC10); an empty set means a subject that exists and has no topic (nothing matches).
    private async Task<List<Guid>?> TopicsOfAsync(QuestionListQuery query, CancellationToken cancellationToken)
    {
        var pickedTopics = query.TopicIds ?? [];
        var pickedSubjects = query.SubjectIds ?? [];
        var resolved = false;
        var result = new HashSet<Guid>();

        if (pickedTopics.Count > 0)
        {
            foreach (var topic in await topics.FindManyAsync([.. pickedTopics], cancellationToken))
            {
                resolved = true;
                result.Add(topic.Id);
            }
        }

        if (pickedSubjects.Count > 0)
        {
            foreach (var subject in await subjects.ListTaxonomyAsync([.. pickedSubjects], cancellationToken))
            {
                resolved = true;
                result.UnionWith(subject.Topics.Select(topic => topic.Id));
            }
        }

        return resolved ? [.. result] : null;
    }

    // The editions the organizer and edition picks mean, or null when nothing was picked or nothing picked exists.
    // Both together are an intersection (AC4); organizers that all are unknown leave the edition alone (AC10).
    private async Task<List<Guid>?> EditionsOfAsync(QuestionListQuery query, CancellationToken cancellationToken)
    {
        HashSet<Guid>? fromOrganizers = null;
        if (query.OrganizerIds is { Count: > 0 } organizerIds)
        {
            var entries = await editions.ListOrganizerEditionsAsync([.. organizerIds], cancellationToken);
            if (entries.Count > 0)
            {
                fromOrganizers = [.. entries.SelectMany(entry => entry.ExamEditionIds)];
            }
        }

        Guid? single = null;
        if (query.ExamEditionId is { } editionId
            && await editions.FindAsync(editionId, cancellationToken) is not null)
        {
            single = editionId;
        }

        return (fromOrganizers, single) switch
        {
            (null, null) => null,
            (null, { } only) => [only],
            ({ } many, null) => [.. many],
            ({ } many, { } only) => many.Contains(only) ? [only] : []
        };
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
