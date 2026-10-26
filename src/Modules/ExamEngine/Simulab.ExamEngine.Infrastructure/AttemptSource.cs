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
public sealed class AttemptSource(IExamEditionDirectory editions, IPublishedQuestionDirectory questions) : IAttemptSource
{
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
