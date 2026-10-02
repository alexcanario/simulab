using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-36's two student screens, signed in as a Student holding <c>catalog.browse</c> only, with a fake Api behind the
/// typed client: the test says what the Api answers and reads what the page shows and what it asked for.
/// </summary>
public abstract class StudentCatalogTestContext : KitTestContext
{
    private const string WebSessionId = "web-student";

    protected static readonly PublishedExamOrganizerResponse Fgv =
        new(Guid.Parse("0198f0a3-1111-7000-8000-000000000001"), "Fundacao Getulio Vargas", "FGV");

    protected static readonly PublishedExamOrganizerResponse Consulplan =
        new(Guid.Parse("0198f0a3-1111-7000-8000-000000000002"), "Instituto Consulplan", "CONSULPLAN");

    protected static readonly PublishedExamResponse GuardaMunicipal = new(
        Guid.Parse("0198f0a3-2222-7000-8000-00000000000a"),
        "Guarda Municipal",
        "Prefeitura de Sao Paulo",
        AssessmentType.PublicServiceExam,
        ExamScope.State,
        "SP",
        "pt-BR",
        2,
        2025);

    protected static readonly PublishedExamResponse Toefl = new(
        Guid.Parse("0198f0a3-2222-7000-8000-00000000000b"),
        "TOEFL iBT",
        "Educational Testing Service",
        AssessmentType.Certification,
        ExamScope.National,
        null,
        "en",
        1,
        2024);

    protected static readonly PublishedExamEditionResponse Edition2025 = new(
        Guid.Parse("0198f0a3-3333-7000-8000-000000000001"),
        2025,
        "Guarda Municipal de 3a Classe",
        Consulplan.Name,
        Consulplan.Acronym,
        "Edital 01/2025",
        "https://www.consulplan.net/edital-2025",
        new DateOnly(2025, 3, 16));

    protected static readonly PublishedExamEditionResponse Edition2023 = new(
        Guid.Parse("0198f0a3-3333-7000-8000-000000000002"),
        2023,
        null,
        Fgv.Name,
        Fgv.Acronym,
        null,
        null,
        null);

    protected FakeStudentCatalogApi Api { get; } = new();

    protected StudentCatalogTestContext()
    {
        Services.AddSingleton(new CatalogApiClient(new HttpClient(Api) { BaseAddress = new Uri("https://api.test") }));

        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [CatalogPermissions.Browse], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId), new Claim(WebAuthClaims.Permission, CatalogPermissions.Browse));

        // Tooltips and selects open into the popover provider.
        Render<MudPopoverProvider>();
    }

    /// <summary>The address the page is opened at: the router would have set it before the page rendered.</summary>
    protected void OpenAt(string relativeUri) =>
        Services.GetRequiredService<NavigationManager>().NavigateTo(relativeUri);

    protected NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    /// <summary>The published exams a test builds to have more than one page: "Guarda 01" and on, all State.</summary>
    protected static List<PublishedExamResponse> Many(int count) =>
        [.. Enumerable.Range(1, count).Select(number => GuardaMunicipal with
        {
            Id = Guid.Parse($"0198f0a3-4444-7000-8000-{number:D12}"),
            Name = $"Guarda {number:D2}"
        })];

    /// <summary>Answers the three student routes with what the test set, and records what it received.</summary>
    protected sealed class FakeStudentCatalogApi : HttpMessageHandler
    {
        /// <summary>What the list holds; null makes it answer a server error.</summary>
        public List<PublishedExamResponse>? Exams { get; set; } = [GuardaMunicipal, Toefl];

        /// <summary>What the filter options hold; null makes them answer a server error.</summary>
        public PublishedExamFiltersResponse? Filters { get; set; } = new([Consulplan, Fgv], [2026, 2025]);

        /// <summary>The exam pages the fake knows; an id that is not here answers 404 <c>exam.not_found</c>.</summary>
        public Dictionary<Guid, PublishedExamDetailResponse> Details { get; } = [];

        /// <summary>When set, reading one exam answers a server error (the page's failure state).</summary>
        public bool FindFails { get; set; }

        /// <summary>When set, the filter options wait for it: the "options loading" state.</summary>
        public TaskCompletionSource? HoldFilters { get; set; }

        /// <summary>When set, reading one exam waits for it: the page's loading state.</summary>
        public TaskCompletionSource? HoldFind { get; set; }

        public List<(HttpMethod Method, string Path, string Query)> Received { get; } = [];

        public IEnumerable<string> ListQueries =>
            Received.Where(call => call.Path == "/api/v1/catalog/published-exams").Select(call => call.Query);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Received.Add((request.Method, path, request.RequestUri.Query));

            if (path == "/api/v1/catalog/published-exam-filters")
            {
                if (HoldFilters is { } hold)
                {
                    await hold.Task;
                }

                return Filters is null ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Json(Filters);
            }

            if (path == "/api/v1/catalog/published-exams")
            {
                return Exams is null ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Json(Page(request.RequestUri.Query));
            }

            var detail = Regex.Match(path, "^/api/v1/catalog/published-exams/(?<id>[^/]+)$");
            if (detail.Success)
            {
                if (HoldFind is { } holdFind)
                {
                    await holdFind.Task;
                }

                if (FindFails)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                return Details.TryGetValue(Guid.Parse(detail.Groups["id"].Value), out var exam)
                    ? Json(exam)
                    : Problem(HttpStatusCode.NotFound, CatalogErrorCodes.ExamNotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        // Paged like the Api, and filtered by the text, the type and the scope: enough to see a filter change the rows.
        // The board and year rules (BR5) are the Api's and are tested there.
        private PublishedExamPageResponse Page(string queryString)
        {
            var query = System.Web.HttpUtility.ParseQueryString(queryString);
            var size = int.Parse(query["pageSize"] ?? "25", System.Globalization.CultureInfo.InvariantCulture);
            var number = int.Parse(query["page"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);

            IEnumerable<PublishedExamResponse> matching = Exams!;
            if (query["search"] is { Length: > 0 } search)
            {
                matching = matching.Where(exam => exam.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (Enum.TryParse<AssessmentType>(query["assessmentType"], out var type))
            {
                matching = matching.Where(exam => exam.AssessmentType == type);
            }

            if (Enum.TryParse<ExamScope>(query["scope"], out var scope))
            {
                matching = matching.Where(exam => exam.Scope == scope);
            }

            var all = matching.ToList();
            return new PublishedExamPageResponse([.. all.Skip(number * size).Take(size)], all.Count);
        }

        private static HttpResponseMessage Json<T>(T value) =>
            new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: AppJson.Options) };

        private static HttpResponseMessage Problem(HttpStatusCode status, string code) =>
            new(status)
            {
                Content = JsonContent.Create(
                    new Dictionary<string, object> { ["status"] = (int)status, ["code"] = code },
                    options: AppJson.Options)
            };
    }
}
