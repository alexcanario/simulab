namespace Simulab.QuestionBank.Contracts;

/// <summary>What happened in one entry of a question's review history (F-105, BR7). Stored as a string.</summary>
public enum QuestionReviewKind
{
    /// <summary>A curator sent a complete draft for review.</summary>
    Submitted,

    /// <summary>A curator took the question back out of review; it is a draft again.</summary>
    Withdrawn,

    /// <summary>A reviewer approved it; it was published.</summary>
    Approved,

    /// <summary>A reviewer rejected it with a reason; it is a draft again.</summary>
    Rejected
}
