namespace Simulab.Web.Components.Ui;

/// <summary>The picture the reader chose in the image dialog: where it is stored and what it says (F-103).</summary>
/// <param name="Url">The address of the stored picture, the one the text will hold.</param>
/// <param name="Alt">The alternative text; empty marks the picture decorative.</param>
public sealed record RichTextImageChoice(string Url, string Alt);
