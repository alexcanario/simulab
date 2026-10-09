using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Components.Ui;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-33's back office page, signed in as an Admin holding <c>catalog.manage</c>, with a fake Api behind
/// the typed client: the test says what the Api answers and reads what the page shows and what it sent.
/// </summary>
public abstract class CatalogPageTestContext : KitTestContext
{
    private const string WebSessionId = "web-1";

    protected static readonly OrganizerResponse Cebraspe =
        new(Guid.Parse("0198f0a3-0000-7000-8000-000000000001"), "Centro Brasileiro de Pesquisa em Avaliacao", "CEBRASPE", OrganizerKind.ExamBoard, null, "https://www.cebraspe.org.br");

    protected static readonly OrganizerResponse Fgv =
        new(Guid.Parse("0198f0a3-0000-7000-8000-000000000002"), "Fundacao Getulio Vargas", "FGV", OrganizerKind.University, "Since 1944", null);

    protected static readonly OrganizerResponse Iso =
        new(Guid.Parse("0198f0a3-0000-7000-8000-000000000003"), "International Organization for Standardization", "ISO", OrganizerKind.CertifyingBody, null, null);

    protected static readonly IssuingAuthorityResponse Guarulhos = new(
        Guid.Parse("0198f0a3-0000-7000-8000-000000000011"),
        "Prefeitura Municipal de Guarulhos",
        null,
        null);

    protected static readonly IssuingAuthorityResponse PoliciaFederal = new(
        Guid.Parse("0198f0a3-0000-7000-8000-000000000012"),
        "Policia Federal",
        null,
        null);

    /// <summary>The bodies the fake knows about, so a saved exam can carry its authority's name (F-34 v2).</summary>
    protected static readonly IReadOnlyList<IssuingAuthorityResponse> Sample = [Guarulhos, PoliciaFederal];

    protected static readonly ExamResponse AgentePf = new(
        Guid.Parse("0198f0a3-0000-7000-8000-00000000000a"),
        "Agente de Policia Federal",
        PoliciaFederal.Id,
        PoliciaFederal.Name,
        AssessmentType.PublicServiceExam,
        ExamScope.National,
        null,
        "pt-BR");

    protected static readonly ExamResponse Fuvest = new(
        Guid.Parse("0198f0a3-0000-7000-8000-00000000000b"),
        "FUVEST",
        Guarulhos.Id,
        Guarulhos.Name,
        AssessmentType.UniversityEntranceExam,
        ExamScope.State,
        "SP",
        "pt-BR");

    protected FakeCatalogApi Api { get; } = new();

