namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// How a question of an attempt is answered. A copy of the question bank's own list, because a contracts project
/// reads the shared kernel only; a test keeps the two lists equal (F-166, BR5).
/// </summary>
public enum AttemptQuestionType
{
    SingleChoice,
    TrueFalse,
    MultipleAnswer,
    Matching,
    FillInTheBlanks,
    ShortAnswer,
    OpenAnswer,
    Essay
}
