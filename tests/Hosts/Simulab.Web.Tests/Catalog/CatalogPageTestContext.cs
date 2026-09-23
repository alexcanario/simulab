using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
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

        /// <summary>When set, every write answers this problem.</summary>
        public (HttpStatusCode Status, string Code)? WriteFailure { get; set; }

        public List<(HttpMethod Method, string Path, string? Query, string? Body)> Received { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Received.Add((request.Method, path, request.RequestUri.Query, body));

            if (request.Method != HttpMethod.Get && WriteFailure is { } failure)
            {
                return Problem(failure);
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

        private static OrganizerResponse Saved(Guid id, SaveOrganizerRequest request) =>
            new(id, request.Name!, request.Acronym!.ToUpperInvariant(), request.ParseKind()!.Value, request.Description, request.Website);

        public static T Read<T>(string? body) => System.Text.Json.JsonSerializer.Deserialize<T>(body!, AppJson.Options)!;

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
}