    protected CatalogPageTestContext()
    {
        Services.AddSingleton(new CatalogApiClient(new HttpClient(Api) { BaseAddress = new Uri("https://api.test") }));

        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [CatalogPermissions.Manage], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId), new Claim(WebAuthClaims.Permission, CatalogPermissions.Manage));
    }

    /// <summary>The providers a page's dialogs, tooltips and snackbars render into.</summary>
    protected (IRenderedComponent<MudDialogProvider> Dialogs, IRenderedComponent<MudSnackbarProvider> Snackbars) RenderProviders()
    {
        Render<MudPopoverProvider>();
        return (Render<MudDialogProvider>(), Render<MudSnackbarProvider>());
    }

    /// <summary>Answers the catalog routes with what the test set, and records what it received.</summary>
    protected sealed class FakeCatalogApi : HttpMessageHandler
    {
        /// <summary>What the list returns; null makes it answer a server error.</summary>
        public List<OrganizerResponse>? Organizers { get; set; } = [Cebraspe, Fgv, Iso];

        /// <summary>What the exam list returns (F-34); null makes it answer a server error.</summary>
        public List<ExamResponse>? Exams { get; set; } = [AgentePf, Fuvest];

        /// <summary>What the issuing-authority list returns (F-34 v2); null makes it answer a server error.</summary>
        public List<IssuingAuthorityResponse>? IssuingAuthorities { get; set; } = [.. Sample];

        /// <summary>When set, every write answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? WriteFailure { get; set; }

        /// <summary>When set, reading one exam answers this problem (F-34: the form page's not-found state).</summary>
        public (HttpStatusCode Status, string Code)? FindExamFailure { get; set; }

        /// <summary>The editions the fake holds (F-35), in the order the Api would answer them: the test sets it.</summary>
        public List<ExamEditionResponse> Editions { get; } = [];

        /// <summary>When set, listing an exam's editions answers this problem (F-35: the section's load error).</summary>
        public (HttpStatusCode Status, string Code)? ListEditionsFailure { get; set; }

        /// <summary>When set, reading one edition answers this problem (F-35: the edition page's not-found state).</summary>
        public (HttpStatusCode Status, string Code)? FindEditionFailure { get; set; }

        /// <summary>The notice subjects the fake holds (F-74), in display order: the test sets it, the writes change it.</summary>
        public List<NoticeSubjectResponse> NoticeSubjects { get; } = [];

        /// <summary>When set, every write waits for it before it is answered (F-75: the screen while a save is in flight).</summary>
        public TaskCompletionSource? WriteGate { get; set; }

        /// <summary>The live subjects and topics the Covers field picks from (F-75), in the order the Api would answer.</summary>
        public List<TaxonomySubjectResponse> Taxonomy { get; } = [];

        /// <summary>When set, reading the taxonomy answers this problem (F-75: the picker's load error).</summary>
        public (HttpStatusCode Status, string Code)? TaxonomyFailure { get; set; }

        /// <summary>How many times the taxonomy was read: once per dialog opening, and again after a refusal that needs it.</summary>
        public int TaxonomyReads => Received.Count(call => call.Method == HttpMethod.Get && call.Path == "/api/v1/catalog/taxonomy");

        /// <summary>When set, listing an edition's notice subjects answers this problem (F-74: the section's load error).</summary>
        public (HttpStatusCode Status, string Code)? ListNoticeSubjectsFailure { get; set; }

        /// <summary>When set, a move answers this problem (F-74: a stale list), and nothing changes.</summary>
        public (HttpStatusCode Status, string Code)? MoveFailure { get; set; }

        public List<(HttpMethod Method, string Path, string? Query, string? Body)> Received { get; } = [];

        /// <summary>
        /// A test that owns its own routes (F-79: the taxonomy) answers them here, ahead of the built-in ones;
        /// returning null falls through to those.
        /// </summary>
        public Func<HttpMethod, string, string?, string?, HttpResponseMessage?>? Custom { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Received.Add((request.Method, path, request.RequestUri.Query, body));

            // A test that must look at the screen while a save is in flight holds the write here, then lets it go.
            if (request.Method != HttpMethod.Get && WriteGate is { } gate)
            {
                await gate.Task;
            }

            if (Custom?.Invoke(request.Method, path, request.RequestUri.Query, body) is { } custom)
            {
                return custom;
            }

            if (path == "/api/v1/catalog/taxonomy" && request.Method == HttpMethod.Get)
            {
                return TaxonomyFailure is { } taxonomyRefused
                    ? Problem(taxonomyRefused)
                    : Json<IReadOnlyList<TaxonomySubjectResponse>>([.. Taxonomy]);
            }

            var subjectRoute = System.Text.RegularExpressions.Regex.Match(
                path, "^/api/v1/catalog/exams/(?<exam>[^/]+)/editions/(?<edition>[^/]+)/notice-subjects(/(?<id>[^/]+))?(?<move>/move)?$");
            if (subjectRoute.Success)
            {
                return HandleNoticeSubject(
                    request.Method,
                    Guid.Parse(subjectRoute.Groups["edition"].Value),
                    subjectRoute.Groups["id"],
                    subjectRoute.Groups["move"].Success,
                    body);
            }

            var editionRoute = System.Text.RegularExpressions.Regex.Match(
                path, "^/api/v1/catalog/exams/(?<exam>[^/]+)/editions(/(?<id>[^/]+))?$");
            if (editionRoute.Success)
            {
                return HandleEdition(request.Method, Guid.Parse(editionRoute.Groups["exam"].Value), editionRoute.Groups["id"], body);
            }

            if (request.Method != HttpMethod.Get && WriteFailure is { } failure)
            {
                return Problem(failure);
            }

            if (path.EndsWith("/issuing-authorities", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                if (IssuingAuthorities is null)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                var authorityQuery = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                var authoritySize = int.Parse(authorityQuery["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
                var authorityPage = int.Parse(authorityQuery["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                return Json(new IssuingAuthorityPageResponse(
                    [.. IssuingAuthorities.Skip(authorityPage * authoritySize).Take(authoritySize)],
                    IssuingAuthorities.Count));
            }

            if (path.EndsWith("/issuing-authorities", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                var saved = Read<SaveIssuingAuthorityRequest>(body);
                return Json(SavedAuthority(Guid.CreateVersion7(), saved), HttpStatusCode.Created);
            }

            if (path.Contains("/issuing-authorities/", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            {
                var saved = Read<SaveIssuingAuthorityRequest>(body);
                return Json(SavedAuthority(Guid.Parse(path[(path.LastIndexOf('/') + 1)..]), saved));
            }

            if (path.Contains("/issuing-authorities/", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path.EndsWith("/exams", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                if (Exams is null)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                var examQuery = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                var examSize = int.Parse(examQuery["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
                var examPage = int.Parse(examQuery["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                return Json(new ExamPageResponse([.. Exams.Skip(examPage * examSize).Take(examSize)], Exams.Count));
            }

            if (path.Contains("/exams/", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                if (FindExamFailure is { } refused)
                {
                    return Problem(refused);
                }

                var exam = Exams?.Find(candidate => candidate.Id == Guid.Parse(path[(path.LastIndexOf('/') + 1)..]));
                return exam is null ? Problem((HttpStatusCode.NotFound, CatalogErrorCodes.ExamNotFound)) : Json(exam);
            }

            if (path.EndsWith("/exams", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                return Json(SavedExam(Guid.CreateVersion7(), Read<SaveExamRequest>(body)), HttpStatusCode.Created);
            }

            if (path.Contains("/exams/", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            {
                return Json(SavedExam(Guid.Parse(path[(path.LastIndexOf('/') + 1)..]), Read<SaveExamRequest>(body)));
            }

            if (path.Contains("/exams/", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path.EndsWith("/organizers", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                if (Organizers is null)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                // Paged like the Api: the page the query asks for, and the total.
                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                var size = int.Parse(query["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
                var number = int.Parse(query["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                return Json(new OrganizerPageResponse([.. Organizers.Skip(number * size).Take(size)], Organizers.Count));
            }

            if (path.EndsWith("/organizers", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                var saved = Read<SaveOrganizerRequest>(body);
                return Json(Saved(Guid.CreateVersion7(), saved), HttpStatusCode.Created);
            }

            if (path.Contains("/organizers/", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            {
                var saved = Read<SaveOrganizerRequest>(body);
                return Json(Saved(Guid.Parse(path[(path.LastIndexOf('/') + 1)..]), saved));
            }

            if (path.Contains("/organizers/", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        // F-35: the five edition routes, answering the way the Api does (BR3 to BR14 are the Api's; the fake keeps the
        // shape: a list, one edition, a create that is a draft by default, an update, and a delete that refuses a
        // published edition).
        private HttpResponseMessage HandleEdition(
            HttpMethod method,
            Guid examId,
            System.Text.RegularExpressions.Group idGroup,
            string? body)
        {
            if (method != HttpMethod.Get && WriteFailure is { } failure)
            {
                return Problem(failure);
            }

            if (!idGroup.Success)
            {
                if (method == HttpMethod.Get)
                {
                    return ListEditionsFailure is { } listRefused
                        ? Problem(listRefused)
                        : Json<IReadOnlyList<ExamEditionResponse>>([.. Editions.Where(edition => edition.ExamId == examId)]);
                }

                var created = SavedEdition(Guid.CreateVersion7(), examId, Read<SaveExamEditionRequest>(body));
                Editions.Add(created);
                return Json(created, HttpStatusCode.Created);
            }

            var id = Guid.Parse(idGroup.Value);
            var existing = Editions.Find(edition => edition.Id == id && edition.ExamId == examId);
            if (method == HttpMethod.Get)
            {
                return FindEditionFailure is { } findRefused
                    ? Problem(findRefused)
                    : existing is null ? Problem((HttpStatusCode.NotFound, CatalogErrorCodes.ExamEditionNotFound)) : Json(existing);
            }

            if (existing is null)
            {
                return Problem((HttpStatusCode.NotFound, CatalogErrorCodes.ExamEditionNotFound));
            }

            if (method == HttpMethod.Put)
            {
                var updated = SavedEdition(id, examId, Read<SaveExamEditionRequest>(body));
                Editions[Editions.IndexOf(existing)] = updated;
                return Json(updated);
            }

            if (existing.Status == ExamEditionStatus.Published)
            {
                return Problem((HttpStatusCode.Conflict, CatalogErrorCodes.ExamEditionPublished));
            }

            Editions.Remove(existing);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        // F-74: the five notice subject routes. The fake keeps the shape the Api has (rows of one group stay together, a
        // new row goes last in its group, a changed group goes to the end of the new one, a move swaps two neighbours of
        // one group); the rules about text and numbers are the Api's and are tested there.
        private HttpResponseMessage HandleNoticeSubject(
            HttpMethod method,
            Guid editionId,
            System.Text.RegularExpressions.Group idGroup,
            bool isMove,
            string? body)
        {
            if (method == HttpMethod.Get)
            {
                return ListNoticeSubjectsFailure is { } listRefused
                    ? Problem(listRefused)
                    : Json<IReadOnlyList<NoticeSubjectResponse>>([.. NoticeSubjects.Where(row => row.ExamEditionId == editionId)]);
            }

            var refusal = isMove ? MoveFailure ?? WriteFailure : WriteFailure;
            if (refusal is { } refused)
            {
                return Problem(refused);
            }

            if (!idGroup.Success)
            {
                var request = Read<SaveNoticeSubjectRequest>(body);
                var created = new NoticeSubjectResponse(
                    Guid.CreateVersion7(), editionId, Blank(request.Group), request.Label!, request.QuestionCount, Resolve(request.Mappings));
                InsertLastInGroup(created);
                return Json(created, HttpStatusCode.Created);
            }

            var id = Guid.Parse(idGroup.Value);
            var existing = NoticeSubjects.Find(row => row.Id == id && row.ExamEditionId == editionId);
            if (existing is null)
            {
                return Problem((HttpStatusCode.NotFound, CatalogErrorCodes.NoticeSubjectNotFound));
            }

            if (isMove)
            {
                var up = Read<MoveNoticeSubjectRequest>(body).ParseDirection() == NoticeSubjectMoveDirection.Up;
                var sameGroup = NoticeSubjects.Where(row => row.ExamEditionId == editionId
                    && AppSuggestField.Normalize(row.Group) == AppSuggestField.Normalize(existing.Group)).ToList();
                var at = sameGroup.IndexOf(existing);
                var neighbour = up ? at - 1 : at + 1;
                if (neighbour < 0 || neighbour >= sameGroup.Count)
                {
                    return Problem((HttpStatusCode.BadRequest, CatalogErrorCodes.NoticeSubjectMoveInvalid));
                }

                var first = NoticeSubjects.IndexOf(sameGroup[Math.Min(at, neighbour)]);
                var second = NoticeSubjects.IndexOf(sameGroup[Math.Max(at, neighbour)]);
                (NoticeSubjects[first], NoticeSubjects[second]) = (NoticeSubjects[second], NoticeSubjects[first]);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (method == HttpMethod.Put)
            {
                var request = Read<SaveNoticeSubjectRequest>(body);
                var updated = existing with
                {
                    Group = Blank(request.Group),
                    Label = request.Label!,
                    QuestionCount = request.QuestionCount,
                    Mappings = Resolve(request.Mappings)
                };
                if (AppSuggestField.Normalize(updated.Group) == AppSuggestField.Normalize(existing.Group))
                {
                    NoticeSubjects[NoticeSubjects.IndexOf(existing)] = updated;
                }
                else
                {
                    NoticeSubjects.Remove(existing);
                    InsertLastInGroup(updated);
                }

                return Json(updated);
            }

            NoticeSubjects.Remove(existing);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        // F-75 BR7: a save replaces the whole mapping. The Api answers each entry with the names it joins from the
        // taxonomy; the fake looks them up in the same list the picker was given. The rules (BR1 to BR5) are the Api's.
        private IReadOnlyList<NoticeSubjectMappingResponse> Resolve(IReadOnlyList<NoticeSubjectMappingRequest>? requested) =>
            [.. (requested ?? []).Select(entry =>
            {
                if (entry.TopicId is { } topicId)
                {
                    var owner = Taxonomy.First(subject => subject.Topics.Any(topic => topic.Id == topicId));
                    return new NoticeSubjectMappingResponse(owner.Id, owner.Name, topicId, owner.Topics.First(topic => topic.Id == topicId).Name);
                }

                var whole = Taxonomy.First(subject => subject.Id == entry.SubjectId);
                return new NoticeSubjectMappingResponse(whole.Id, whole.Name, null, null);
            })];

        private static string? Blank(string? group) => string.IsNullOrWhiteSpace(group) ? null : group.Trim();

        private void InsertLastInGroup(NoticeSubjectResponse row)
        {
            var key = AppSuggestField.Normalize(row.Group);
            var last = NoticeSubjects.FindLastIndex(candidate => candidate.ExamEditionId == row.ExamEditionId
                && AppSuggestField.Normalize(candidate.Group) == key);
            NoticeSubjects.Insert(last < 0 ? NoticeSubjects.Count : last + 1, row);
        }

        // The Api joins the board's name and acronym; the fake looks the board up in the rows it knows.
        private ExamEditionResponse SavedEdition(Guid id, Guid examId, SaveExamEditionRequest request)
        {
            var organizer = Organizers?.Find(candidate => candidate.Id == request.OrganizerId) ?? Cebraspe;

            return new ExamEditionResponse(
                id,
                examId,
                organizer.Id,
                organizer.Name,
                organizer.Acronym,
                request.NoticeYear ?? 0,
                request.Position,
                request.NoticeReference,
                request.NoticeUrl,
                request.AppliedOn,
                request.ParseStatus() ?? ExamEditionStatus.Draft);
        }

        private static IssuingAuthorityResponse SavedAuthority(Guid id, SaveIssuingAuthorityRequest request) =>
            new(id, request.Name!, request.Description, request.Website);

        // The Api joins the issuing authority's name; the fake looks it up in the same sample rows.
        private static ExamResponse SavedExam(Guid id, SaveExamRequest request)
        {
            var authority = Sample.FirstOrDefault(body => body.Id == request.IssuingAuthorityId) ?? Guarulhos;

            return new ExamResponse(
                id,
                request.Name!,
                authority.Id,
                authority.Name,
                request.ParseAssessmentType() ?? AssessmentType.PublicServiceExam,
                request.ParseScope() ?? ExamScope.National,
                request.ScopeDetail,
                request.ContentLanguage ?? "pt-BR");
        }

        private static OrganizerResponse Saved(Guid id, SaveOrganizerRequest request) =>
            new(id, request.Name!, request.Acronym!.ToUpperInvariant(), request.ParseKind()!.Value, request.Description, request.Website);

        public static T Read<T>(string? body) => System.Text.Json.JsonSerializer.Deserialize<T>(body!, AppJson.Options)!;

        public static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = JsonContent.Create(value, options: AppJson.Options) };

        public static HttpResponseMessage Problem((HttpStatusCode Status, string Code) failure) =>
            new(failure.Status)
            {
                Content = JsonContent.Create(
                    new Dictionary<string, object> { ["status"] = (int)failure.Status, ["code"] = failure.Code },
                    options: AppJson.Options)
            };
    }
}
