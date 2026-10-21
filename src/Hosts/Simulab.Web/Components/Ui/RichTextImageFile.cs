namespace Simulab.Web.Components.Ui;

/// <summary>A picture the reader chose, already read from the browser (F-103): what the page's upload sends to the Api.</summary>
/// <param name="Name">The file name as the browser has it; the Api never uses it to name the stored file.</param>
/// <param name="ContentType">The type the browser guessed; the Api decides from the bytes and ignores it.</param>
/// <param name="Content">The bytes of the file, up to the limit of <see cref="RichTextImageOptions"/>.</param>
public sealed record RichTextImageFile(string Name, string ContentType, byte[] Content);
