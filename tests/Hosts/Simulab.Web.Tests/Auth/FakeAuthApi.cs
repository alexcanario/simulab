using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// The two Api calls the web session makes (B-3): the token endpoint and <c>GET /api/v1/identity/session</c>.
/// Each answer is what the test sets; nothing reaches a real server.
/// </summary>
public sealed class FakeAuthApi : HttpMessageHandler
{
    private int _refreshCount;
    private int _sessionCount;

    /// <summary>What a refresh answers: a new pair (the default), a rejection, or no answer at all.</summary>
    public RefreshAnswer Refresh { get; set; } = RefreshAnswer.NewPair;

    /// <summary>What the session check answers.</summary>
    public HttpStatusCode SessionStatus { get; set; } = HttpStatusCode.OK;

    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>When set, a refresh waits for it: lets a test hold two refreshes in flight at once.</summary>
    public TaskCompletionSource? RefreshGate { get; set; }

    public int RefreshCount => _refreshCount;

    public int SessionCount => _sessionCount;

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

        if (path == "/api/v1/identity/session")
        {
            Interlocked.Increment(ref _sessionCount);
            if (SessionStatus != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(SessionStatus);
            }

            var token = request.Headers.Authorization?.Parameter ?? string.Empty;
            return Json(new SessionInfoResponse(Guid.Empty.ToString(), "ana@example.com", $"jti-of-{token}", Permissions), options: AppJson.Options);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json<T>(T body, HttpStatusCode status = HttpStatusCode.OK, System.Text.Json.JsonSerializerOptions? options = null) =>
        new(status) { Content = JsonContent.Create(body, options: options) };
}

public enum RefreshAnswer
{
    NewPair,
    Rejected,
    Unreachable
}
