using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One left item of a matching question (F-106, BR1). Like an option it has no rules of its own: the question owns
/// the list, writes it only through its own methods and keeps the positions contiguous. The number is the position
/// counted from 1 and is never stored; <see cref="MatchPosition"/> is the only copy of the key.
/// </summary>
public sealed class QuestionMatchItem : TenantEntity
{
    private QuestionMatchItem()
    {
    }

    internal QuestionMatchItem(int position, string textHtml, int? matchPosition)
    {
        Position = position;
        TextHtml = textHtml;
        MatchPosition = matchPosition;
    }

    public Guid QuestionId { get; private set; }

    /// <summary>Where the item sits, from 0.</summary>
    public int Position { get; private set; }

    /// <summary>The sanitized HTML of the item text.</summary>
    public string TextHtml { get; private set; } = string.Empty;

    /// <summary>The position of the option (right item) it matches; null while not set on a draft.</summary>
    public int? MatchPosition { get; private set; }

    internal void Set(string textHtml, int? matchPosition)
    {
        TextHtml = textHtml;
        MatchPosition = matchPosition;
    }
}
