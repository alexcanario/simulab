namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// One left item of a matching question as the form holds it before Save (F-106). The number is the row's index; the
/// match holds the right item itself, so moving a right item keeps the match and removing it clears the match.
/// </summary>
public sealed class MatchItemDraft
{
    /// <summary>The item's HTML as the editor holds it.</summary>
    public string Html { get; set; } = string.Empty;

    /// <summary>The right item (an option) this item matches; null while not chosen.</summary>
    public OptionDraft? Match { get; set; }

    /// <summary>The refusal that belongs to this item's editor, already translated.</summary>
    public string? Error { get; set; }

    /// <summary>True for a row the curator has just added: its editor takes the focus once it is ready.</summary>
    public bool FocusOnRender { get; set; }
}
