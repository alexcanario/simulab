using Microsoft.EntityFrameworkCore;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question bank's answer to the exam engine (F-166): the published questions of an edition at their latest
/// version and any one version, reduced to what checking an answer needs; and (F-169) what a student reads of given
/// versions and their pictures. The key, the explanation and the rubric are never read into the answer.
/// </summary>
public sealed class PublishedQuestionDirectory(QuestionBankModuleDbContext context, OpenQuestionImageHandler images)
    : IPublishedQuestionDirectory
{
    public async Task<IReadOnlyList<QuestionAnswerShape>> ListPublishedByEditionAsync(
        Guid examEditionId,
        CancellationToken cancellationToken)
    {
        var rows = await context.Questions
            .AsNoTracking()
            .Where(question => question.ExamEditionId == examEditionId && question.Status == QuestionStatus.Published)
            .OrderBy(question => question.PaperNumber)
            .Select(question => new
            {
                question.Id,
                PaperNumber = question.PaperNumber!.Value,
                Version = context.QuestionVersions
                    .Where(version => version.QuestionId == question.Id)
                    .OrderByDescending(version => version.Number)
                    .First()
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => ShapeOf(row.Id, row.PaperNumber, row.Version))];
    }

    public async Task<QuestionAnswerShape?> FindVersionAsync(
        Guid questionId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var row = await context.QuestionVersions
            .AsNoTracking()
            .Where(version => version.QuestionId == questionId && version.Number == versionNumber)
            .Select(version => new
            {
                Version = version,
                PaperNumber = context.Questions
                    .Where(question => question.Id == version.QuestionId)
                    .Select(question => question.PaperNumber)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : ShapeOf(questionId, row.PaperNumber ?? 0, row.Version);
    }

    public async Task<IReadOnlyList<QuestionVersionContent>> ListVersionContentAsync(
        IReadOnlyCollection<QuestionVersionKey> versions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(versions);

        var questionIds = versions.Select(key => key.QuestionId).Distinct().ToList();
        var wanted = versions.ToHashSet();
        var rows = await context.QuestionVersions
            .AsNoTracking()
            .Where(version => questionIds.Contains(version.QuestionId))
            .ToListAsync(cancellationToken);
        var subjects = await context.Questions
            .AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .Select(question => new { question.Id, question.NoticeSubjectId })
            .ToDictionaryAsync(question => question.Id, question => question.NoticeSubjectId, cancellationToken);

        // T1 of F-169: a projection, so neither the key of an option nor the explanation is ever read into the answer.
        return [.. rows
            .Where(version => wanted.Contains(new QuestionVersionKey(version.QuestionId, version.Number)))
            .Select(version => new QuestionVersionContent(
                version.QuestionId,
                version.Number,
                version.ContentLanguage,
                version.StatementHtml,
                [.. version.Options.OrderBy(option => option.Position).Select(option => new QuestionVersionOptionContent(option.Position, option.TextHtml))],
                version.BaseTextHtml,
                subjects.GetValueOrDefault(version.QuestionId)))];
    }

    // A uuid is hex digits and hyphens, so it has no LIKE wildcard in it.
    // shortcut(F-103): a LIKE scan over the versions, no index and no link table (decision of 2026-10-20); F-169 is its second user, limited to the attempt's own versions — revisit when question_versions reaches the hundreds of thousands of rows.
    public async Task<QuestionImageFile?> OpenVersionImageAsync(
        Guid imageId,
        IReadOnlyCollection<QuestionVersionKey> versions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(versions);

        if (versions.Count == 0)
        {
            return null;
        }

        var address = $"%/question-images/{imageId}%";
        var questionIds = versions.Select(key => key.QuestionId).ToArray();
        var numbers = versions.Select(key => key.VersionNumber).ToArray();
        var held = await context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM question_bank.question_versions version
                    JOIN unnest({questionIds}::uuid[], {numbers}::integer[]) AS pair(question_id, number)
                        ON version.question_id = pair.question_id AND version.number = pair.number
                    WHERE version.statement_html LIKE {address}
                        OR version.base_text_html LIKE {address}
                        OR version.options::text LIKE {address}) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (!held)
        {
            return null;
        }

        // T3 of F-169: the handler only opens the file; whether the student may see it was decided just above.
        var opened = await images.HandleAsync(imageId, cancellationToken);

        return opened.IsSuccess ? new QuestionImageFile(opened.Value.Content, opened.Value.ContentType) : null;
    }

    private static QuestionAnswerShape ShapeOf(Guid questionId, int paperNumber, QuestionVersion version) =>
        new(
            questionId,
            paperNumber,
            version.Number,
            version.Type,
            [.. version.Options.Select(option => option.Position).Order()],
            [.. version.MatchItems.Select(item => item.Position).Order()],
            [.. version.Blanks.Select(blank => blank.Number).Order()],
            version.AnswerSizeMax);
}
