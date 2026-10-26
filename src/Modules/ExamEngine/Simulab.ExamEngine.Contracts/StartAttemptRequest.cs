namespace Simulab.ExamEngine.Contracts;

/// <summary>What starts an attempt (F-166, UC1): the published edition to take.</summary>
/// <param name="ExamEditionId">The edition. Required.</param>
public sealed record StartAttemptRequest(Guid? ExamEditionId = null);
