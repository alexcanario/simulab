namespace Simulab.QuestionBank.Contracts;

/// <summary>One subject offered by the classification lookup of the question form (B-28, BR1).</summary>
/// <param name="Id">The subject.</param>
/// <param name="Name">The subject's name.</param>
public sealed record SubjectLookupResponse(Guid Id, string Name);
