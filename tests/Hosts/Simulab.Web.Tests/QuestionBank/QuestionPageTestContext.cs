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

    /// <summary>F-102: a pt-PT base text that the published question below links.</summary>
    protected static readonly BaseTextResponse CommaText = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000d1"),
        "Texto I - A vírgula",
        "pt-PT",
        "<p>Texto-base com <sup>nota</sup>.</p>",
        1,
        new DateTimeOffset(2026, 10, 14, 9, 0, 0, TimeSpan.Zero));

    /// <summary>F-102: pt-BR texts, one used by three questions and one by none.</summary>
    protected static readonly BaseTextResponse ClinicalCase = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000d2"),
        "Caso clínico - UBS do bairro",
        "pt-BR",
        "<p>Paciente de 54 anos chega à UBS.</p>",
        3,
        new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero));

    protected static readonly BaseTextResponse UnusedTariffs = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000d3"),
        "Tabela de tarifas de energia 2025",
        "pt-BR",
        "<p>Tabela de tarifas.</p>",
        0,
        new DateTimeOffset(2026, 9, 20, 9, 0, 0, TimeSpan.Zero));

    protected static readonly BaseTextResponse PacificText = new(
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000d4"),
        "Text 1 - The Great Pacific Garbage Patch",
        "en",
        "<p>The patch is a gyre of plastic.</p>",
        5,
        new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero));

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
        new QuestionBaseTextResponse(CommaText.Id, CommaText.Title, CommaText.TextHtml),
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
        private const string BaseTextsPath = "/api/v1/question-bank/base-texts";
        private const string EditionsPath = "/api/v1/question-bank/editions";
        private const string SubjectsPath = "/api/v1/question-bank/subjects";
        private const string TaxonomyPath = "/api/v1/question-bank/taxonomy";
        private const string OrganizersPath = "/api/v1/question-bank/organizers";
        private const string CatalogPath = "/api/v1/catalog/";

        /// <summary>The stored questions, newest first: the test sets it, the writes change it.</summary>
        public List<QuestionResponse> Questions { get; } = [];

        public List<SubjectResponse> Subjects { get; } = [Portuguese, Law];

        public List<TopicResponse> Topics { get; } = [Crase, Punctuation, FundamentalRights];

        /// <summary>
        /// F-102: the stored base texts, with the count of linked questions as the test sets it. It starts with the
        /// text the published question links, so saving that question unchanged is not refused.
        /// </summary>
        public List<BaseTextResponse> BaseTexts { get; } = [CommaText];

        /// <summary>F-102: when set, listing the base texts answers this problem (the list's load error).</summary>
        public (HttpStatusCode Status, string Code)? BaseTextListFailure { get; set; }

        /// <summary>F-102: when set, reading one base text, or the questions of one, answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? BaseTextFindFailure { get; set; }

        /// <summary>F-102: when set, creating or updating a base text answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? BaseTextSaveFailure { get; set; }

        /// <summary>F-102: when set, deleting a base text answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? BaseTextDeleteFailure { get; set; }

        /// <summary>F-102: replaces the stored base texts with exactly these.</summary>
        public void SeedBaseTexts(params BaseTextResponse[] texts)
        {
            BaseTexts.Clear();
            BaseTexts.AddRange(texts);
        }

        public SaveBaseTextRequest LastSavedBaseText(HttpMethod method) =>
            Read<SaveBaseTextRequest>(Received.Last(call => call.Method == method && call.Path.StartsWith(BaseTextsPath, StringComparison.Ordinal)).Body);

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

            if (path.StartsWith(BaseTextsPath, StringComparison.Ordinal))
            {
                return HandleBaseTexts(request.Method, path, query, body);
            }

            if (path.StartsWith(ReviewQueuePath, StringComparison.Ordinal))
            {
                return HandleReviewQueue(request.Method, path, query, body);
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
                (2, "GET") when segments[1] == "reviews" => ListReviews(Guid.Parse(segments[0])),
                (2, "POST") when segments[1] == "submit" => ChangeReview(Guid.Parse(segments[0]), submit: true),
                (2, "POST") when segments[1] == "withdraw" => ChangeReview(Guid.Parse(segments[0]), submit: false),
                (2, "POST") when segments[1] == "annul" => ChangeAnnulment(Guid.Parse(segments[0]), body, annul: true),
                (2, "POST") when segments[1] == "reinstate" => ChangeAnnulment(Guid.Parse(segments[0]), body, annul: false),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        private const string ReviewQueuePath = "/api/v1/question-bank/review-queue";

        /// <summary>F-105: when set, listing the review queue answers this problem (the queue's load error).</summary>
        public (HttpStatusCode Status, string Code)? QueueFailure { get; set; }

        /// <summary>F-105: when set, reading one question of the queue answers this problem (the review screen's load error).</summary>
        public (HttpStatusCode Status, string Code)? ReviewFindFailure { get; set; }

        /// <summary>F-105: when set, an approve or a reject answers this problem (a 409 race, say).</summary>
        public (HttpStatusCode Status, string Code)? DecisionFailure { get; set; }

        /// <summary>F-105: how many times the queue was listed.</summary>
        public int QueueCalls => Received.Count(call => call.Method == HttpMethod.Get && call.Path == ReviewQueuePath);

        /// <summary>F-105: how many times one question of the queue was read.</summary>
        public int ReviewReads => Received.Count(call =>
            call.Method == HttpMethod.Get && call.Path.StartsWith(ReviewQueuePath + "/", StringComparison.Ordinal));

        public ApproveQuestionRequest LastApproval() =>
            Read<ApproveQuestionRequest>(Received.Last(call => call.Path.EndsWith("/approve", StringComparison.Ordinal)).Body);

        public RejectQuestionRequest LastRejection() =>
            Read<RejectQuestionRequest>(Received.Last(call => call.Path.EndsWith("/reject", StringComparison.Ordinal)).Body);

        // F-105: the review routes as the Api answers them, with the smallest rules a screen test needs.
        private HttpResponseMessage HandleReviewQueue(HttpMethod method, string path, string query, string? body)
        {
            var segments = path[ReviewQueuePath.Length..].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            return (segments.Length, method.Method) switch
            {
                (0, "GET") => ListQueue(query),
                (1, "GET") => FindInReview(Guid.Parse(segments[0])),
                (2, "POST") when segments[1] == "approve" => Decide(Guid.Parse(segments[0]), approve: true, body),
                (2, "POST") when segments[1] == "reject" => Decide(Guid.Parse(segments[0]), approve: false, body),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        // BR8: only the questions in review, the oldest latest send first.
        private HttpResponseMessage ListQueue(string query)
        {
            if (QueueFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var parameters = System.Web.HttpUtility.ParseQueryString(query);
            var all = Questions
                .Where(question => question.Status == QuestionStatus.InReview)
                .OrderBy(question => question.SubmittedAt)
                .ToList();
            var size = int.Parse(parameters["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
            var page = int.Parse(parameters["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var items = all.Skip(page * size).Take(size)
                .Select(question => new ReviewQueueItemResponse(
                    question.Id,
                    PlainText(question.StatementHtml),
                    question.ContentLanguage,
                    question.Type,
                    question.TopicName,
                    question.SubjectName,
                    question.Difficulty,
                    question.SubmittedAt ?? default))
                .ToList();

            return Json(new ReviewQueuePageResponse(items, all.Count));
        }

        private HttpResponseMessage FindInReview(Guid id)
        {
            if (ReviewFindFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var question = Questions.Find(candidate => candidate.Id == id);
            if (question is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            return question.Status == QuestionStatus.InReview
                ? Json(question)
                : Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        }

        // Approve publishes (and makes the version the Api would), reject takes it back to a draft; a SubmittedAt other
        // than the latest send is a lost race (change note v2).
        private HttpResponseMessage Decide(Guid id, bool approve, string? body)
        {
            if (DecisionFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var index = Questions.FindIndex(candidate => candidate.Id == id);
            if (index < 0)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var current = Questions[index];
            if (current.Status != QuestionStatus.InReview)
            {
                return Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
            }

            var (text, submittedAt) = approve
                ? (Read<ApproveQuestionRequest>(body).Note, Read<ApproveQuestionRequest>(body).SubmittedAt)
                : (Read<RejectQuestionRequest>(body).Reason, Read<RejectQuestionRequest>(body).SubmittedAt);
            if (!approve && string.IsNullOrWhiteSpace(text))
            {
                return Problem(HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionReviewReasonRequired);
            }

            if (submittedAt is { } seen && seen != current.SubmittedAt)
            {
                return Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionVersionConflict);
            }

            var changed = current with
            {
                Status = approve ? QuestionStatus.Published : QuestionStatus.Draft,
                CurrentVersion = approve ? (current.CurrentVersion ?? 0) + 1 : current.CurrentVersion,
                SubmittedAt = null
            };
            Questions[index] = changed;

            return Json(changed);
        }

        /// <summary>F-105: the review history of each question, oldest first: the test sets it; a send and a withdrawal add to it.</summary>
        public Dictionary<Guid, List<QuestionReviewEntryResponse>> Reviews { get; } = [];

        /// <summary>F-105: when set, a send or a withdrawal answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? ReviewFailure { get; set; }

        /// <summary>F-105: when set, reading the review history answers this problem (the card then shows its error state).</summary>
        public (HttpStatusCode Status, string Code)? ReviewsReadFailure { get; set; }

        private static readonly DateTimeOffset ReviewMoment = new(2026, 10, 16, 14, 2, 0, TimeSpan.Zero);

        private HttpResponseMessage ListReviews(Guid questionId)
        {
            if (!Questions.Exists(candidate => candidate.Id == questionId))
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            if (ReviewsReadFailure is { } failed)
            {
                return Problem(failed.Status, failed.Code);
            }

            return Json(Reviews.TryGetValue(questionId, out var found) ? found : []);
        }

        // F-105: the Api's answer to a send or a withdrawal. It records the entry the Api would, so the history the
        // page reads again shows it, and moves the question the way the Api does.
        private HttpResponseMessage ChangeReview(Guid id, bool submit)
        {
            if (ReviewFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var index = Questions.FindIndex(candidate => candidate.Id == id);
            if (index < 0)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var current = Questions[index];
            var changed = current with
            {
                Status = submit ? QuestionStatus.InReview : QuestionStatus.Draft,
                SubmittedAt = submit ? ReviewMoment : null
            };
            Questions[index] = changed;

            if (!Reviews.TryGetValue(id, out var entries))
            {
                entries = [];
                Reviews[id] = entries;
            }

            entries.Add(new QuestionReviewEntryResponse(
                submit ? QuestionReviewKind.Submitted : QuestionReviewKind.Withdrawn,
                null,
                ReviewMoment.AddMinutes(entries.Count)));

            return Json(changed);
        }

        /// <summary>F-101: when set, an annul or a reinstate answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? AnnulmentFailure { get; set; }

        // F-101: the Api's answer to an annulment or a reinstatement, with the reason it was given. It makes the
        // version the Api would make, so the History card the page reloads shows it.
        private HttpResponseMessage ChangeAnnulment(Guid id, string? body, bool annul)
        {
            if (AnnulmentFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var index = Questions.FindIndex(candidate => candidate.Id == id);
            if (index < 0)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
            }

            var reason = (annul ? Read<AnnulQuestionRequest>(body).Reason : Read<ReinstateQuestionRequest>(body).Reason)?.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                return Problem(HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionStatusReasonRequired);
            }

            var current = Questions[index];
            var number = (current.CurrentVersion ?? 0) + 1;
            var recorded = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var changed = current with
            {
                Status = annul ? QuestionStatus.Annulled : QuestionStatus.Published,
                CurrentVersion = number,
                Annulment = annul ? new QuestionAnnulmentResponse(reason, recorded) : null
            };
            Questions[index] = changed;

            if (!Versions.TryGetValue(id, out var versions))
            {
                versions = [];
                Versions[id] = versions;
            }

            versions.Add(new QuestionVersionResponse(
                Guid.CreateVersion7(),
                id,
                number,
                current.Type,
                current.ContentLanguage,
                current.StatementHtml,
                current.BaseText?.TextHtml,
                current.ExplanationHtml,
                current.Options,
                current.TrueFalseKey,
                false,
                reason,
                recorded,
                annul));

            return Json(changed);
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
                    version.Id,
                    version.Number,
                    version.CreatedAt,
                    version.AnswerKeyChanged,
                    version.Reason,
                    index == 0,
                    version.Annulled,
                    !version.Annulled && index + 1 < ordered.Count && ordered[index + 1].Annulled))
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

        // F-102: the base text routes, as the Api answers them, with the smallest rules a screen test needs.
        private HttpResponseMessage HandleBaseTexts(HttpMethod method, string path, string query, string? body)
        {
            var segments = path[BaseTextsPath.Length..].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return method == HttpMethod.Post ? CreateBaseText(body) : ListBaseTexts(query);
            }

            var id = Guid.Parse(segments[0]);
            if (segments.Length == 2 && segments[1] == "questions")
            {
                return ListBaseTextQuestions(id, query);
            }

            return method.Method switch
            {
                "GET" => FindBaseText(id),
                "PUT" => UpdateBaseText(id, body),
                "DELETE" => DeleteBaseText(id),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        private HttpResponseMessage ListBaseTexts(string query)
        {
            if (BaseTextListFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var parameters = System.Web.HttpUtility.ParseQueryString(query);
            IEnumerable<BaseTextResponse> rows = BaseTexts;
            if (parameters["language"] is { Length: > 0 } language)
            {
                rows = rows.Where(row => string.Equals(row.ContentLanguage, language, StringComparison.OrdinalIgnoreCase));
            }

            if (parameters["search"] is { Length: > 0 } term)
            {
                rows = rows.Where(row => row.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var all = rows.OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase).ToList();
            var size = int.Parse(parameters["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
            var page = int.Parse(parameters["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var items = all.Skip(page * size).Take(size)
                .Select(row => new BaseTextListItemResponse(row.Id, row.Title, row.ContentLanguage, row.QuestionCount, row.UpdatedAt))
                .ToList();

            return Json(new BaseTextPageResponse(items, all.Count));
        }

        private HttpResponseMessage FindBaseText(Guid id)
        {
            if (BaseTextFindFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var found = BaseTexts.Find(candidate => candidate.Id == id);

            return found is null
                ? Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.BaseTextNotFound)
                : Json(found);
        }

        private HttpResponseMessage CreateBaseText(string? body)
        {
            if (BaseTextSaveFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var request = Read<SaveBaseTextRequest>(body);
            var created = new BaseTextResponse(
                Guid.CreateVersion7(),
                request.Title!.Trim(),
                request.ContentLanguage!,
                Sanitize(request.TextHtml) ?? string.Empty,
                0,
                new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero));
            BaseTexts.Add(created);

            return Json(created, HttpStatusCode.Created);
        }

        private HttpResponseMessage UpdateBaseText(Guid id, string? body)
        {
            if (BaseTextSaveFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var existing = BaseTexts.Find(candidate => candidate.Id == id);
            if (existing is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.BaseTextNotFound);
            }

            var request = Read<SaveBaseTextRequest>(body);
            var updated = existing with
            {
                Title = request.Title!.Trim(),
                ContentLanguage = request.ContentLanguage!,
                TextHtml = Sanitize(request.TextHtml) ?? string.Empty
            };
            BaseTexts[BaseTexts.IndexOf(existing)] = updated;

            return Json(updated);
        }

        private HttpResponseMessage DeleteBaseText(Guid id)
        {
            if (BaseTextDeleteFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            var existing = BaseTexts.Find(candidate => candidate.Id == id);
            if (existing is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.BaseTextNotFound);
            }

            if (existing.QuestionCount > 0)
            {
                return Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.BaseTextInUse);
            }

            BaseTexts.Remove(existing);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private HttpResponseMessage ListBaseTextQuestions(Guid id, string query)
        {
            if (BaseTextFindFailure is { } refused)
            {
                return Problem(refused.Status, refused.Code);
            }

            if (!BaseTexts.Exists(candidate => candidate.Id == id))
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.BaseTextNotFound);
            }

            var parameters = System.Web.HttpUtility.ParseQueryString(query);
            var all = Questions.Where(question => question.BaseText?.Id == id).ToList();
            var size = int.Parse(parameters["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
            var page = int.Parse(parameters["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);

            return Json(new QuestionPageResponse([.. all.Skip(page * size).Take(size).Select(ToListItem)], all.Count));
        }

        // The Api answers 404 for a text that is gone and 409 for one in another language, before it judges the rest.
        private HttpResponseMessage? BaseTextRefusal(SaveQuestionRequest request)
        {
            if (request.BaseTextId is not { } baseTextId)
            {
                return null;
            }

            var linked = BaseTexts.Find(candidate => candidate.Id == baseTextId);
            if (linked is null)
            {
                return Problem(HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionBaseTextNotFound);
            }

            return string.Equals(linked.ContentLanguage, request.ContentLanguage, StringComparison.OrdinalIgnoreCase)
                ? null
                : Problem(HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionBaseTextLanguageMismatch);
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

            if (segments[1] == "next-paper-number")
            {
                var highest = Questions
                    .Where(question => question.Origin?.ExamEditionId == editionId)
                    .Select(question => question.Origin!.PaperNumber)
                    .DefaultIfEmpty(0)
                    .Max();

                return Json(new NextPaperNumberResponse(highest + 1));
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

            var request = Read<SaveQuestionRequest>(body);
            if (BaseTextRefusal(request) is { } baseTextRefused)
            {
                return baseTextRefused;
            }

            var created = Build(Guid.CreateVersion7(), request, QuestionStatus.Draft);
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

            var request = Read<SaveQuestionRequest>(body);
            if (BaseTextRefusal(request) is { } baseTextRefused)
            {
                return baseTextRefused;
            }

            var updated = Build(id, request, existing.Status, existing.Type);
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
                request.BaseTextId is { } baseTextId && BaseTexts.Find(candidate => candidate.Id == baseTextId) is { } linked
                    ? new QuestionBaseTextResponse(linked.Id, linked.Title, linked.TextHtml)
                    : null,
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
