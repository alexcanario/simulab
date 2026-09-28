namespace Simulab.Catalog.Contracts;

/// <summary>
/// Who can see an exam edition (F-35, BR9). <c>InReview</c> is not offered: it arrives with the AI import
/// (epic 698).
/// </summary>
public enum ExamEditionStatus
{
    /// <summary>Kept in the administration; students do not see it.</summary>
    Draft = 1,

    /// <summary>Offered to students in the catalog.</summary>
    Published = 2
}
