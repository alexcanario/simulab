namespace Simulab.Catalog.Contracts;

/// <summary>A notice subject as another module may see it (F-96): its label and where it sits, never its mappings.</summary>
/// <param name="Id">The notice subject.</param>
/// <param name="ExamEditionId">The edition it belongs to.</param>
/// <param name="Group">The group it sits under, when it has one.</param>
/// <param name="Label">What the notice calls it.</param>
/// <param name="QuestionCount">How many questions the notice gives it, when it says.</param>
/// <param name="PointsPerQuestion">What each of its questions is worth, when the notice says (F-168).</param>
public sealed record NoticeSubjectDirectoryEntry(
    Guid Id,
    Guid ExamEditionId,
    string? Group,
    string Label,
    int? QuestionCount,
    decimal? PointsPerQuestion = null);
