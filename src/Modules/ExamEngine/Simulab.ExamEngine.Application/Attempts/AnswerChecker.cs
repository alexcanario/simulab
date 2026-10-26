using Simulab.ExamEngine.Contracts;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>One thing wrong in an answer: the member of the body and the stable code that says why (F-166, BR5).</summary>
public sealed record AnswerFieldError(string Field, string Code);

/// <summary>An answer that fits its question, with its line breaks made one kind, or the entries that say why it does not.</summary>
public sealed record AnswerCheck(AnswerPayload? Answer, IReadOnlyList<AnswerFieldError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Checks an answer against the question version the attempt fixed (F-166, BR5): the shape of the type, the positions and
/// numbers the version has, and the size limits. Nothing is stored when it finds anything. Typed text is kept as written,
/// with <c>\r\n</c> and <c>\r</c> made <c>\n</c>.
/// </summary>
public static class AnswerChecker
{
    public static AnswerCheck Check(AnswerShape shape, AnswerPayload answer)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(answer);

        var errors = new List<AnswerFieldError>();
        AnswerPayload checkedAnswer = (shape.Type, answer) switch
        {
            (AttemptQuestionType.SingleChoice, SingleChoicePayload single) => CheckSingle(shape, single, errors),
            (AttemptQuestionType.TrueFalse, TrueFalsePayload trueFalse) => CheckTrueFalse(trueFalse, errors),
            (AttemptQuestionType.MultipleAnswer, MultipleAnswerPayload multiple) => CheckMultiple(shape, multiple, errors),
            (AttemptQuestionType.Matching, MatchingPayload matching) => CheckMatching(shape, matching, errors),
            (AttemptQuestionType.FillInTheBlanks, FillInTheBlanksPayload blanks) => CheckBlanks(shape, blanks, errors),
            (AttemptQuestionType.ShortAnswer, ShortAnswerPayload text) => new ShortAnswerPayload(CheckText("text", text.Text, null, errors)),
            (AttemptQuestionType.OpenAnswer, OpenAnswerPayload text) => new OpenAnswerPayload(CheckText("text", text.Text, shape.SizeMax, errors)),
            (AttemptQuestionType.Essay, EssayPayload text) => new EssayPayload(CheckEssay(text.Text, shape.SizeMax, errors)),
            _ => Mismatch(errors, answer)
        };

        return new AnswerCheck(errors.Count == 0 ? checkedAnswer : null, errors);
    }

    private static AnswerPayload Mismatch(List<AnswerFieldError> errors, AnswerPayload answer)
    {
        errors.Add(new AnswerFieldError("type", ExamAttemptErrorCodes.AnswerTypeMismatch));

        return answer;
    }

    private static SingleChoicePayload CheckSingle(AnswerShape shape, SingleChoicePayload answer, List<AnswerFieldError> errors)
    {
        CheckPosition("optionPosition", answer.OptionPosition, shape.OptionPositions, ExamAttemptErrorCodes.OptionUnknown, errors);

        return answer;
    }

    private static TrueFalsePayload CheckTrueFalse(TrueFalsePayload answer, List<AnswerFieldError> errors)
    {
        if (answer.Value is null)
        {
            errors.Add(new AnswerFieldError("value", ExamAttemptErrorCodes.AnswerEmpty));
        }

        return answer;
    }

    private static MultipleAnswerPayload CheckMultiple(AnswerShape shape, MultipleAnswerPayload answer, List<AnswerFieldError> errors)
    {
        if (answer.OptionPositions is not { Count: > 0 } positions)
        {
            errors.Add(new AnswerFieldError("optionPositions", ExamAttemptErrorCodes.AnswerEmpty));

            return answer;
        }

        for (var index = 0; index < positions.Count; index++)
        {
            CheckPosition($"optionPositions[{index}]", positions[index], shape.OptionPositions, ExamAttemptErrorCodes.OptionUnknown, errors);
        }

        return new MultipleAnswerPayload([.. positions.Distinct()]);
    }

    private static MatchingPayload CheckMatching(AnswerShape shape, MatchingPayload answer, List<AnswerFieldError> errors)
    {
        if (answer.Pairs is not { Count: > 0 } pairs)
        {
            errors.Add(new AnswerFieldError("pairs", ExamAttemptErrorCodes.AnswerEmpty));

            return answer;
        }

        var seen = new HashSet<int>();
        for (var index = 0; index < pairs.Count; index++)
        {
            var pair = pairs[index];
            CheckPosition($"pairs[{index}].itemPosition", pair.ItemPosition, shape.ItemPositions, ExamAttemptErrorCodes.ItemUnknown, errors);
            CheckPosition($"pairs[{index}].optionPosition", pair.OptionPosition, shape.OptionPositions, ExamAttemptErrorCodes.OptionUnknown, errors);
            if (pair.ItemPosition is { } item && !seen.Add(item))
            {
                errors.Add(new AnswerFieldError($"pairs[{index}].itemPosition", ExamAttemptErrorCodes.EntryRepeated));
            }
        }

        return answer;
    }

    private static FillInTheBlanksPayload CheckBlanks(AnswerShape shape, FillInTheBlanksPayload answer, List<AnswerFieldError> errors)
    {
        if (answer.Blanks is not { Count: > 0 } blanks)
        {
            errors.Add(new AnswerFieldError("blanks", ExamAttemptErrorCodes.AnswerEmpty));

            return answer;
        }

        var seen = new HashSet<int>();
        var normalized = new List<BlankTextPayload>(blanks.Count);
        for (var index = 0; index < blanks.Count; index++)
        {
            var blank = blanks[index];
            CheckPosition($"blanks[{index}].number", blank.Number, shape.BlankNumbers, ExamAttemptErrorCodes.BlankUnknown, errors);
            if (blank.Number is { } number && !seen.Add(number))
            {
                errors.Add(new AnswerFieldError($"blanks[{index}].number", ExamAttemptErrorCodes.EntryRepeated));
            }

            normalized.Add(new BlankTextPayload(blank.Number, CheckText($"blanks[{index}].text", blank.Text, null, errors)));
        }

        return new FillInTheBlanksPayload(normalized);
    }

    private static void CheckPosition(
        string field,
        int? value,
        IReadOnlyList<int> known,
        string unknownCode,
        List<AnswerFieldError> errors)
    {
        if (value is null)
        {
            errors.Add(new AnswerFieldError(field, ExamAttemptErrorCodes.AnswerEmpty));
        }
        else if (!known.Contains(value.Value))
        {
            errors.Add(new AnswerFieldError(field, unknownCode));
        }
    }

    // The characters limit of an open answer is the question's own; the cap is the module's, for every type.
    private static string? CheckText(string field, string? text, int? maxCharacters, List<AnswerFieldError> errors)
    {
        if (text is null)
        {
            errors.Add(new AnswerFieldError(field, ExamAttemptErrorCodes.AnswerEmpty));

            return null;
        }

        var normalized = NormalizeLineBreaks(text);
        if (normalized.Length > AttemptLimits.TextMaxLength || (maxCharacters is { } max && normalized.Length > max))
        {
            errors.Add(new AnswerFieldError(field, ExamAttemptErrorCodes.TextTooLong));
        }

        return normalized;
    }

    // The limit of an essay is in lines; the minimum is judged at grading and never checked here.
    private static string? CheckEssay(string? text, int? maxLines, List<AnswerFieldError> errors)
    {
        var normalized = CheckText("text", text, null, errors);
        if (normalized is not null && maxLines is { } max && normalized.Count(character => character == '\n') + 1 > max
            && !errors.Any(error => error.Field == "text" && error.Code == ExamAttemptErrorCodes.TextTooLong))
        {
            errors.Add(new AnswerFieldError("text", ExamAttemptErrorCodes.TextTooLong));
        }

        return normalized;
    }

    private static string NormalizeLineBreaks(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
