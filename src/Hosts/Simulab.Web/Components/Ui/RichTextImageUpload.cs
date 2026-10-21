namespace Simulab.Web.Components.Ui;

/// <summary>
/// What a page's upload answers to the image dialog (F-103): the address of the stored picture, or the message to show
/// under the file field. The kit never names a module client: the page does the upload and translates the Api's code.
/// </summary>
/// <param name="Url">The address of the stored picture; null when the upload was refused.</param>
/// <param name="Error">The localized message for the file field; null when the upload went through.</param>
public sealed record RichTextImageUpload(string? Url, string? Error);
