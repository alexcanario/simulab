namespace Simulab.QuestionBank.Contracts;

/// <summary>One notice subject of an edition, offered by the origin lookup in display order (F-96, BR9).</summary>
/// <param name="Id">The notice subject.</param>
/// <param name="Group">The group it sits under, when it has one.</param>
/// <param name="Label">What the notice calls it.</param>
/// <param name="QuestionCount">How many questions the notice gives it, when it says.</param>
public sealed record NoticeSubjectLookupResponse(Guid Id, string? Group, string Label, int? QuestionCount = null);
