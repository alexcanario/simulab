using Microsoft.EntityFrameworkCore;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question bank's answer to the exam engine (F-166): the published questions of an edition at their latest
/// version and any one version, reduced to what checking an answer needs. The key, the explanation and the rubric are
/// never read into the answer.
/// </summary>
public sealed class PublishedQuestionDirectory(QuestionBankModuleDbContext context) : IPublishedQuestionDirectory
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
