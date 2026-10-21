using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Services.QuestionBank;

/// <summary>
/// What the question forms answer to the image dialog (F-103): the Api's answer as the dialog understands it, the address of
/// the stored picture or the message for the file field. The two forms that take pictures share it.
/// </summary>
internal static class QuestionImageUpload
{
    /// <summary>The upload went through: its address. Otherwise the text of the Api's code, or the generic one when the session is gone.</summary>
    internal static RichTextImageUpload Answer(QuestionBankResult<QuestionImageResponse>? result, ErrorText errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return result?.Value is { } image
            ? new RichTextImageUpload(image.Url, null)
            : new RichTextImageUpload(null, errors.For(result?.ErrorCode));
    }

    /// <summary>The limits the dialog checks for comfort, read from the one place the Api reads them.</summary>
    internal static RichTextImageOptions Options(Func<RichTextImageFile, Task<RichTextImageUpload>> upload) =>
        new(upload, QuestionLimits.ImageMaxBytes, QuestionLimits.ImageAltMaxLength);
}
