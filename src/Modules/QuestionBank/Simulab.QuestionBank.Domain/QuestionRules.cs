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

    /// <summary>
    /// The first thing wrong with the two columns of a matching question, or null (F-106, BR3, BR4). Any draft: at most
    /// eight on each side, each within its length, and no match naming a missing or an already used option. To
    /// publish: two to eight left items, at least as many options, every text written and every left item matched.
    /// The position of a fault about one item is the item's in its own column.
    /// </summary>
    public static OptionFault? CheckMatching(IReadOnlyList<OptionDraft> options, IReadOnlyList<MatchItemDraft> items, bool published)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count > QuestionLimits.MatchingItemsMax || options.Count > QuestionLimits.MatchingItemsMax
            || (published && (items.Count < QuestionLimits.MatchingItemsMin || options.Count < items.Count)))
        {
            return Fault(QuestionBankErrorCodes.QuestionMatchingItemsCount, null);
        }

        // BR3: a missing or a repeated letter is broken data, refused on a draft too; an unset one is unfinished work.
        var used = new HashSet<int>();
        for (var position = 0; position < items.Count; position++)
        {
            if (items[position].MatchPosition is not { } match)
            {
                continue;
            }

            if (match < 0 || match >= options.Count)
            {
                return Fault(QuestionBankErrorCodes.QuestionMatchingKeyInvalid, position);
            }

            if (!used.Add(match))
            {
                return Fault(QuestionBankErrorCodes.QuestionMatchingKeyRepeated, position);
            }
        }

        var fault = CheckEachOption(options, published)
            ?? CheckEach(
                [.. items.Select(item => item.Text)],
                published,
                QuestionBankErrorCodes.QuestionMatchItemTooLong,
                QuestionBankErrorCodes.QuestionMatchItemTextRequired);
        if (fault is not null || !published)
        {
            return fault;
        }

        var unmatched = items.ToList().FindIndex(item => item.MatchPosition is null);

        return unmatched < 0 ? null : Fault(QuestionBankErrorCodes.QuestionMatchingKeyInvalid, unmatched);
    }

    // What every type with options shares (F-97 BR5): each option within its length and, to publish, with text.
    private static OptionFault? CheckEachOption(IReadOnlyList<OptionDraft> options, bool published) =>
        CheckEach(
            [.. options.Select(option => option.Text)],
            published,
            QuestionBankErrorCodes.QuestionOptionTooLong,
            QuestionBankErrorCodes.QuestionOptionTextRequired);

    // One rule for a list of short rich texts, options or left items (F-106 BR7): each within the option length
    // and, to publish, with text; the codes are the list's own.
    private static OptionFault? CheckEach(IReadOnlyList<SanitizedHtml> texts, bool published, string tooLongCode, string requiredCode)
    {
        for (var position = 0; position < texts.Count; position++)
        {
            if (texts[position].Html.Length > QuestionLimits.OptionMaxLength)
            {
                return Fault(tooLongCode, position);
            }
        }

        if (!published)
        {
            return null;
        }

        for (var position = 0; position < texts.Count; position++)
        {
            if (texts[position].IsEmpty)
            {
                return Fault(requiredCode, position);
            }
        }

        return null;
    }

    private static OptionFault Fault(string code, int? position) =>
        new(new Error(code, ErrorKind.Validation), position);
}
