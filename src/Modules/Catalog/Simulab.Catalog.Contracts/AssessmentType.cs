namespace Simulab.Catalog.Contracts;

/// <summary>
/// What kind of assessment an exam is (F-34, BR6). The four the brief names; it travels as a string,
/// so an unknown value is answered with <c>exam.assessment_type_invalid</c> and not by a failed
/// deserialization (the lesson of F-33's review).
/// </summary>
public enum AssessmentType
{
    /// <summary>A competitive exam for a public position (concurso publico).</summary>
    PublicServiceExam = 1,

    /// <summary>A professional certification.</summary>
    Certification = 2,

    /// <summary>A university entrance exam (vestibular).</summary>
    UniversityEntranceExam = 3,

    /// <summary>The Brazilian national secondary education exam.</summary>
    Enem = 4
}
