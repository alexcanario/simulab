using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// A refusal about the options, with the position of the option it is about when there is one. The handler
/// puts the position on the problem so the form can mark the right row (F-95 decision of 2026-10-10).
/// </summary>
/// <param name="Error">The refusal.</param>
/// <param name="Position">The option's position, from 0; null when the refusal is about the list.</param>
public sealed record OptionFault(Error Error, int? Position);

/// <summary>
/// The rules about a question's options (F-95, BR3 and BR4), in one place so the entity and the handler agree:
/// the handler needs the position of a refused option, the entity is the authority.
/// </summary>
public static class QuestionRules
{
    /// <summary>
    /// False only when <paramref name="questionLanguage"/> is a language the app knows and it differs from the
    /// base text's (F-102, BR3). A language the app does not know is not a mismatch: the question's own rule
    /// refuses it with its own code, and that answer comes first.
    /// </summary>
    public static bool IsInBaseTextLanguage(string? questionLanguage, string baseTextLanguage)
    {
        var language = SupportedLanguages.Canonical(questionLanguage);

        return language is null || language == baseTextLanguage;
    }

    /// <summary>
    /// The first thing wrong with <paramref name="options"/>, or null. A draft only needs up to five options
    /// within their length; a published question also needs two to five with text and exactly one key.
    /// </summary>
    public static OptionFault? CheckOptions(IReadOnlyList<OptionDraft> options, bool published)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count > QuestionLimits.OptionsMax || (published && options.Count < QuestionLimits.OptionsMin))
        {
            return Fault(QuestionBankErrorCodes.QuestionOptionsCount, null);
        }

        var fault = CheckEachOption(options, published);
        if (fault is not null || !published)
        {
            return fault;
        }

        return options.Count(option => option.IsCorrect) == 1
            ? null
            : Fault(QuestionBankErrorCodes.QuestionKeyInvalid, null);
    }

    /// <summary>
    /// The first thing wrong with the options of a multiple-answer question, or null (F-104, BR3, BR4). A draft
    /// needs up to five options within their length, whatever is marked; a published question also needs three
    /// to five with text, at least two correct and at least one wrong.
    /// </summary>
    public static OptionFault? CheckMultipleAnswerOptions(IReadOnlyList<OptionDraft> options, bool published)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count > QuestionLimits.OptionsMax)
        {
            return Fault(QuestionBankErrorCodes.QuestionOptionsCount, null);
        }

        if (published && options.Count < QuestionLimits.MultipleAnswerOptionsMin)
        {
            return Fault(QuestionBankErrorCodes.QuestionMultipleOptionsCount, null);
        }

        var fault = CheckEachOption(options, published);
        if (fault is not null || !published)
        {
            return fault;
        }

        var correct = options.Count(option => option.IsCorrect);

        return correct >= QuestionLimits.MultipleAnswerCorrectMin && correct < options.Count
            ? null
            : Fault(QuestionBankErrorCodes.QuestionMultipleKeyInvalid, null);
    }

    // What every type with options shares (F-97 BR5): each option within its length and, to publish, with text.
    private static OptionFault? CheckEachOption(IReadOnlyList<OptionDraft> options, bool published)
    {
        for (var position = 0; position < options.Count; position++)
        {
            if (options[position].Text.Html.Length > QuestionLimits.OptionMaxLength)
            {
                return Fault(QuestionBankErrorCodes.QuestionOptionTooLong, position);
            }
        }

        if (!published)
        {
            return null;
        }

        for (var position = 0; position < options.Count; position++)
        {
            if (options[position].Text.IsEmpty)
            {
                return Fault(QuestionBankErrorCodes.QuestionOptionTextRequired, position);
            }
        }

        return null;
    }

    private static OptionFault Fault(string code, int? position) =>
        new(new Error(code, ErrorKind.Validation), position);
}
