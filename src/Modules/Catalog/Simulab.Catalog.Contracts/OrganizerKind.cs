namespace Simulab.Catalog.Contracts;

/// <summary>
/// What an organizer is (F-33 BR7). The three kinds the brief names; it travels as a string. An organizer
/// is the board that elaborates, applies and marks a paper, and nothing else: the body that publishes the
/// notice is <c>IssuingAuthority</c>, its own entity since F-34 v2.
/// </summary>
public enum OrganizerKind
{
    /// <summary>A board hired to run public service exams (CEBRASPE, FGV, VUNESP).</summary>
    ExamBoard = 1,

    /// <summary>A body that issues professional certifications.</summary>
    CertifyingBody = 2,

    /// <summary>A university running its own entrance exam.</summary>
    University = 3
}
