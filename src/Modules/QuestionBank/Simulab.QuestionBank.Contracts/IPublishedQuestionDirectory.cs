namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the exam engine asks the question bank (F-166): the published questions of an edition and what is needed to
/// check an answer against one version. The answer never carries a key, a model answer or an explanation.
/// </summary>
public interface IPublishedQuestionDirectory
{
    /// <summary>
    /// The <see cref="QuestionStatus.Published"/> questions of the edition in paper-number order, each at its
    /// latest version. An empty list when the edition has none.
    /// </summary>
    Task<IReadOnlyList<QuestionAnswerShape>> ListPublishedByEditionAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>One version of a question, whatever its status today; null when the question or the version is unknown.</summary>
    Task<QuestionAnswerShape?> FindVersionAsync(Guid questionId, int versionNumber, CancellationToken cancellationToken);
}

/// <summary>
/// One version of a question as far as answering it goes: its type, its place in the paper and the positions and
/// numbers an answer may name. <c>AnswerSizeMax</c> is an essay's maximum lines or an open answer's maximum
/// characters, null when not set.
/// </summary>
public sealed record QuestionAnswerShape(
    Guid QuestionId,
    int PaperNumber,
    int VersionNumber,
    QuestionType Type,
    IReadOnlyList<int> OptionPositions,
    IReadOnlyList<int> MatchItemPositions,
    IReadOnlyList<int> BlankNumbers,
    int? AnswerSizeMax = null);
