using Microsoft.AspNetCore.Components;

namespace Simulab.Web.Components.Layout;

/// <summary>
/// What the exam session's bar shows (F-169, Screen 2). The layout owns the bar and cascades this object; the page, which
/// knows the edition and how many questions are answered, writes into it. The layout draws it again when a value changed.
/// </summary>
public sealed class ExamSessionBar
{
    private (string? Title, string? TitleLang, string? LeaveHref, bool Phone, string? SheetLabel) _values;

    /// <summary>The edition's label, the page's <c>h1</c>; null while the attempt loads or when it has no edition.</summary>
    public string? Title => _values.Title;

    /// <summary>The language of the title, which is content: the exam's.</summary>
    public string? TitleLang => _values.TitleLang;

    /// <summary>Where "Leave and continue later" goes; null hides the link.</summary>
    public string? LeaveHref => _values.LeaveHref;

    /// <summary>A phone: the leave link is an icon and the answer-sheet button is shown.</summary>
    public bool Phone => _values.Phone;

    /// <summary>The answer-sheet button's accessible name, with the progress; null hides the button.</summary>
    public string? SheetLabel => _values.SheetLabel;

    /// <summary>What the answer-sheet button does.</summary>
    public EventCallback OnOpenSheet { get; private set; }

    /// <summary>Raised when a value changed and the bar has to be drawn again.</summary>
    public event Action? Changed;

    public void Update(string? title, string? titleLang, string? leaveHref, bool phone, string? sheetLabel, EventCallback onOpenSheet)
    {
        OnOpenSheet = onOpenSheet;
        var next = (title, titleLang, leaveHref, phone, sheetLabel);
        if (next == _values)
        {
            return;
        }

        _values = next;
        Changed?.Invoke();
    }
}
