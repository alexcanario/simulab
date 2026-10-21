namespace Simulab.QuestionBank.Domain;

/// <summary>What holds a stored image in its HTML (F-103, BR6). A statement and its options belong to the question.</summary>
public enum QuestionImageOwnerKind
{
    /// <summary>The statement or an option of a question.</summary>
    Question,

    /// <summary>The text of a base text.</summary>
    BaseText
}
