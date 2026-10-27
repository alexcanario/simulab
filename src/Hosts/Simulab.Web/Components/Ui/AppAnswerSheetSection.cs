namespace Simulab.Web.Components.Ui;

/// <summary>
/// One group of <see cref="AppAnswerSheet"/> (F-169, BR3): a heading, the lowest and highest paper number it holds (an
/// annulled number inside the range is simply absent) and its cells in paper order. <c>Lang</c> marks a heading that is
/// content (a notice subject's label); it is null for the app's own "Other".
/// </summary>
public sealed record AppAnswerSheetSection(string Heading, string? Lang, int First, int Last, IReadOnlyList<AppAnswerSheetCell> Cells);

/// <summary>One number of the answer sheet: the question it opens, the number printed on the paper, and whether it is answered.</summary>
public sealed record AppAnswerSheetCell(Guid Id, int Number, bool Answered);
