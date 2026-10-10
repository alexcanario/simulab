using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-95's back office pages, signed in as a curator holding <c>questions.manage</c>, with a fake Api behind both
/// typed clients (the question bank and the catalog the form's pickers read). The test says what the Api answers
/// and reads what the page shows and what it sent; the rules themselves are the Api's and have their own tests.
/// </summary>
public abstract partial class QuestionPageTestContext : KitTestContext
{
    private const string WebSessionId = "web-questions";

    protected static readonly SubjectResponse Portuguese = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000a1"), "Português", null, null, 2);

    protected static readonly SubjectResponse Law = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000a2"), "Direito Constitucional", null, null, 1);

    protected static readonly TopicResponse Crase = new(Guid.Parse("0198f5a0-0000-7000-8000-0000000000b1"), Portuguese.Id, "Crase");

    protected static readonly TopicResponse Punctuation = new(Guid.Parse("0198f5a0-0000-7000-8000-0000000000b2"), Portuguese.Id, "Pontuação");

    protected static readonly TopicResponse FundamentalRights = new(Guid.Parse("0198f5a0-0000-7000-8000-0000000000b3"), Law.Id, "Direitos fundamentais");

    /// <summary>A draft with two options and no key: it appears in the list as a draft and cannot be published yet.</summary>
    protected static readonly QuestionResponse Draft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000001"),
        QuestionType.SingleChoice,
        QuestionStatus.Draft,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Qual frase usa a crase corretamente?</p>",
        null,
        null,
        3,
        [new(0, "<p>Fui <strong>à</strong> escola.</p>", false), new(1, "<p>Fui a escola.</p>", false)]);

    /// <summary>A published question with a base text, a key and an explanation.</summary>
    protected static readonly QuestionResponse Published = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000002"),
        QuestionType.SingleChoice,
        QuestionStatus.Published,
        Punctuation.Id,
        Punctuation.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-PT",
        "<p>Onde entra a vírgula?</p>",
        "<p>Texto-base com <sup>nota</sup>.</p>",
        "<p>Porque separa o vocativo.</p>",
        null,
        [new(0, "<p>Antes do vocativo</p>", true), new(1, "<p>Depois do verbo</p>", false), new(2, "<p>Nunca</p>", false)]);

    protected FakeQuestionBankApi Api { get; } = new();

    protected QuestionPageTestContext()
    {
        var questionBankClient = new HttpClient(Api, disposeHandler: false) { BaseAddress = new Uri("https://api.test") };
        var catalogClient = new HttpClient(Api, disposeHandler: false) { BaseAddress = new Uri("https://api.test") };
        Services.AddSingleton(new QuestionBankApiClient(questionBankClient));
        Services.AddSingleton(new CatalogApiClient(catalogClient));

        var store = new InMemoryWebSessionStore();
        store.SaveAsync(
            WebSessionId,
            new WebSession(
                "jti-questions",
                "access-1",
                "refresh-1",
                DateTimeOffset.UtcNow.AddMinutes(10),
                [QuestionBankPermissions.Manage],
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("curator@exemplo.com")
            .SetClaims(
                new Claim(WebAuthClaims.WebSessionId, WebSessionId),
                new Claim(WebAuthClaims.Permission, QuestionBankPermissions.Manage));
    }

    /// <summary>The providers a page's dialogs, tooltips and snackbars render into.</summary>
    protected (IRenderedComponent<MudDialogProvider> Dialogs, IRenderedComponent<MudSnackbarProvider> Snackbars) RenderProviders()
    {
        Render<MudPopoverProvider>();
        return (Render<MudDialogProvider>(), Render<MudSnackbarProvider>());
    }

    /// <summary>Answers the question bank routes and the catalog routes the pickers read, and records what it received.</summary>
    protected sealed partial class FakeQuestionBankApi : HttpMessageHandler
    {
        private const string QuestionsPath = "/api/v1/question-bank/questions";

        /// <summary>The stored questions, newest first: the test sets it, the writes change it.</summary>
        public List<QuestionResponse> Questions { get; } = [];

        public List<SubjectResponse> Subjects { get; } = [Portuguese, Law];

        public List<TopicResponse> Topics { get; } = [Crase, Punctuation, FundamentalRights];

        /// <summary>When set, listing the questions answers this problem (the list's load error).</summary>
        public (HttpStatusCode Status, string Code)? ListFailure { get; set; }

        /// <summary>When set, reading one question answers this problem (the form's not-found and load-error states).</summary>
        public (HttpStatusCode Status, string Code)? FindFailure { get; set; }

        /// <summary>When set, a create or an update answers this problem, with the option position when given.</summary>
        public (HttpStatusCode Status, string Code, int? Position)? SaveFailure { get; set; }

        /// <summary>When set, a publish or an unpublish answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? PublicationFailure { get; set; }

        /// <summary>When set, a delete answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? DeleteFailure { get; set; }

        /// <summary>When set, every write waits for it before it is answered (the screen while a save is in flight).</summary>
        public TaskCompletionSource? WriteGate { get; set; }

        public List<(HttpMethod Method, string Path, string? Query, string? Body)> Received { get; } = [];

        public int ListCalls => Received.Count(call => call.Method == HttpMethod.Get && call.Path == QuestionsPath);

        public SaveQuestionRequest LastSaved(HttpMethod method) =>
            Read<SaveQuestionRequest>(Received.Last(call => call.Method == method && call.Path.StartsWith(QuestionsPath, StringComparison.Ordinal)).Body);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var query = request.RequestUri.Query;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Received.Add((request.Method, path, query, body));

            if (request.Method != HttpMethod.Get && WriteGate is { } gate)
            {
                await gate.Task;
            }

            if (path.StartsWith("/api/v1/catalog/subjects", StringComparison.Ordinal))
            {
                return HandleCatalog(path, query);
            }

            if (!path.StartsWith(QuestionsPath, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var rest = path[QuestionsPath.Length..].Trim('/');
            var segments = rest.Length == 0 ? [] : rest.Split('/');

            return (segments.Length, request.Method.Method) switch
            {
                (0, "GET") => List(query),
                (0, "POST") => Create(body),
                (1, "GET") => Find(Guid.Parse(segments[0])),
                (1, "PUT") => Update(Guid.Parse(segments[0]), body),
                (1, "DELETE") => Delete(Guid.Parse(segments[0])),
                (2, "POST") when segments[1] == "publish" => SetStatus(Guid.Parse(segments[0]), QuestionStatus.Published),
                (2, "POST") when segments[1] == "unpublish" => SetStatus(Guid.Parse(segments[0]), QuestionStatus.Draft),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        private HttpResponseMessage HandleCatalog(string path, string query)
        {
            var segments = path["/api/v1/catalog/subjects".Length..].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                var term = System.Web.HttpUtility.ParseQueryString(query)["search"];
                var found = Subjects
                    .Where(subject => string.IsNullOrEmpty(term) || subject.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Json(new SubjectPageResponse(found, found.Count));
            }

            if (segments.Length == 2 && segments[1] == "topics")
            {
                var subjectId = Guid.Parse(segments[0]);
                return Json(Topics.Where(topic => topic.SubjectId == subjectId).ToList());
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private HttpResponseMessage List(string query)
        {
            if (ListFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var parameters = System.Web.HttpUtility.ParseQueryString(query);
            IEnumerable<QuestionResponse> rows = Questions;
            if (Enum.TryParse<QuestionStatus>(parameters["status"], ignoreCase: true, out var status))
            {
                rows = rows.Where(row => row.Status == status);
            }

            if (parameters["search"] is { Length: > 0 } term)
            {
                rows = rows.Where(row => PlainText(row.StatementHtml).Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var all = rows.ToList();
            var size = int.Parse(parameters["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
            var page = int.Parse(parameters["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var items = all.Skip(page * size).Take(size).Select(ToListItem).ToList();

            return Json(new QuestionPageResponse(items, all.Count));
        }

        private static QuestionListItemResponse ToListItem(QuestionResponse question) =>
            new(
                question.Id,
                PlainText(question.StatementHtml),
                question.TopicId,
                question.TopicName,
                question.SubjectId,
                question.SubjectName,
                question.Difficulty,
                question.ContentLanguage,
                question.Status);

        private HttpResponseMessage Find(Guid id)
        {
            if (FindFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var question = Questions.Find(candidate => candidate.Id == id);

            return question is null
                ? Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound)
                : Json(question);
        }

        private HttpResponseMessage Create(string? body)
        {
            if (SaveFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code, refused.Position);
            }

            var created = Build(Guid.CreateVersion7(), Read<SaveQuestionRequest>(body), QuestionStatus.Draft);
            Questions.Insert(0, created);

            return Json(created, HttpStatusCode.Created);
        }

        private HttpResponseMessage Update(Guid id, string? body)
        {
            if (SaveFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code, refused.Position);
            }

            var existing = Questions.Find(candidate => candidate.Id == id);
            if (existing is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var updated = Build(id, Read<SaveQuestionRequest>(body), existing.Status);
            Questions[Questions.IndexOf(existing)] = updated;

            return Json(updated);
        }

        private HttpResponseMessage SetStatus(Guid id, QuestionStatus status)
        {
            if (PublicationFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var existing = Questions.Find(candidate => candidate.Id == id);
            if (existing is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var changed = existing with { Status = status };
            Questions[Questions.IndexOf(existing)] = changed;

            return Json(changed);
        }

        private HttpResponseMessage Delete(Guid id)
        {
            if (DeleteFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var existing = Questions.Find(candidate => candidate.Id == id);
            if (existing is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            if (existing.Status == QuestionStatus.Published)
            {
                return Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
            }

            Questions.Remove(existing);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        // The Api joins the topic and subject names and sanitizes every text; the fake does both in the smallest way
        // that lets a test see "the stored text replaces what the editor held".
        private QuestionResponse Build(Guid id, SaveQuestionRequest request, QuestionStatus status)
        {
            var topic = Topics.First(candidate => candidate.Id == request.TopicId);
            var subject = Subjects.First(candidate => candidate.Id == topic.SubjectId);

            return new QuestionResponse(
                id,
                QuestionType.SingleChoice,
                status,
                topic.Id,
                topic.Name,
                subject.Id,
                subject.Name,
                request.ContentLanguage!,
                Sanitize(request.StatementHtml) ?? string.Empty,
                Sanitize(request.BaseTextHtml),
                Sanitize(request.ExplanationHtml),
                request.Difficulty,
                [.. (request.Options ?? []).Select((option, position) =>
                    new QuestionOptionResponse(position, Sanitize(option.TextHtml) ?? string.Empty, option.IsCorrect))]);
        }

        private static string? Sanitize(string? html) =>
            html is null ? null : ScriptPattern().Replace(html, string.Empty);

        private static string PlainText(string? html) =>
            WebUtility.HtmlDecode(TagPattern().Replace(html ?? string.Empty, " ")).Trim();

        [GeneratedRegex(@"<script\b[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ScriptPattern();

        [GeneratedRegex("<[^>]*>")]
        private static partial Regex TagPattern();

        public static T Read<T>(string? body) => System.Text.Json.JsonSerializer.Deserialize<T>(body!, AppJson.Options)!;

        private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = JsonContent.Create(value, options: AppJson.Options) };

        private static HttpResponseMessage Problem(HttpStatusCode status, string code, int? position = null)
        {
            var body = new Dictionary<string, object> { ["status"] = (int)status, ["code"] = code };
            if (position is { } at)
            {
                body["position"] = at;
            }

            return new HttpResponseMessage(status) { Content = JsonContent.Create(body, options: AppJson.Options) };
        }
    }
}
