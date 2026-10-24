namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One criterion of the rubric of an open-answer or essay question as the form sends it (F-109, BR3): what a grader
/// scores and the most points it can give.
/// </summary>
/// <param name="Name">The name, plain text; trimmed, 1 to 100 characters, unique in the rubric ignoring case and extra white space.</param>
/// <param name="Description">What the grader looks for, plain text; trimmed, up to 500 characters; null or empty means none.</param>
/// <param name="MaxPoints">The most points the criterion gives, a whole number from 1 to 100; null is refused.</param>
public sealed record SaveCriterionRequest(string? Name = null, string? Description = null, int? MaxPoints = null);
