using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169: the Api behind the Web's typed <c>ExamEngineApiClient</c>. The test says what the Api answers (the content of an
/// attempt, a refusal, a held save) and reads what the page asked for. The rules behind the answers (who may read, what a
/// valid answer is) are the Api's and are tested there.
/// </summary>
public sealed class FakeExamEngineApi : HttpMessageHandler
{
    private const string Base = "/api/v1/exam-engine/attempts";

    /// <summary>What the content read answers; the attempt the page opens.</summary>
    public AttemptContentResponse? Content { get; set; }

    /// <summary>When set, the content read answers this problem (an attempt that is not the user's answers 404).</summary>
    public (HttpStatusCode Status, string Code)? ContentFailure { get; set; }

    /// <summary>When set, a save or a clear answers this problem and stores nothing.</summary>
    public (HttpStatusCode Status, string Code)? SaveFailure { get; set; }

    /// <summary>When set, the submit answers this problem.</summary>
    public (HttpStatusCode Status, string Code)? SubmitFailure { get; set; }

    /// <summary>When set, the start answers this problem.</summary>
    public (HttpStatusCode Status, string Code)? StartFailure { get; set; }

    /// <summary>The id of the attempt the start answers.</summary>
    public Guid StartedAttemptId { get; set; } = AttemptSamples.AttemptId;

    /// <summary>When set, the content read waits for it: the page's loading state.</summary>
    public TaskCompletionSource? HoldContent { get; set; }

    /// <summary>When set, a save or a clear waits for it: the page while a save is in flight.</summary>
    public TaskCompletionSource? HoldSave { get; set; }

    /// <summary>When set, the start waits for it: the button while the start is in flight.</summary>
    public TaskCompletionSource? HoldStart { get; set; }

    public List<(HttpMethod Method, string Path, string? Body)> Received { get; } = [];

    public IEnumerable<(HttpMethod Method, string Path, string? Body)> Writes =>
        Received.Where(call => call.Method != HttpMethod.Get);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Received.Add((request.Method, path, body));

        if (path == Base && request.Method == HttpMethod.Post)
        {
            return await StartAsync(body, cancellationToken);
        }

        if (path.EndsWith("/content", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
        {
            if (HoldContent is { } hold)
            {
                await hold.Task.WaitAsync(cancellationToken);
            }

            return ContentFailure is { } refused ? Problem(refused) : Json(Content);
        }

        if (path.Contains("/answers/", StringComparison.Ordinal))
        {
            if (HoldSave is { } hold)
            {
                await hold.Task.WaitAsync(cancellationToken);
            }

            if (SaveFailure is { } refused)
            {
                return Problem(refused);
            }

            var questionId = Guid.Parse(path[(path.LastIndexOf('/') + 1)..]);
            if (request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            var stored = Content!.Questions.First(question => question.QuestionId == questionId);
            return Json(new AttemptQuestionResponse(
                questionId,
                stored.PaperNumber,
                stored.VersionNumber,
                stored.Type,
                JsonSerializer.Deserialize<AnswerPayload>(body!, AppJson.Options)));
        }

        if (path.EndsWith("/submit", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
        {
            return SubmitFailure is { } refused
                ? Problem(refused)
                : Json(new AttemptResponse(AttemptSamples.AttemptId, AttemptSamples.EditionId, AttemptStatus.Submitted, DateTimeOffset.UtcNow, [], DateTimeOffset.UtcNow));
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> StartAsync(string? body, CancellationToken cancellationToken)
    {
        if (HoldStart is { } hold)
        {
            await hold.Task.WaitAsync(cancellationToken);
        }

        if (StartFailure is { } refused)
        {
            return Problem(refused);
        }

        var editionId = JsonSerializer.Deserialize<StartAttemptRequest>(body!, AppJson.Options)!.ExamEditionId ?? Guid.Empty;
        return Json(new AttemptResponse(StartedAttemptId, editionId, AttemptStatus.InProgress, DateTimeOffset.UtcNow, []), HttpStatusCode.Created);
    }

    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value, options: AppJson.Options) };

    private static HttpResponseMessage Problem((HttpStatusCode Status, string Code) failure) =>
        new(failure.Status)
        {
            Content = JsonContent.Create(
                new Dictionary<string, object> { ["status"] = (int)failure.Status, ["code"] = failure.Code },
                options: AppJson.Options)
        };
}
