namespace Simulab.QuestionBank.Contracts;

/// <summary>Where a question is in its life (F-95, BR2). Review sits between draft and published (F-105).</summary>
public enum QuestionStatus
{
    /// <summary>Being written; seen only in the back office.</summary>
    Draft,

    /// <summary>Complete and ready for the bank; it must stay complete when edited.</summary>
    Published,

    /// <summary>
    /// Annulled by the organizer (F-101): frozen, out of practice, still in the bank, its edition and its
    /// history. Only a reinstatement leaves it. Appended last so a stored or sent value keeps its meaning.
    /// </summary>
    Annulled,

    /// <summary>
    /// Sent for review (F-105): complete, frozen and waiting for a reviewer. Only a decision or a withdrawal leaves it.
    /// Appended last so a stored or sent value keeps its meaning.
    /// </summary>
    InReview
}
