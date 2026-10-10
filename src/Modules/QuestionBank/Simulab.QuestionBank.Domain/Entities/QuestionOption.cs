using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One option of a question (F-95, BR5). It has no rules of its own: the question owns the list, writes it
/// only through its own methods and keeps the positions contiguous. The letter is the position counted from A
/// and is never stored.
/// </summary>
public sealed class QuestionOption : TenantEntity
{
    private QuestionOption()
    {
    }

    internal QuestionOption(Guid questionId, int position, string textHtml, bool isCorrect)
    {
        QuestionId = questionId;
        Position = position;
        TextHtml = textHtml;
        IsCorrect = isCorrect;
    }

    public Guid QuestionId { get; private set; }

    /// <summary>Where the option sits, from 0.</summary>
    public int Position { get; private set; }

    /// <summary>The sanitized HTML of the option text.</summary>
    public string TextHtml { get; private set; } = string.Empty;

    /// <summary>True for the answer key.</summary>
    public bool IsCorrect { get; private set; }
}
