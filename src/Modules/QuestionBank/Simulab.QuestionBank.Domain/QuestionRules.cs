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

        return options.Count(option => option.IsCorrect) == 1
            ? null
            : Fault(QuestionBankErrorCodes.QuestionKeyInvalid, null);
    }

    private static OptionFault Fault(string code, int? position) =>
        new(new Error(code, ErrorKind.Validation), position);
}
