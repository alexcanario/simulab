using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// The identity pages with a fake API behind the typed client: the test says what the API answers and
/// then reads what the page shows.
/// </summary>
public abstract class IdentityPageTestContext : KitTestContext
{
    protected StubApiHandler Api { get; } = new();

    protected IdentityPageTestContext()
    {
        Services.AddSingleton(new IdentityApiClient(new HttpClient(Api) { BaseAddress = new Uri("https://api.test") }));
        Services.AddScoped<SignUpFlow>();
        Services.AddSingleton(TimeProvider.System);

        // Both documents are there unless a test says otherwise.
        Api.Terms = Document(LegalTopic.Terms);
        Api.Privacy = Document(LegalTopic.Privacy);
    }

    /// <summary>A legal document as the API returns it.</summary>
    protected static LegalDocumentResponse Document(LegalTopic topic, string version = "2026-v1", bool placeholder = false) =>
        new(topic, "en", version, new DateOnly(2026, 9, 17), topic == LegalTopic.Terms ? "Terms of use" : "Privacy policy",
            "<h1>Title</h1>", placeholder);

    /// <summary>Answers each route with what the test set, so no page ever reaches a real server.</summary>
    protected sealed class StubApiHandler : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _requests = [];

        public LegalDocumentResponse? Terms { get; set; }

        public LegalDocumentResponse? Privacy { get; set; }

        /// <summary>What POST /registrations answers. Null means 202.</summary>
        public (HttpStatusCode Status, string Code)? RegisterFailure { get; set; }

        public (HttpStatusCode Status, string Code)? VerifyFailure { get; set; }

        public VerificationOutcome VerifyOutcome { get; set; } = VerificationOutcome.Verified;

        public (HttpStatusCode Status, string Code)? RequestResetFailure { get; set; }

        public (HttpStatusCode Status, string Code)? CheckFailure { get; set; }

        public (HttpStatusCode Status, string Code)? ResetFailure { get; set; }

        public (HttpStatusCode Status, string Code)? ChangeFailure { get; set; }

        /// <summary>When set, a password change answers 423 with these seconds left.</summary>
        public int? ChangeLockedSeconds { get; set; }

        public IReadOnlyList<HttpRequestMessage> Requests => _requests;

        public int CountOf(string route) => _requests.Count(request => request.RequestUri!.AbsolutePath.EndsWith(route, StringComparison.Ordinal));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/legal-documents/terms", StringComparison.Ordinal))
            {
                return Json(Terms);
            }

            if (path.EndsWith("/legal-documents/privacy", StringComparison.Ordinal))
            {
                return Json(Privacy);
            }

            if (path.EndsWith("/registrations", StringComparison.Ordinal))
            {
                return RegisterFailure is { } failure ? Problem(failure) : new HttpResponseMessage(HttpStatusCode.Accepted);
            }

            if (path.EndsWith("/email-verifications", StringComparison.Ordinal))
            {
                return VerifyFailure is { } verifyFailure
                    ? Problem(verifyFailure)
                    : Json(new VerifyEmailResponse(VerifyOutcome));
            }

            if (path.EndsWith("/resend", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }

            // F-7.
            if (path.EndsWith("/password-reset-requests", StringComparison.Ordinal))
            {
                return RequestResetFailure is { } failure ? Problem(failure) : new HttpResponseMessage(HttpStatusCode.Accepted);
            }

            if (path.EndsWith("/password-reset-token-checks", StringComparison.Ordinal))
            {
                return CheckFailure is { } failure ? Problem(failure) : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path.EndsWith("/password-resets", StringComparison.Ordinal))
            {
                return ResetFailure is { } failure ? Problem(failure) : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path.EndsWith("/password-changes", StringComparison.Ordinal))
            {
                if (ChangeLockedSeconds is { } seconds)
                {
                    return new HttpResponseMessage(HttpStatusCode.Locked)
                    {
                        Content = JsonContent.Create(
                            new Dictionary<string, object> { ["status"] = 423, ["code"] = IdentityErrorCodes.AccountLocked, ["retryAfterSeconds"] = seconds },
                            options: AppJson.Options)
                    };
                }

                return ChangeFailure is { } failure ? Problem(failure) : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json<T>(T? value) => value is null
            ? Problem((HttpStatusCode.NotFound, IdentityErrorCodes.LegalDocumentNotFound))
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: AppJson.Options) };

        private static HttpResponseMessage Problem((HttpStatusCode Status, string Code) failure) =>
            new(failure.Status)
            {
                Content = JsonContent.Create(
                    new Dictionary<string, object> { ["status"] = (int)failure.Status, ["code"] = failure.Code },
                    options: AppJson.Options)
            };
    }
}
