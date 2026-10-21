using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The shape rules about the images in the texts of one question or base text (F-103, BR4 and BR7). The texts are the
/// ones that count together: the statement and the options of a question, the text of a base text.
/// </summary>
internal static class QuestionImageRules
{
    /// <summary>
    /// The first rule the texts break, or null: an <c>img</c> with no alternative text attribute, one with an
    /// attribute over the limit, then more images than a text may hold. The codes are the owner's (question or base text).
    /// </summary>
    internal static Error? Check(IEnumerable<SanitizedHtml> texts, string altRequiredCode, string altTooLongCode, string tooManyCode)
    {
        var all = texts.ToList();

        if (all.Sum(text => text.ImagesWithoutAlt) > 0)
        {
            return new Error(altRequiredCode, ErrorKind.Validation);
        }

        if (all.Sum(text => text.AltTooLong) > 0)
        {
            return new Error(altTooLongCode, ErrorKind.Validation);
        }

        return all.Sum(text => text.ImageIds.Count) > QuestionLimits.ImagesPerTextMax
            ? new Error(tooManyCode, ErrorKind.Validation)
            : null;
    }
}
