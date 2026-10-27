using Simulab.ExamEngine.Contracts;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169: an attempt as the content read answers it. Six questions in paper order with a numbering that jumps (5 was
/// annulled): 1 to 3 in "Língua Portuguesa", 4 and 6 in "Legislação", 7 with no notice subject, which is the last
/// section. Question 1 has a base text, 2 is true or false, 3 has several answers, 4 is a type this item cannot answer.
/// </summary>
internal static class AttemptSamples
{
    internal static readonly Guid AttemptId = Guid.Parse("0198f0a3-5000-7000-8000-000000000001");
    internal static readonly Guid EditionId = Guid.Parse("0198f0a3-5000-7000-8000-000000000002");
    internal static readonly Guid ExamId = Guid.Parse("0198f0a3-5000-7000-8000-000000000003");
    internal static readonly Guid PortugueseId = Guid.Parse("0198f0a3-5000-7000-8000-000000000011");
    internal static readonly Guid LawId = Guid.Parse("0198f0a3-5000-7000-8000-000000000012");

    internal static readonly Guid Q1 = Guid.Parse("0198f0a3-5000-7000-8000-0000000000a1");
    internal static readonly Guid Q2 = Guid.Parse("0198f0a3-5000-7000-8000-0000000000a2");
    internal static readonly Guid Q3 = Guid.Parse("0198f0a3-5000-7000-8000-0000000000a3");
    internal static readonly Guid Q4 = Guid.Parse("0198f0a3-5000-7000-8000-0000000000a4");
    internal static readonly Guid Q6 = Guid.Parse("0198f0a3-5000-7000-8000-0000000000a6");
    internal static readonly Guid Q7 = Guid.Parse("0198f0a3-5000-7000-8000-0000000000a7");

    internal const string EditionLabel = "Guarda Municipal 2026 · CONSULPLAN";

    internal static AttemptEditionResponse Edition { get; } =
        new(ExamId, "Guarda Municipal", "Instituto Consulplan", "CONSULPLAN", 2026, ContentLanguage: "pt-BR");

    private static IReadOnlyList<AttemptOptionResponse> Options(string prefix) =>
        [.. Enumerable.Range(0, 4).Select(position => new AttemptOptionResponse(position, $"<p>{prefix} {position + 1}</p>"))];

    private static AttemptQuestionContentResponse Single(Guid id, int paper, Guid? subject, AnswerPayload? answer = null, string? baseText = null) =>
        new(id, paper, 1, AttemptQuestionType.SingleChoice, "pt-BR", $"<p>Statement {paper}</p>", Options($"Option {paper}"), baseText, subject, answer);

    /// <summary>
    /// The attempt in progress. <paramref name="answers"/> says which paper numbers already hold an answer, as a resumed
    /// attempt does.
    /// </summary>
    internal static AttemptContentResponse InProgress(IReadOnlyDictionary<int, AnswerPayload>? answers = null, bool withEdition = true)
    {
        AnswerPayload? AnswerOf(int paper) => answers is not null && answers.TryGetValue(paper, out var answer) ? answer : null;

        IReadOnlyList<AttemptQuestionContentResponse> questions =
        [
            Single(Q1, 1, PortugueseId, AnswerOf(1), baseText: "<p>The base text of question 1.</p>"),
            new(Q2, 2, 1, AttemptQuestionType.TrueFalse, "pt-BR", "<p>Statement 2</p>", [], null, PortugueseId, AnswerOf(2)),
            new(Q3, 3, 1, AttemptQuestionType.MultipleAnswer, "pt-BR", "<p>Statement 3</p>", Options("Several"), null, PortugueseId, AnswerOf(3)),
            new(Q4, 4, 1, AttemptQuestionType.Matching, "pt-BR", "<p>Statement 4, a matching question</p>", [], "<p>The base text of question 4.</p>", LawId, AnswerOf(4)),
            Single(Q6, 6, LawId, AnswerOf(6)),
            Single(Q7, 7, null, AnswerOf(7))
        ];

        IReadOnlyList<AttemptSectionResponse> sections =
        [
            new(1, 3, [Q1, Q2, Q3], PortugueseId, "Língua Portuguesa"),
            new(4, 6, [Q4, Q6], LawId, "Legislação"),
            new(7, 7, [Q7])
        ];

        return new AttemptContentResponse(
            AttemptId,
            EditionId,
            AttemptStatus.InProgress,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            sections,
            questions,
            withEdition ? Edition : null);
    }

    internal static AttemptContentResponse Submitted(IReadOnlyDictionary<int, AnswerPayload>? answers = null, bool withEdition = true) =>
        InProgress(answers, withEdition) with
        {
            Status = AttemptStatus.Submitted,
            SubmittedAt = new DateTimeOffset(2026, 10, 27, 18, 42, 0, TimeSpan.Zero)
        };
}
