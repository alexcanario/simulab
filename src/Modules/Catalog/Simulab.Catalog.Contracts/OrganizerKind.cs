namespace Simulab.Catalog.Contracts;

/// <summary>
/// What an organizer is (F-33 BR7, F-34 BR4). The three kinds the brief names plus the public body F-34
/// added, when the exam gained its issuing authority; it travels as a string. The kind says what the
/// institution is, never which role it plays: the same one contracts an exam and runs another (F-34 BR3).
/// </summary>
public enum OrganizerKind
{
    /// <summary>A board hired to run public service exams (CEBRASPE, FGV, VUNESP).</summary>
    ExamBoard = 1,

    /// <summary>A body that issues professional certifications.</summary>
    CertifyingBody = 2,

    /// <summary>A university running its own entrance exam.</summary>
    University = 3,

    /// <summary>A city hall, a state or federal government body, a ministry, an agency or a public foundation (F-34).</summary>
    PublicBody = 4
}
