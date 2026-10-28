using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Simulab.ExamEngine.Contracts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.ExamEngine;

/// <summary>
/// F-169 AC12 and AC15 (Web half) on the Web host: the student's browser shows a picture of the attempt from the Web, which
/// asks the Api with the signed-in user's token and streams the answer on with the headers of F-103 (BR12).
/// </summary>
public sealed class AttemptImageEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly Guid AttemptId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c1");
    private static readonly Guid ImageId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c2");

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 5, 6, 7, 8];

    private readonly FakeAuthApi _auth = new();
    private readonly InMemoryWebSessionStore _store = new();
    private readonly FakeImageApi _api = new();

    private static string Route => $"/attempts/{AttemptId}/images/{ImageId}";

    // AC12, AC15: a holder of attempts.take gets the Api's bytes with the stored type and the hardening headers, and the
    // Api is asked for that picture of that attempt with the user's token.
    [Fact]
    public async Task Show_ForAHolderOfTake_StreamsTheApisBytes_WithTheStoredTypeAndTheHardeningHeaders()
    {
        await using var host = Host();
        var client = await SignedInAsync(host, ExamEnginePermissions.Take);

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("Content-Security-Policy").Should().Equal("sandbox");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        _api.Requests.Should().ContainSingle().Which.Path.Should().Be($"/api/v1/exam-engine/attempts/{AttemptId}/images/{ImageId}");
        _api.Requests[0].Authorization.Should().Be("Bearer access-1");
    }

    // AC15: the Api's 404 (another student's attempt, a picture outside it) reaches the browser unchanged.
    [Fact]
    public async Task Show_TheApiAnswers404_TheWebAnswers404()
    {
        _api.Refusal = HttpStatusCode.NotFound;
        await using var host = Host();
        var client = await SignedInAsync(host, ExamEnginePermissions.Take);

        using var response = await client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _api.Requests.Should().ContainSingle();
    }

    // AC15: an anonymous caller is sent to sign in, and a user without attempts.take is refused; the Api is not asked.
    [Fact]
    public async Task Show_NotSignedIn_GoesToSignIn_AndWithoutTake_IsRefused_AndTheApiIsNotAsked()
    {
        await using var host = Host();
        var anonymous = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var curator = await SignedInAsync(host, "catalog.browse");

        using var signIn = await anonymous.GetAsync(Route);
        using var refused = await curator.GetAsync(Route);

        signIn.StatusCode.Should().Be(HttpStatusCode.Redirect);
        signIn.Headers.Location!.AbsolutePath.Should().Be("/sign-in");
        refused.StatusCode.Should().NotBe(HttpStatusCode.OK);
        _api.Requests.Should().BeEmpty();
    }

    private WebApplicationFactory<Program> Host() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IWebSessionStore>(_store);
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _auth);
            services.AddHttpClient<ExamEngineApiClient>(client => client.BaseAddress = new Uri("https://api.test"))
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

    /// <summary>The Api's picture route of an attempt: the bytes of a PNG, or the status the test set.</summary>
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
