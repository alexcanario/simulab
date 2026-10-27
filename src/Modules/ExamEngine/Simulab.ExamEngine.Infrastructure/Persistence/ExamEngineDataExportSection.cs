using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Infrastructure.Persistence;

/// <summary>
/// The <c>examEngine</c> section of the file a user downloads (F-167 BR7-BR9): every attempt of the user, read by the
/// token subject alone, as <see cref="ExamEngineDataResponse"/>. A thin read over the context, as the module's filters
/// see it.
/// </summary>
public sealed class ExamEngineDataExportSection(ExamEngineModuleDbContext context) : IDataExportSection
{
    public const string SectionName = "examEngine";

    public string Name => SectionName;

    public async Task<JsonElement> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var attempts = await context.Attempts
            .AsNoTracking()
            .Include(attempt => attempt.Answers)
            .AsSplitQuery()
            .Where(attempt => attempt.StudentId == userId)
            .OrderBy(attempt => attempt.StartedAt)
            .ThenBy(attempt => attempt.Id)
            .ToListAsync(cancellationToken);

        var section = new ExamEngineDataResponse([.. attempts.Select(attempt => new ExportedAttemptResponse(
            attempt.Id,
            attempt.ExamEditionId,
            attempt.Status,
            attempt.StartedAt,
            attempt.SubmittedAt,
            [.. attempt.Answers
                .OrderBy(answer => answer.PaperNumber)
                .Select(answer => new ExportedAttemptQuestionResponse(
                    answer.QuestionId, answer.PaperNumber, answer.VersionNumber, answer.QuestionType, answer.Answer))]))]);

        return JsonSerializer.SerializeToElement(section, AppJson.Options);
    }
}
