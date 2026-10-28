namespace Simulab.Web.Components.Ui;

/// <summary>How <see cref="AppAnswerOptions"/> marks answers (F-169): one of a list, one of two, or any number of a list.</summary>
public enum AppAnswerMode
{
    /// <summary>One option; a click on the marked one clears it. A radio group.</summary>
    SingleChoice,

    /// <summary>The two rows of a true or false question. A radio group, like <see cref="SingleChoice"/>.</summary>
    TrueFalse,

    /// <summary>Any number of options; each click toggles one. A checkbox group.</summary>
    Multiple
}
