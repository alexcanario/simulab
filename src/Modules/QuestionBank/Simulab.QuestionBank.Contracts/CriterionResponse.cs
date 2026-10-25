namespace Simulab.QuestionBank.Contracts;

/// <summary>One criterion of the rubric of an open-answer or essay question as stored (F-109).</summary>
/// <param name="Position">Where it sits in the rubric, from 1: the order the curator wrote and the number errors name.</param>
/// <param name="Name">The name, trimmed.</param>
/// <param name="Description">What the grader looks for, trimmed; null when there is none.</param>
/// <param name="MaxPoints">The most points it gives, 1 to 100.</param>
public sealed record CriterionResponse(int Position, string Name, string? Description, int MaxPoints);
