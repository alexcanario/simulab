using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// What the module asks the catalog and the question bank, in its own types (F-166). Application reads its own contracts
/// and domain only, so the adapter over the two other modules' contracts lives in Infrastructure.
/// </summary>
public interface IAttemptSource
{
    /// <summary>The edition, or null when it does not exist.</summary>
    Task<EditionState?> FindEditionAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>The published questions of the edition in paper-number order, each at its latest version (BR2).</summary>
    Task<IReadOnlyList<AttemptQuestion>> ListPublishedQuestionsAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>What an answer to one version of a question may name; null when the version is unknown.</summary>
    Task<AnswerShape?> FindShapeAsync(Guid questionId, int versionNumber, CancellationToken cancellationToken);

    /// <summary>What a student reads of these versions (F-169, BR2); an unknown version is left out.</summary>
    Task<IReadOnlyList<QuestionContent>> ListContentAsync(IReadOnlyCollection<AttemptVersion> versions, CancellationToken cancellationToken);

    /// <summary>
    /// The edition as the simulator's bar names it, with the exam's content language (null when the exam is no longer
    /// published); null when the edition no longer exists.
    /// </summary>
    Task<AttemptEditionResponse?> FindEditionLabelAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>The label of each live notice subject among the ids; unknown or deleted ids are left out.</summary>
    Task<IReadOnlyDictionary<Guid, string>> FindNoticeSubjectLabelsAsync(
        IReadOnlyCollection<Guid> noticeSubjectIds,
        CancellationToken cancellationToken);

    /// <summary>The picture when one of these versions holds it (F-169, BR12); null otherwise.</summary>
    Task<AttemptImage?> OpenImageAsync(Guid imageId, IReadOnlyCollection<AttemptVersion> versions, CancellationToken cancellationToken);
}

/// <summary>One version of one question, as an attempt fixed it.</summary>
public sealed record AttemptVersion(Guid QuestionId, int VersionNumber);

/// <summary>One version of a question as a student reads it: sanitized HTML as stored, the options in position order.</summary>
public sealed record QuestionContent(
    Guid QuestionId,
    int VersionNumber,
    string ContentLanguage,
    string StatementHtml,
    IReadOnlyList<AttemptOptionResponse> Options,
    string? BaseTextHtml,
    Guid? NoticeSubjectId);

/// <summary>A picture of a question version, opened for the student; the caller disposes the stream (F-169, BR12).</summary>
public sealed record AttemptImage(Stream Content, string ContentType);

/// <summary>An edition and whether it is published.</summary>
public sealed record EditionState(Guid Id, bool IsPublished);

/// <summary>
/// The type of a question version and what an answer to it may name: option and matching item positions, blank numbers
/// and the size limit of its answer (characters for an open answer, lines for an essay), null when not set.
/// </summary>
public sealed record AnswerShape(
    AttemptQuestionType Type,
    IReadOnlyList<int> OptionPositions,
    IReadOnlyList<int> ItemPositions,
    IReadOnlyList<int> BlankNumbers,
    int? SizeMax);
