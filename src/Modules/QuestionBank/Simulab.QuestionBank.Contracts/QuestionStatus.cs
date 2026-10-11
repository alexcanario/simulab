namespace Simulab.QuestionBank.Contracts;

/// <summary>Where a question is in its life (F-95, BR2). A review state comes with F-105.</summary>
public enum QuestionStatus
{
    /// <summary>Being written; seen only in the back office.</summary>
    Draft,

    /// <summary>Complete and ready for the bank; it must stay complete when edited.</summary>
    Published
}
