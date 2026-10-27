using Simulab.ExamEngine.Contracts;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// The sections of a paper (F-169, BR3): one per notice subject of the questions, each with its questions in paper-number
/// order, ordered by their lowest paper number; the questions with no notice subject form a last section.
/// </summary>
public static class AttemptSections
{
    /// <summary>The sections of these questions; <paramref name="labels"/> names each notice subject.</summary>
    public static IReadOnlyList<AttemptSectionResponse> Build(
        IReadOnlyList<AttemptQuestionContentResponse> questions,
        IReadOnlyDictionary<Guid, string> labels)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(labels);

        var withSubject = questions
            .Where(question => question.NoticeSubjectId is not null)
            .GroupBy(question => question.NoticeSubjectId!.Value)
            .Select(group => SectionOf([.. group], group.Key, labels.GetValueOrDefault(group.Key)))
            .OrderBy(section => section.FirstPaperNumber);
        var others = questions.Where(question => question.NoticeSubjectId is null).ToList();

        return others.Count == 0 ? [.. withSubject] : [.. withSubject, SectionOf(others, null, null)];
    }

    private static AttemptSectionResponse SectionOf(List<AttemptQuestionContentResponse> questions, Guid? noticeSubjectId, string? label)
    {
        var ordered = questions.OrderBy(question => question.PaperNumber).ToList();

        return new AttemptSectionResponse(
            ordered[0].PaperNumber,
            ordered[^1].PaperNumber,
            [.. ordered.Select(question => question.QuestionId)],
            noticeSubjectId,
            label);
    }
}
