namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// Why and when a question was annulled (F-101, BR2), read from its latest version. It is only present while
/// the question's status is <see cref="QuestionStatus.Annulled"/>.
/// </summary>
/// <param name="Reason">The reason the curator wrote, trimmed.</param>
/// <param name="AnnulledAt">When the annulment was recorded; the organizer's own date is not stored (F-98).</param>
public sealed record QuestionAnnulmentResponse(string Reason, DateTimeOffset AnnulledAt);
