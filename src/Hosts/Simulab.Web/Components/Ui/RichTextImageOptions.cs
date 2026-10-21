namespace Simulab.Web.Components.Ui;

/// <summary>
/// What turns the image button on for a rich text field (F-103): the page's upload and the two limits the dialog checks
/// for comfort (the Api is the authority). The kit holds no number of its own.
/// </summary>
/// <param name="Upload">Stores the chosen file and answers with its address or the message to show.</param>
/// <param name="MaxBytes">The largest file the dialog sends.</param>
/// <param name="AltMaxLength">The most characters of the alternative text.</param>
public sealed record RichTextImageOptions(
    Func<RichTextImageFile, Task<RichTextImageUpload>> Upload,
    long MaxBytes,
    int AltMaxLength);
