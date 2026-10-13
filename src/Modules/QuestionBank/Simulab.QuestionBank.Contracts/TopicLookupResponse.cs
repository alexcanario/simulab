namespace Simulab.QuestionBank.Contracts;

/// <summary>One topic of a subject offered by the classification lookup of the question form (B-28, BR2).</summary>
/// <param name="Id">The topic.</param>
/// <param name="Name">The topic's name.</param>
public sealed record TopicLookupResponse(Guid Id, string Name);
