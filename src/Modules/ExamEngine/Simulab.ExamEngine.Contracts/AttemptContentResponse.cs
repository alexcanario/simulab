namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// What the student reads to take an attempt (F-169, BR2, BR3): the questions at the versions the attempt fixed, in
/// paper-number order, the sections they fall in and the edition. Only the owner reads it (BR11). It never carries an
/// answer key, an explanation or a rubric. <c>Edition</c> is null when the edition no longer exists; <c>SubmittedAt</c> is
/// null while the attempt is in progress.
/// </summary>
public sealed record AttemptContentResponse(
    Guid Id,
    Guid ExamEditionId,
    AttemptStatus Status,
    DateTimeOffset StartedAt,
    IReadOnlyList<AttemptSectionResponse> Sections,
    IReadOnlyList<AttemptQuestionContentResponse> Questions,
    AttemptEditionResponse? Edition = null,
    DateTimeOffset? SubmittedAt = null);

/// <summary>
/// The edition an attempt belongs to, as the simulator's bar names it. <c>ContentLanguage</c> is the exam's, or the first
/// question's when the exam is no longer published; null when neither is known.
/// </summary>
public sealed record AttemptEditionResponse(
    Guid ExamId,
    string ExamName,
    string OrganizerName,
    string OrganizerAcronym,
    int NoticeYear,
    string? Position = null,
    string? ContentLanguage = null);

/// <summary>
/// A section of the paper (F-169, BR3): the questions of one notice subject, in paper-number order, with the lowest and
/// highest paper number among them. A null <c>NoticeSubjectId</c> is the last section, of the questions with none.
/// </summary>
public sealed record AttemptSectionResponse(
    int FirstPaperNumber,
    int LastPaperNumber,
    IReadOnlyList<Guid> QuestionIds,
    Guid? NoticeSubjectId = null,
    string? Label = null);

/// <summary>
/// One question of an attempt as the student reads it, at the version fixed at the start (F-169, BR2). The HTML is the
/// sanitized text as stored, with its picture addresses pointing at the attempt. <c>Answer</c> is null while it is blank.
/// </summary>
public sealed record AttemptQuestionContentResponse(
    Guid QuestionId,
    int PaperNumber,
    int VersionNumber,
    AttemptQuestionType Type,
    string ContentLanguage,
    string StatementHtml,
    IReadOnlyList<AttemptOptionResponse> Options,
    string? BaseTextHtml = null,
    Guid? NoticeSubjectId = null,
    AnswerPayload? Answer = null);

/// <summary>One option of a question version: its position, counted from 0 as an answer names it, and its text.</summary>
public sealed record AttemptOptionResponse(int Position, string TextHtml);
