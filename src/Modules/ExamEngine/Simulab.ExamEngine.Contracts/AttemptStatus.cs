namespace Simulab.ExamEngine.Contracts;

/// <summary>Where an attempt is (F-166, BR6). Pause, abandoned and automatic submit arrive with the timer.</summary>
public enum AttemptStatus
{
    /// <summary>Takes answers.</summary>
    InProgress,

    /// <summary>Submitted by the student; it never changes again.</summary>
    Submitted
}
