namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// One option as the form holds it before Save (F-95). The position is the row's index and the letter comes from
/// it (BR5); nothing here is stored until the question is saved.
/// </summary>
public sealed class OptionDraft
{
    /// <summary>The option's HTML as the editor holds it.</summary>
    public string Html { get; set; } = string.Empty;

    /// <summary>True for the option that is the answer key.</summary>
    public bool IsCorrect { get; set; }

    /// <summary>The refusal that belongs to this option's editor, already translated.</summary>
    public string? Error { get; set; }

    /// <summary>True for a row the curator has just added: its editor takes the focus once it is ready.</summary>
    public bool FocusOnRender { get; set; }
}
