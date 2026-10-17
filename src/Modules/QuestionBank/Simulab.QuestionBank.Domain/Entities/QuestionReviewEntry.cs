using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One event in the review of a question (F-105, BR7): sent, withdrawn, approved or rejected, with the reason or
/// the note. The history is append-only: an entry is made by <see cref="Question"/> together with the change of
/// status it records, and is never edited or deleted. Who and when are the audit columns, as for a version.
/// </summary>
public sealed class QuestionReviewEntry : TenantEntity
{
    // For EF.
    private QuestionReviewEntry()
    {
    }

    internal QuestionReviewEntry(Guid questionId, QuestionReviewKind kind, string? reason)
    {
        QuestionId = questionId;
        Kind = kind;
        Reason = reason;
    }

    /// <summary>The question the entry belongs to, held by id with no navigation back.</summary>
    public Guid QuestionId { get; private set; }

    public QuestionReviewKind Kind { get; private set; }

    /// <summary>The rejection reason or the approval note, trimmed; null when none was given.</summary>
    public string? Reason { get; private set; }
}
