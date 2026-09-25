namespace Simulab.Catalog.Contracts;

/// <summary>
/// How far an exam reaches (F-34, BR7). <see cref="State"/> and <see cref="Municipal"/> ask for the
/// scope detail: which state, which municipality (BR8).
/// </summary>
public enum ExamScope
{
    /// <summary>The whole country. Certifications and ENEM sit here.</summary>
    National = 1,

    /// <summary>One state.</summary>
    State = 2,

    /// <summary>One municipality.</summary>
    Municipal = 3
}
