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
/// F-95's back office pages, signed in as a curator holding <c>questions.manage</c>, with a fake Api behind the
/// typed client of the question bank, whose lookups feed the form's pickers. The test says what the Api answers
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

    /// <summary>F-96: a past edition the origin lookup offers.</summary>
    protected static readonly ExamEditionLookupResponse GuardEdition = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000e1"), "Guarda Municipal", "CONSULPLAN", 2024, "Guarda", "Published");

    protected static readonly ExamEditionLookupResponse OtherEdition = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000e2"), "Professor de História", "FGV", 2023, null, "Draft");

    protected static readonly NoticeSubjectLookupResponse Basic = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000f1"), "Basic knowledge", "Portuguese", 10);

    protected static readonly NoticeSubjectLookupResponse Specific = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000f2"), "Specific knowledge", "Constitutional law", 10);

    protected static readonly NoticeSubjectLookupResponse OtherSubject = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000f3"), "Basic knowledge", "History", 20);

    /// <summary>A draft that came from the guard edition: number 12 under its first notice subject.</summary>
    protected static readonly QuestionResponse FromGuard = Draft with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000020"),
        StatementHtml = "<p>Fiscal de posturas: qual a competência?</p>",
        Origin = new QuestionOriginResponse(
            GuardEdition.Id,
            Guid.Parse("0198f5a0-0000-7000-8000-0000000000c1"),
            GuardEdition.ExamName,
            GuardEdition.OrganizerAcronym,
            "Consulplan",
            GuardEdition.NoticeYear,
            GuardEdition.Position,
            12,
            Basic.Id,
            Basic.Label)
    };

    protected static readonly QuestionResponse FromGuardSecond = FromGuard with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000021"),
        StatementHtml = "<p>Outra questão da mesma prova.</p>",
        Origin = FromGuard.Origin! with { PaperNumber = 30, NoticeSubjectId = null, NoticeSubjectLabel = null }
    };

    protected FakeQuestionBankApi Api { get; } = new();

    protected QuestionPageTestContext()
    {
        var questionBankClient = new HttpClient(Api, disposeHandler: false) { BaseAddress = new Uri("https://api.test") };
        Services.AddSingleton(new QuestionBankApiClient(questionBankClient));

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

    /// <summary>
    /// Answers the question bank routes, the pickers' lookups included, and records what it received. A route of
    /// the catalog answers 403, as it does for a curator, so a page that reaches for one fails (B-28).
    /// </summary>
    protected sealed partial class FakeQuestionBankApi : HttpMessageHandler
    {
        private const string QuestionsPath = "/api/v1/question-bank/questions";
        private const string EditionsPath = "/api/v1/question-bank/editions";
        private const string SubjectsPath = "/api/v1/question-bank/subjects";
        private const string TaxonomyPath = "/api/v1/question-bank/taxonomy";
        private const string OrganizersPath = "/api/v1/question-bank/organizers";
        private const string CatalogPath = "/api/v1/catalog/";

        /// <summary>The stored questions, newest first: the test sets it, the writes change it.</summary>
        public List<QuestionResponse> Questions { get; } = [];

        public List<SubjectResponse> Subjects { get; } = [Portuguese, Law];

        public List<TopicResponse> Topics { get; } = [Crase, Punctuation, FundamentalRights];

        /// <summary>F-96: the editions the origin lookup knows.</summary>
        public List<ExamEditionLookupResponse> Editions { get; } = [];

        /// <summary>F-100: the organizers the list's organizer filter offers.</summary>
        public List<OrganizerLookupResponse> Organizers { get; } = [];

        /// <summary>F-100: when set, the taxonomy of the list's subject filter answers 500 (its load error).</summary>
        public bool TaxonomyFails { get; set; }

        /// <summary>F-96: the notice subjects of each edition, in display order.</summary>
        public Dictionary<Guid, List<NoticeSubjectLookupResponse>> NoticeSubjects { get; } = [];

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

        /// <summary>F-98: the versions of each question, newest first: the test sets them.</summary>
        public Dictionary<Guid, List<QuestionVersionResponse>> Versions { get; } = [];

        /// <summary>F-98 BR5: when set, a publish with no reason in its body answers 400 <c>question.key_change_reason_required</c>.</summary>
        public bool PublishNeedsReason { get; set; }

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

            if (path.StartsWith(CatalogPath, StringComparison.Ordinal))
            {
                return Problem(HttpStatusCode.Forbidden, "identity.forbidden");
            }

            if (path == TaxonomyPath)
            {
                return TaxonomyFails ? Problem(HttpStatusCode.InternalServerError, "common.unexpected_error") : Json(TaxonomyOf());
            }

            if (path == OrganizersPath)
            {
                return Json(Organizers.OrderBy(organizer => organizer.Acronym, StringComparer.OrdinalIgnoreCase).ToList());
            }

            if (path.StartsWith(SubjectsPath, StringComparison.Ordinal))
            {
                return HandleSubjects(path, query);
            }

            if (path.StartsWith(EditionsPath, StringComparison.Ordinal))
            {
                return HandleEditions(path, query);
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
                (2, "GET") when segments[1] == "versions" => ListVersions(Guid.Parse(segments[0])),
                (3, "GET") when segments[1] == "versions" => FindVersion(Guid.Parse(segments[0]), int.Parse(segments[2], System.Globalization.CultureInfo.InvariantCulture)),
                (2, "POST") when segments[1] == "publish" && PublishNeedsReason && !HasReason(body) =>
                    Problem(HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionKeyChangeReasonRequired),
                (2, "POST") when segments[1] == "publish" => SetStatus(Guid.Parse(segments[0]), QuestionStatus.Published),
                (2, "POST") when segments[1] == "unpublish" => SetStatus(Guid.Parse(segments[0]), QuestionStatus.Draft),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        private static bool HasReason(string? body) =>
            !string.IsNullOrWhiteSpace(body)
            && !string.IsNullOrWhiteSpace(Read<PublishQuestionRequest>(body).ChangeReason);

        private HttpResponseMessage ListVersions(Guid questionId)
        {
            if (!Questions.Exists(candidate => candidate.Id == questionId))
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var versions = Versions.TryGetValue(questionId, out var found) ? found : [];
            var ordered = versions.OrderByDescending(version => version.Number).ToList();

            return Json(ordered
                .Select((version, index) => new QuestionVersionListItemResponse(
                    version.Id, version.Number, version.CreatedAt, version.AnswerKeyChanged, version.Reason, index == 0))
                .ToList());
        }

        private HttpResponseMessage FindVersion(Guid questionId, int number)
        {
            if (!Questions.Exists(candidate => candidate.Id == questionId))
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var version = Versions.TryGetValue(questionId, out var found)
                ? found.Find(candidate => candidate.Number == number)
                : null;

            return version is null
                ? Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionVersionNotFound)
                : Json(version);
        }

        private HttpResponseMessage HandleEditions(string path, string query)
        {
            var segments = path[EditionsPath.Length..].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                var term = System.Web.HttpUtility.ParseQueryString(query)["search"];

                return Json(Editions
                    .Where(edition => string.IsNullOrEmpty(term)
                        || edition.ExamName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || edition.OrganizerAcronym.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .ToList());
            }

            var editionId = Guid.Parse(segments[0]);
            if (!Editions.Exists(edition => edition.Id == editionId))
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionExamEditionNotFound);
            }

            if (segments.Length == 1)
            {
                return Json(Editions.Single(edition => edition.Id == editionId));
            }

            if (segments[1] == "notice-subjects")
            {
                return Json(NoticeSubjects.TryGetValue(editionId, out var subjects) ? subjects : []);
            }

            var parameters = System.Web.HttpUtility.ParseQueryString(query);
            var size = int.Parse(parameters["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
            var page = int.Parse(parameters["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var ordered = Questions
                .Where(question => question.Origin?.ExamEditionId == editionId)
                .OrderBy(question => question.Origin!.PaperNumber)
                .ToList();
            var items = ordered.Skip(page * size).Take(size)
                .Select(question => new EditionQuestionListItemResponse(
                    question.Id,
                    question.Origin!.PaperNumber,
                    PlainText(question.StatementHtml),
                    question.Origin.NoticeSubjectId,
                    question.Origin.NoticeSubjectLabel,
                    question.Status,
                    question.ContentLanguage))
                .ToList();

            return Json(new EditionQuestionPageResponse(items, ordered.Count));
        }

        private HttpResponseMessage HandleSubjects(string path, string query)
        {
            var segments = path[SubjectsPath.Length..].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                var term = System.Web.HttpUtility.ParseQueryString(query)["search"];

                return Json(Subjects
                    .Where(subject => string.IsNullOrEmpty(term) || subject.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(subject => new SubjectLookupResponse(subject.Id, subject.Name))
                    .ToList());
            }

            if (segments.Length == 2 && segments[1] == "topics")
            {
                var subjectId = Guid.Parse(segments[0]);
                if (!Subjects.Exists(subject => subject.Id == subjectId))
                {
                    return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionSubjectNotFound);
                }

                return Json(Topics
                    .Where(topic => topic.SubjectId == subjectId)
                    .Select(topic => new TopicLookupResponse(topic.Id, topic.Name))
                    .ToList());
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
            rows = FilterBy<QuestionStatus>(rows, parameters, "status", (row, value) => row.Status == value);
            rows = FilterBy<QuestionType>(rows, parameters, "type", (row, value) => row.Type == value);

            var difficulties = parameters.GetValues("difficulty") ?? [];
            if (difficulties.Length > 0)
            {
                var numbers = difficulties.Select(value => int.TryParse(value, out var number) ? number : 0).ToList();
                var none = difficulties.Contains("none", StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(row => row.Difficulty is null ? none : numbers.Contains(row.Difficulty.Value));
            }

            var topicIds = Guids(parameters, "topic");
            var subjectIds = Guids(parameters, "subject");
            if (topicIds.Count + subjectIds.Count > 0)
            {
                rows = rows.Where(row => topicIds.Contains(row.TopicId) || subjectIds.Contains(row.SubjectId));
            }

            var organizerIds = Guids(parameters, "organizer");
            if (organizerIds.Count > 0)
            {
                var acronyms = Organizers.Where(organizer => organizerIds.Contains(organizer.Id)).Select(organizer => organizer.Acronym).ToList();
                rows = rows.Where(row => row.Origin is not null && acronyms.Contains(row.Origin.OrganizerAcronym));
            }

            if (Guids(parameters, "edition") is [var editionId])
            {
                rows = rows.Where(row => row.Origin?.ExamEditionId == editionId);
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

        private static IEnumerable<QuestionResponse> FilterBy<TEnum>(
            IEnumerable<QuestionResponse> rows,
            System.Collections.Specialized.NameValueCollection parameters,
            string key,
            Func<QuestionResponse, TEnum, bool> matches)
            where TEnum : struct, Enum
        {
            var values = (parameters.GetValues(key) ?? [])
                .Select(value => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : (TEnum?)null)
                .Where(value => value is not null)
                .Select(value => value!.Value)
                .ToList();

            return values.Count == 0 ? rows : rows.Where(row => values.Exists(value => matches(row, value)));
        }

        private static List<Guid> Guids(System.Collections.Specialized.NameValueCollection parameters, string key) =>
            [.. (parameters.GetValues(key) ?? []).Select(value => Guid.TryParse(value, out var id) ? id : (Guid?)null)
                .Where(id => id is not null).Select(id => id!.Value)];

        // The taxonomy as the Api answers it: subjects by name, each with its topics by name.
        private List<QuestionTaxonomySubjectResponse> TaxonomyOf() =>
            [.. Subjects.OrderBy(subject => subject.Name, StringComparer.OrdinalIgnoreCase)
                .Select(subject => new QuestionTaxonomySubjectResponse(
                    subject.Id,
                    subject.Name,
                    [.. Topics.Where(topic => topic.SubjectId == subject.Id)
                        .OrderBy(topic => topic.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(topic => new TopicLookupResponse(topic.Id, topic.Name))]))];

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
                question.Status,
                question.Type,
                question.Origin);

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

            var updated = Build(id, Read<SaveQuestionRequest>(body), existing.Status, existing.Type);
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
        private QuestionResponse Build(Guid id, SaveQuestionRequest request, QuestionStatus status, QuestionType? stored = null)
        {
            var topic = Topics.First(candidate => candidate.Id == request.TopicId);
            var subject = Subjects.First(candidate => candidate.Id == topic.SubjectId);

            return new QuestionResponse(
                id,
                stored ?? request.Type ?? QuestionType.SingleChoice,
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
                    new QuestionOptionResponse(position, Sanitize(option.TextHtml) ?? string.Empty, option.IsCorrect))],
                OriginOf(request),
                request.TrueFalseKey);
        }

        // The Api reads the origin parts from the edition; the fake does it from the editions the test seeded.
        private QuestionOriginResponse? OriginOf(SaveQuestionRequest request)
        {
            if (request.ExamEditionId is not { } editionId)
            {
                return null;
            }

            var edition = Editions.Find(candidate => candidate.Id == editionId);
            var label = request.NoticeSubjectId is { } subjectId
                && NoticeSubjects.TryGetValue(editionId, out var subjects)
                    ? subjects.Find(subject => subject.Id == subjectId)?.Label
                    : null;

            return new QuestionOriginResponse(
                editionId,
                Guid.Empty,
                edition?.ExamName ?? string.Empty,
                edition?.OrganizerAcronym ?? string.Empty,
                edition?.OrganizerAcronym ?? string.Empty,
                edition?.NoticeYear,
                edition?.Position,
                request.PaperNumber ?? 0,
                request.NoticeSubjectId,
                label);
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
