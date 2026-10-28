using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Reads the content of an attempt for its owner only (F-169, BR2, BR11): anyone else, an <c>Admin</c> included, gets the
/// answer of an id that does not exist. The questions come at the versions the attempt fixed, their picture addresses
/// pointing at the attempt (BR12), grouped in sections (BR3).
/// </summary>
public sealed class GetAttemptContentHandler(IAttemptStore store, IAttemptSource source)
{
    /// <summary>The address the sanitizer writes for a stored picture (F-103); the only one a question's HTML can hold.</summary>
    private const string StoredAddress = "src=\"/question-images/";

    public async Task<Result<AttemptContentResponse>> HandleAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken)
    {
        // T2 of F-169: no reading of attempts.read here, unlike the F-166 read.
        var attempt = await store.FindAsync(attemptId, cancellationToken);
        if (attempt is null || attempt.StudentId != userId)
        {
            return Result.Failure<AttemptContentResponse>(new Error(ExamAttemptErrorCodes.NotFound, ErrorKind.NotFound));
        }

        var answers = attempt.Answers.OrderBy(answer => answer.PaperNumber).ToList();
        var contents = (await source.ListContentAsync(
                [.. answers.Select(answer => new AttemptVersion(answer.QuestionId, answer.VersionNumber))], cancellationToken))
            .ToDictionary(content => new AttemptVersion(content.QuestionId, content.VersionNumber));
        var address = $"src=\"/attempts/{attempt.Id}/images/";

        var questions = answers.Select(answer =>
        {
            // A version the attempt fixed cannot vanish without a bug elsewhere: fail loudly rather than drop a question.
            if (!contents.TryGetValue(new AttemptVersion(answer.QuestionId, answer.VersionNumber), out var content))
            {
                throw new InvalidOperationException(
                    $"Attempt {attempt.Id}: version {answer.VersionNumber} of question {answer.QuestionId} is not in the question bank.");
            }

            return new AttemptQuestionContentResponse(
                answer.QuestionId,
                answer.PaperNumber,
                answer.VersionNumber,
                answer.QuestionType,
                content.ContentLanguage,
                Point(content.StatementHtml, address),
                [.. content.Options.Select(option => option with { TextHtml = Point(option.TextHtml, address) })],
                content.BaseTextHtml is null ? null : Point(content.BaseTextHtml, address),
                content.NoticeSubjectId,
                answer.Answer);
        }).ToList();

        var labels = await source.FindNoticeSubjectLabelsAsync(
            [.. questions.Where(question => question.NoticeSubjectId is not null).Select(question => question.NoticeSubjectId!.Value).Distinct()],
            cancellationToken);
        var edition = await source.FindEditionLabelAsync(attempt.ExamEditionId, cancellationToken);
        if (edition is { ContentLanguage: null } && questions.Count > 0)
        {
            edition = edition with { ContentLanguage = questions[0].ContentLanguage };
        }

        return Result.Success(new AttemptContentResponse(
            attempt.Id,
            attempt.ExamEditionId,
            attempt.Status,
            attempt.StartedAt,
            AttemptSections.Build(questions, labels),
            questions,
            edition,
            attempt.SubmittedAt));
    }

    private static string Point(string html, string address) => html.Replace(StoredAddress, address, StringComparison.Ordinal);
}
