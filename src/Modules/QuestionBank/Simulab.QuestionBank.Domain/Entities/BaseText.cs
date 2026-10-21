using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// A passage, table or case that several questions refer to (F-102, BR1 to BR3): a title the curator finds it
/// by, the language it is written in and its sanitized HTML. Global data: <see cref="TenantEntity.TenantId"/>
/// is null in v1. The rules answer with a <see cref="Result"/> and never throw.
/// </summary>
public sealed class BaseText : TenantEntity
{
    private BaseText()
    {
    }

    /// <summary>What the curator calls the text, trimmed, 1 to 200 characters (BR1).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Title"/>: what the list search reads (BR11).</summary>
    public string NormalizedTitle { get; private set; } = string.Empty;

    /// <summary>The language the text is written in, canonical; never translated (BR1).</summary>
    public string ContentLanguage { get; private set; } = string.Empty;

    /// <summary>The sanitized HTML, never empty (BR1, BR8).</summary>
    public string TextHtml { get; private set; } = string.Empty;

    public static Result<BaseText> Create(string? title, string? contentLanguage, SanitizedHtml text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var baseText = new BaseText();
        var applied = baseText.Apply(title, contentLanguage, text, languageLocked: false);

        return applied.IsFailure ? Result.Failure<BaseText>(applied.Error!) : Result.Success(baseText);
    }

    /// <summary>
    /// Replaces the content. <paramref name="hasLinkedQuestions"/> locks the language (BR3): the shape of the
    /// content is judged first, so a bad title answers 400 before the lock answers 409. Nothing changes when it
    /// fails.
    /// </summary>
    public Result Update(string? title, string? contentLanguage, SanitizedHtml text, bool hasLinkedQuestions)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Apply(title, contentLanguage, text, languageLocked: hasLinkedQuestions);
    }

    private Result Apply(string? title, string? contentLanguage, SanitizedHtml text, bool languageLocked)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextTitleRequired, ErrorKind.Validation));
        }

        if (trimmed.Length > QuestionLimits.BaseTextTitleMaxLength)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextTitleTooLong, ErrorKind.Validation));
        }

        var language = SupportedLanguages.Canonical(contentLanguage);
        if (language is null)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextContentLanguageInvalid, ErrorKind.Validation));
        }

        if (text.IsEmpty)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextTextRequired, ErrorKind.Validation));
        }

        if (text.Html.Length > QuestionLimits.BaseTextMaxLength)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextTextTooLong, ErrorKind.Validation));
        }

        // F-103 BR4, BR7: at most ten images, each with its alternative text.
        if (QuestionImageRules.Check(
                [text],
                QuestionBankErrorCodes.BaseTextImageAltRequired,
                QuestionBankErrorCodes.BaseTextImageAltTooLong,
                QuestionBankErrorCodes.BaseTextImagesTooMany) is { } imageFault)
        {
            return Result.Failure(imageFault);
        }

        // BR3: the language of a text that questions use cannot change; the same language is not a change.
        if (languageLocked && language != ContentLanguage)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextLanguageInUse, ErrorKind.Conflict));
        }

        // Everything checked: from here on nothing can fail, so a refusal never leaves a half-written entity.
        Title = trimmed;
        NormalizedTitle = ComparableText.Normalize(trimmed);
        ContentLanguage = language;
        TextHtml = text.Html;

        return Result.Success();
    }
}
