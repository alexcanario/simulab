using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-9's back office pages, signed in as an Admin, with a fake Api behind the typed client: the test says
/// what the Api answers and reads what the page shows and what it sent.
/// </summary>
public abstract class AdminPageTestContext : KitTestContext
{
    private const string WebSessionId = "web-1";

    protected static readonly RoleResponse Student = new(Guid.Parse("0198f0a2-0000-7000-8000-000000000001"), IdentityRoles.Student, true, [], 1243);
    protected static readonly RoleResponse Curator = new(Guid.Parse("0198f0a2-0000-7000-8000-000000000002"), IdentityRoles.Curator, true, [], 2);
    protected static readonly RoleResponse Admin = new(Guid.Parse("0198f0a2-0000-7000-8000-000000000003"), IdentityRoles.Admin, true, [IdentityPermissions.RolesManage], 1);
    protected static readonly RoleResponse Reviewer = new(Guid.Parse("0198f0a2-0000-7000-8000-000000000004"), "Content reviewer", false, [], 2);
    protected static readonly RoleResponse Support = new(Guid.Parse("0198f0a2-0000-7000-8000-000000000005"), "Support", false, [IdentityPermissions.RolesManage], 0);

    protected FakeAdminApi Api { get; } = new();

    protected AdminPageTestContext()
    {
        var visitor = new VisitorContext();
        Services.AddSingleton(visitor);
        Services.AddSingleton(new IdentityApiClient(
            new HttpClient(Api) { BaseAddress = new Uri("https://api.test") },
            visitor,
            Options.Create(new OpenIddictClientOptions())));

        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [IdentityPermissions.RolesManage], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId), new Claim(WebAuthClaims.Permission, IdentityPermissions.RolesManage));
    }

    /// <summary>The providers a page's dialogs, tooltips and snackbars render into.</summary>
    protected (IRenderedComponent<MudDialogProvider> Dialogs, IRenderedComponent<MudSnackbarProvider> Snackbars) RenderProviders()
    {
        Render<MudPopoverProvider>();
        return (Render<MudDialogProvider>(), Render<MudSnackbarProvider>());
    }

    /// <summary>Answers the back office routes with what the test set, and records what it received.</summary>
    protected sealed class FakeAdminApi : HttpMessageHandler
    {
        public List<RoleResponse>? Roles { get; set; } = [Student, Curator, Admin, Reviewer, Support];

        public List<string> Permissions { get; set; } = [IdentityPermissions.RolesManage];

        public List<UserSummaryResponse> Users { get; set; } =
        [
            new(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a1"), "ana.souza@exemplo.com.br", "Ana Souza", "Active", [new(Admin.Id, Admin.Name, true)]),
            new(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a2"), "bruno.lima@exemplo.com.br", "Bruno Lima", "Active", [new(Curator.Id, Curator.Name, true), new(Reviewer.Id, Reviewer.Name, false)]),
            new(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a3"), "diego.alves@exemplo.com.br", null, "Pending", [])
        ];

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

            if (path.EndsWith("/permissions", StringComparison.Ordinal))
            {
                return Json(Permissions.Select(name => new PermissionResponse(name)).ToList());
            }

            if (path.EndsWith("/roles", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                return Roles is null ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Json(Roles);
            }

            if (path.EndsWith("/roles", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                var saved = Read<SaveRoleRequest>(body);
                return Json(new RoleResponse(Guid.NewGuid(), saved.Name!, false, saved.Permissions ?? [], 0), HttpStatusCode.Created);
            }

            if (path.Contains("/roles/", StringComparison.Ordinal) && request.Method == HttpMethod.Put && !path.StartsWith("/api/v1/identity/users", StringComparison.Ordinal))
            {
                var saved = Read<SaveRoleRequest>(body);
                return Json(new RoleResponse(Guid.NewGuid(), saved.Name!, false, saved.Permissions ?? [], 0));
            }

            if (path.Contains("/roles/", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path.EndsWith("/users", StringComparison.Ordinal))
            {
                return Json(new UserPageResponse(Users, Users.Count));
            }

            if (path.EndsWith("/roles", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            {
                var user = Users[1];
                var ids = Read<SetUserRolesRequest>(body).RoleIds ?? [];
                return Json(user with { Roles = [.. (Roles ?? []).Where(role => ids.Contains(role.Id)).Select(role => new UserRoleResponse(role.Id, role.Name, role.IsSystem))] });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

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
