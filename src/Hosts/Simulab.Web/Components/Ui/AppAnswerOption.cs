namespace Simulab.Web.Components.Ui;

/// <summary>
/// One row of <see cref="AppAnswerOptions"/>. <c>Position</c> is what the page gets back in the selection. Exactly one of
/// <c>Text</c> (the app's own words, already translated) and <c>Html</c> (the question's sanitized option text) is set;
/// <c>Letter</c> is the visible A, B, C and is null on a true or false row.
/// </summary>
public sealed record AppAnswerOption(int Position, string? Letter, string? Text = null, string? Html = null);
