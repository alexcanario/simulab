using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// The Api calls the web session makes (B-3): the token endpoint and <c>GET /api/v1/identity/session</c>;
/// and, for F-8, the profile calls the Web's own endpoints make. Each answer is what the test sets;
/// nothing reaches a real server.
/// </summary>
public sealed class FakeAuthApi : HttpMessageHandler
{
    private int _refreshCount;
    private int _sessionCount;
    private int _signOutCount;

    /// <summary>What a refresh answers: a new pair (the default), a rejection, or no answer at all.</summary>
    public RefreshAnswer Refresh { get; set; } = RefreshAnswer.NewPair;

    /// <summary>What the session check answers.</summary>
    public HttpStatusCode SessionStatus { get; set; } = HttpStatusCode.OK;

    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>When set, a refresh waits for it: lets a test hold two refreshes in flight at once.</summary>
    public TaskCompletionSource? RefreshGate { get; set; }

    public int RefreshCount => _refreshCount;

    public int SessionCount => _sessionCount;

    public int SignOutCount => _signOutCount;

    /// <summary>What the Api's sign-out answers.</summary>
    public HttpStatusCode SignOutStatus { get; set; } = HttpStatusCode.NoContent;

    /// <summary>F-8: the name and language the session answer carries, as the Api reads them from the account.</summary>
    public string? SessionFullName { get; set; }

    public string? SessionPreferredLanguage { get; set; }

    /// <summary>F-8: what GET /profile answers; null answers 500.</summary>
    public ProfileResponse? Profile { get; set; } = new("ana@example.com", "Ana", "en");

    /// <summary>F-8: what PUT /profile/preferred-language answers.</summary>
    public HttpStatusCode PreferredLanguageStatus { get; set; } = HttpStatusCode.NoContent;

    /// <summary>F-8: the languages PUT /profile/preferred-language received, in order.</summary>
    public List<string?> SavedLanguages { get; } = [];

    public HttpClient Client() => new(this) { BaseAddress = new Uri("https://api.test") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;

        if (path == "/connect/token")
        {
            var count = Interlocked.Increment(ref _refreshCount);
            if (RefreshGate is not null)
            {
                await RefreshGate.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            return Refresh switch
            {
                RefreshAnswer.InvalidClient => Json(new { error = "invalid_client" }, HttpStatusCode.BadRequest),
                RefreshAnswer.ServerError => new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"title\":\"An error occurred\",\"status\":500}", System.Text.Encoding.UTF8, "application/problem+json")
                },
                RefreshAnswer.TimedOut => throw new TaskCanceledException("The request timed out."),
                RefreshAnswer.NewPair => Json(new
                {
                    access_token = $"access-{count}",
                    refresh_token = $"refresh-{count}",
                    expires_in = 900
                }),
                RefreshAnswer.Rejected => Json(new { error = IdentityErrorCodes.RefreshTokenInvalid }, HttpStatusCode.BadRequest),
                _ => throw new HttpRequestException("The Api did not answer.")
            };
        }

        if (path == "/api/v1/identity/sign-out")
        {
            Interlocked.Increment(ref _signOutCount);
            return new HttpResponseMessage(SignOutStatus);
        }

        if (path == "/api/v1/identity/session")
        {
            Interlocked.Increment(ref _sessionCount);
            if (SessionStatus != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(SessionStatus);
            }

            var token = request.Headers.Authorization?.Parameter ?? string.Empty;
            return Json(
                new SessionInfoResponse(Guid.Empty.ToString(), "ana@example.com", $"jti-of-{token}", Permissions, SessionFullName, SessionPreferredLanguage),
                options: AppJson.Options);
        }

        if (path == "/api/v1/identity/profile" && request.Method == HttpMethod.Get)
        {
            return Profile is null ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Json(Profile, options: AppJson.Options);
        }

        if (path == "/api/v1/identity/profile/preferred-language")
        {
            var body = await request.Content!.ReadFromJsonAsync<UpdatePreferredLanguageRequest>(AppJson.Options, cancellationToken);
            SavedLanguages.Add(body!.PreferredLanguage);
            return new HttpResponseMessage(PreferredLanguageStatus);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json<T>(T body, HttpStatusCode status = HttpStatusCode.OK, System.Text.Json.JsonSerializerOptions? options = null) =>
        new(status) { Content = JsonContent.Create(body, options: options) };
}

