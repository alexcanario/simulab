namespace Simulab.Catalog.Domain.Entities;

/// <summary>What an organizer is (F-33, BR7). The three kinds the brief names; it travels as a string.</summary>
public enum OrganizerKind
{
    /// <summary>A board hired to run public service exams (CEBRASPE, FGV, VUNESP).</summary>
    ExamBoard = 1,

    /// <summary>A body that issues professional certifications.</summary>
    CertifyingBody = 2,

    /// <summary>A university running its own entrance exam.</summary>
    University = 3
}
