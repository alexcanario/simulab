namespace Simulab.QuestionBank.Contracts;

/// <summary>How a question is answered (F-95, BR2). Each later type adds its own value.</summary>
public enum QuestionType
{
    /// <summary>One statement, two to five options, exactly one correct.</summary>
    SingleChoice,

    /// <summary>One statement and one answer key: true ("certo") or false ("errado"). It has no options (F-97).</summary>
    TrueFalse
}
