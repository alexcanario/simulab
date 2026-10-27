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
        IReadOnlyDictionary<Guid, string> labels) =>
        throw new NotImplementedException();
}
