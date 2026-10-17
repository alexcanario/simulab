namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One line of a question's review history (F-105, UC6, BR7). The history is oldest first. The person is not
/// shown: the module cannot turn a user id into a name.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Reason">The rejection reason or the approval note; null when none was given.</param>
/// <param name="At">When it happened.</param>
public sealed record QuestionReviewEntryResponse(QuestionReviewKind Kind, string? Reason, DateTimeOffset At);
