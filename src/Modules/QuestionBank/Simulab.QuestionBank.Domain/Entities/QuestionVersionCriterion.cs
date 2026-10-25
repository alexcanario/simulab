namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One criterion of a <see cref="QuestionVersion"/> of an open-answer or essay question (F-109, BR10): a copy of what
/// the criterion held when the version was recorded. It lives inside the version's JSON column, in rubric order, and is
/// never changed.
/// </summary>
public sealed class QuestionVersionCriterion
{
    /// <summary>Where it sat in the rubric, from 1.</summary>
    public required int Position { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required int MaxPoints { get; init; }
}
