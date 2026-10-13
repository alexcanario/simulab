namespace Simulab.Web.Components.Ui;

/// <summary>F-75: one picked item of an <see cref="AppMultiPickField"/>, drawn as a removable <see cref="AppChip"/>.</summary>
/// <param name="Key">The key of the option it came from; it is what the field hands back when the chip is removed.</param>
/// <param name="Text">The chip's visible text.</param>
/// <param name="Name">What the item is called when it is spoken of: the remove button's wording, "Remove Mathematics, whole subject".</param>
/// <param name="Icon">A decorative <see cref="AppIcons"/> constant.</param>
/// <param name="Secondary">The chip's quieter text ("whole subject").</param>
/// <param name="Mark">Set when the Api refused this item: the chip is drawn in error with this text.</param>
/// <param name="SpokenText">What a screen reader hears in place of <paramref name="Text"/>, when the visible form is a symbol.</param>
public sealed record AppMultiPickItem(
    string Key,
    string Text,
    string Name,
    string? Icon = null,
    string? Secondary = null,
    string? Mark = null,
    string? SpokenText = null);
