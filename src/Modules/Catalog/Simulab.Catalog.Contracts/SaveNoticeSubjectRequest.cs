namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the notice subject dialog sends (F-74, UC2 and UC3). Every field is optional in the contract so a
/// missing one is a validation error with a code, never a silent default.
/// </summary>
/// <param name="Group">How the notice groups the subject; blank is stored as null, "no group" (BR3).</param>
/// <param name="Label">The subject as the notice names it, 2 to 200 characters (BR2). Required.</param>
/// <param name="QuestionCount">The number of questions the notice states, 1 to 500; null when it does not say (BR4).</param>
public sealed record SaveNoticeSubjectRequest(
    string? Group = null,
    string? Label = null,
    int? QuestionCount = null);
