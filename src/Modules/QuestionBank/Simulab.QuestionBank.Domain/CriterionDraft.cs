namespace Simulab.QuestionBank.Domain;

/// <summary>One criterion of a rubric as the Application hands it to the question (F-109, BR3); the key trims and checks it.</summary>
/// <param name="Name">The name as written, plain text.</param>
/// <param name="Description">The description as written, plain text; null or empty means none.</param>
/// <param name="MaxPoints">The most points it gives; null is refused.</param>
public sealed record CriterionDraft(string? Name, string? Description, int? MaxPoints);
