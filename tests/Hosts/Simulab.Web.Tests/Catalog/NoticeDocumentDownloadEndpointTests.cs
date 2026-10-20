using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-89 UC3 and UC4 on the Web host: the browser downloads the notice PDF from the Web, which asks the Api with the
/// signed-in user's token and streams the answer on. The routes are checked by permission (BR6), carry no storage
/// address (BR8) and forward the Api's refusal.
/// </summary>
public sealed class NoticeDocumentDownloadEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly Guid ExamId = Guid.Parse("0198f0a3-0000-7000-8000-00000000000a");
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000f3");

    private static readonly byte[] Pdf = [.. "%PDF-1.7\nthe notice"u8.ToArray()];

    private readonly FakeAuthApi _auth = new();
    private readonly InMemoryWebSessionStore _store = new();
    private readonly FakeNoticeApi _api = new();

    private static string ManageRoute => $"/catalog/exams/{ExamId}/editions/{EditionId}/notice-document";

    private static string BrowseRoute => $"/catalog/published-exams/{ExamId}/editions/{EditionId}/notice-document";

    [Fact]
    public async Task Manage_StreamsTheApisBytesAsAnAttachment_WithTheSignedInUsersToken()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, CatalogPermissions.Manage);

        using var response = await client.GetAsync(ManageRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        (response.Content.Headers.ContentDisposition.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName)
            .Should().Be("Edital 2026.pdf");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(Pdf);
        _api.Requests.Should().ContainSingle().Which.Path.Should().Be($"/api/v1/catalog/exams/{ExamId}/editions/{EditionId}/notice-document");
        _api.Requests[0].Authorization.Should().Be("Bearer access-1");
    }

    [Fact]
    public async Task Browse_AsksTheStudentRouteOfTheApi()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, CatalogPermissions.Browse);

        using var response = await client.GetAsync(BrowseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _api.Requests.Should().ContainSingle().Which.Path.Should()
            .Be($"/api/v1/catalog/published-exams/{ExamId}/editions/{EditionId}/notice-document");
    }

    // BR6: the permission decides, by its name and not by a role; a student does not reach the back-office route.
    [Fact]
    public async Task Manage_WithoutThePermission_IsRefusedAndTheApiIsNotAsked()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, CatalogPermissions.Browse);

        using var response = await client.GetAsync(ManageRoute);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        _api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Browse_WithoutThePermission_IsRefusedAndTheApiIsNotAsked()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, CatalogPermissions.Manage);

        using var response = await client.GetAsync(BrowseRoute);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        _api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Download_NotSignedIn_GoesToSignIn()
    {
        await using var host = Host();
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        using var response = await client.GetAsync(BrowseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsolutePath.Should().Be("/sign-in");
        _api.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Download_TheApiRefuses_TheWebAnswersTheSameStatus(HttpStatusCode refusal)
    {
        _api.Refusal = refusal;
        await using var host = Host();
        var client = await SignedInAsync(host, CatalogPermissions.Browse);

        using var response = await client.GetAsync(BrowseRoute);

        response.StatusCode.Should().Be(refusal);
    }

    private WebApplicationFactory<Program> Host() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IWebSessionStore>(_store);
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _auth);
            services.AddHttpClient<CatalogApiClient>(client => client.BaseAddress = new Uri("https://api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => _api);
        }));

    private static async Task<HttpClient> SignedInAsync(WebApplicationFactory<Program> host, params string[] permissions)
    {
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var ticket = host.Services.GetRequiredService<SignInTicketStore>().Issue(new SignInTicket(
            Guid.NewGuid().ToString(), "ana@example.com", DisplayName: null, "jti-1", "access-1", "refresh-1",
            TimeSpan.FromMinutes(15), permissions));
        var response = await client.GetAsync($"/account/sign-in-complete?ticket={ticket}");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        return client;
    }

    public void Dispose()
    {
        _auth.Dispose();
        _api.Dispose();
    }

    /// <summary>The Api's notice document route: the bytes of a PDF, or the status the test set.</summary>
    private sealed class FakeNoticeApi : HttpMessageHandler
    {
        public List<(string Path, string? Authorization)> Requests { get; } = [];

        public HttpStatusCode? Refusal { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString()));
            if (Refusal is { } status)
            {
                return Task.FromResult(new HttpResponseMessage(status));
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileNameStar = "Edital 2026.pdf" };

            return Task.FromResult(response);
        }
    }
}
