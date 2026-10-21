using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-103 AC5 (Web half), AC6 and UC3 on the Web host: the browser shows a stored picture from the Web, which asks the Api
/// with the signed-in user's token and streams the answer on. The route is checked by permission (BR12), carries no storage
/// address and forwards the Api's refusal.
/// </summary>
public sealed class QuestionImageEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly Guid ImageId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000a1");

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly FakeAuthApi _auth = new();
    private readonly InMemoryWebSessionStore _store = new();
    private readonly FakeImageApi _api = new();

    private static string Route => $"/question-images/{ImageId}";

    // AC5, BR12: questions.manage reads, and the Api is asked with the signed-in user's token.
    [Fact]
    public async Task Show_ForAHolderOfManage_StreamsTheApisBytes_WithTheUsersToken()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, QuestionBankPermissions.Manage);

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
        _api.Requests.Should().ContainSingle().Which.Path.Should().Be($"/api/v1/question-bank/images/{ImageId}");
        _api.Requests[0].Authorization.Should().Be("Bearer access-1");
    }

    // AC5, BR12: questions.review alone reads too.
    [Fact]
    public async Task Show_ForAHolderOfReviewOnly_StreamsTheApisBytes()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, QuestionBankPermissions.Review);

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _api.Requests.Should().ContainSingle();
    }

    // AC5, BR12: any other permission is refused here and the Api is not even asked.
    [Fact]
    public async Task Show_WithoutEitherPermission_IsRefusedAndTheApiIsNotAsked()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, "catalog.browse");

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        _api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Show_NotSignedIn_GoesToSignIn()
    {
        await using var host = Host();
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsolutePath.Should().Be("/sign-in");
        _api.Requests.Should().BeEmpty();
    }

    // AC6, BR13: the type the Api stored, nosniff, sandbox and private; shown inline, with no storage address.
    [Fact]
    public async Task Show_AnswersTheStoredTypeAndTheHardeningHeaders_AndNoStorageAddress()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, QuestionBankPermissions.Manage);

        using var response = await client.GetAsync(Route);

        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("Content-Security-Policy").Should().Equal("sandbox");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeFalse();
        response.Content.Headers.ContentDisposition.Should().BeNull();
        response.Headers.Location.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Show_TheApiRefuses_TheWebAnswersTheSameStatus(HttpStatusCode refusal)
    {
        _api.Refusal = refusal;
        await using var host = Host();
        var client = await SignedInAsync(host, QuestionBankPermissions.Manage);

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().Be(refusal);
    }

    private WebApplicationFactory<Program> Host() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IWebSessionStore>(_store);
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _auth);
            services.AddHttpClient<QuestionBankApiClient>(client => client.BaseAddress = new Uri("https://api.test"))
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

    /// <summary>The Api's image route: the bytes of a PNG, or the status the test set.</summary>
    private sealed class FakeImageApi : HttpMessageHandler
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

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            return Task.FromResult(response);
        }
    }
}
