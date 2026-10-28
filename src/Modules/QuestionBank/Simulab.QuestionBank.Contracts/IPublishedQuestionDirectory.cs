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

    /// <summary>
    /// What a student reads of these versions (F-169, BR2): the type, the language, the statement, the base text and the
    /// options as stored, with the question's notice subject. Never a key, an explanation or a rubric. A version that is
    /// unknown is left out.
    /// </summary>
    Task<IReadOnlyList<QuestionVersionContent>> ListVersionContentAsync(
        IReadOnlyCollection<QuestionVersionKey> versions,
        CancellationToken cancellationToken);

    /// <summary>
    /// The file of a picture when one of these versions holds it in its statement, base text or options (F-169, BR12);
    /// null otherwise, or when the picture or its file is gone. The caller disposes the stream.
    /// </summary>
    Task<QuestionImageFile?> OpenVersionImageAsync(
        Guid imageId,
        IReadOnlyCollection<QuestionVersionKey> versions,
        CancellationToken cancellationToken);
}

/// <summary>One version of one question.</summary>
public sealed record QuestionVersionKey(Guid QuestionId, int VersionNumber);

/// <summary>
/// One version of a question as a student reads it (F-169): sanitized HTML as stored, the options in position order and the
/// notice subject the question has today.
/// </summary>
public sealed record QuestionVersionContent(
    Guid QuestionId,
    int VersionNumber,
    string ContentLanguage,
    string StatementHtml,
    IReadOnlyList<QuestionVersionOptionContent> Options,
    string? BaseTextHtml = null,
    Guid? NoticeSubjectId = null);

/// <summary>One option of a version: its position, counted from 0, and its text. Never whether it is right.</summary>
public sealed record QuestionVersionOptionContent(int Position, string TextHtml);

/// <summary>The file of a stored picture and its type; the caller disposes <c>Content</c>.</summary>
public sealed record QuestionImageFile(Stream Content, string ContentType);

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
