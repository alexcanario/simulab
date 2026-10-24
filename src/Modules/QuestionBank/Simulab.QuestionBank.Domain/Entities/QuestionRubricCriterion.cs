using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One criterion of the rubric of an open-answer or essay question (F-109, BR1, BR3): what a grader scores and the most
/// points it can give. Like an option it has no rules of its own: the question owns the list and writes it only
/// through its own methods.
/// </summary>
public sealed class QuestionRubricCriterion : TenantEntity
{
    private QuestionRubricCriterion()
    {
    }

    internal QuestionRubricCriterion(int position, string name, string? description, int maxPoints)
    {
        Position = position;
        Name = name;
        Description = description;
        MaxPoints = maxPoints;
    }

    public Guid QuestionId { get; private set; }

    /// <summary>Where it sits in the rubric, from 1: the order the curator wrote.</summary>
    public int Position { get; private set; }

    /// <summary>The name, trimmed, 1 to 100 characters.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>What the grader looks for, trimmed, up to 500 characters; null when there is none.</summary>
    public string? Description { get; private set; }

    /// <summary>The most points the criterion gives, 1 to 100.</summary>
    public int MaxPoints { get; private set; }

    internal void Set(int position, string name, string? description, int maxPoints)
    {
        Position = position;
        Name = name;
        Description = description;
        MaxPoints = maxPoints;
    }
}
