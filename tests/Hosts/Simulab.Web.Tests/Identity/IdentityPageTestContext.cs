using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// The identity pages with a fake API behind the typed client: the test says what the API answers and
/// then reads what the page shows.
/// </summary>
public abstract class IdentityPageTestContext : KitTestContext
{
    /// <summary>B-4: the secret the Web proves its visitor addresses with.</summary>
    protected const string WebSecret = "web-client-secret";

    protected StubApiHandler Api { get; } = new();

    /// <summary>B-4: the visitor of this circuit; a test sets its address as <c>Routes</c> would.</summary>
    protected VisitorContext Visitor { get; } = new();

    protected IdentityPageTestContext()
    {
        Services.AddSingleton(Visitor);
        Services.AddSingleton(new IdentityApiClient(
            new HttpClient(Api) { BaseAddress = new Uri("https://api.test") },
            Visitor,
            Options.Create(new OpenIddictClientOptions { ClientSecret = WebSecret })));
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

        /// <summary>F-8: what GET /profile answers. Null means a server error.</summary>
        public ProfileResponse? Profile { get; set; } = new("ana@exemplo.com", "Ana", "en");

        /// <summary>F-8: what PUT /profile answers. Null means 204.</summary>
        public (HttpStatusCode Status, string Code)? UpdateProfileFailure { get; set; }

        /// <summary>F-8: the bodies PUT /profile received, in order.</summary>
        public List<UpdateProfileRequest> ProfileUpdates { get; } = [];

        /// <summary>F-10: what POST /account-erasures answers. Null means 204.</summary>
        public (HttpStatusCode Status, string Code)? EraseFailure { get; set; }

        /// <summary>F-10: when set, an erasure answers 423 with these seconds left.</summary>
        public int? EraseLockedSeconds { get; set; }

        /// <summary>F-10: the passwords POST /account-erasures received, in order.</summary>
        public List<string?> ErasureAttempts { get; } = [];

        /// <summary>F-11: what GET /totp answers. Null means 404, the answer while the feature is off.</summary>
        public TotpStatusResponse? TotpStatus { get; set; }

        /// <summary>F-11: when true, GET /totp answers 500 — a failure, not the switch.</summary>
        public bool TotpStatusFails { get; set; }

        /// <summary>F-11: what POST /totp/enrolments answers.</summary>
        public TotpEnrolmentResponse TotpEnrolment { get; set; } = new("JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP", "otpauth://totp/Simulab%3Aana", "data:image/png;base64,iVBORw0KGgo=");

        /// <summary>F-11: the recovery codes a confirmation or a regeneration answers.</summary>
        public IReadOnlyList<string> RecoveryCodes { get; set; } = [.. Enumerable.Range(0, 10).Select(i => $"AAAA{i}-BBBB{i}")];

        /// <summary>F-11: a failure every confirm, regenerate and disable call answers with. Null means success.</summary>
        public (HttpStatusCode Status, string Code)? TotpFailure { get; set; }

        /// <summary>F-11: when set, those calls answer 423 with these seconds left.</summary>
        public int? TotpLockedSeconds { get; set; }

        /// <summary>F-11: the bodies the confirm, regenerate and disable calls received, as raw JSON, in order.</summary>
        public List<string> TotpBodies { get; } = [];

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

            // F-10.
            if (path.EndsWith("/account-erasures", StringComparison.Ordinal))
            {
                ErasureAttempts.Add((await request.Content!.ReadFromJsonAsync<EraseAccountRequest>(AppJson.Options, cancellationToken))!.CurrentPassword);

                if (EraseLockedSeconds is { } eraseSeconds)
                {
                    return new HttpResponseMessage(HttpStatusCode.Locked)
                    {
                        Content = JsonContent.Create(
                            new Dictionary<string, object> { ["status"] = 423, ["code"] = IdentityErrorCodes.AccountLocked, ["retryAfterSeconds"] = eraseSeconds },
                            options: AppJson.Options)
                    };
                }

                return EraseFailure is { } eraseFailure ? Problem(eraseFailure) : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            // F-11.
            if (path.EndsWith("/totp", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                if (TotpStatusFails)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                return TotpStatus is null ? new HttpResponseMessage(HttpStatusCode.NotFound) : Json(TotpStatus);
            }

            if (path.EndsWith("/totp/enrolments", StringComparison.Ordinal))
            {
                return Json(TotpEnrolment);
            }

            if (path.EndsWith("/totp/enrolments/confirmations", StringComparison.Ordinal)
                || path.EndsWith("/totp/recovery-codes", StringComparison.Ordinal)
                || (path.EndsWith("/totp", StringComparison.Ordinal) && request.Method == HttpMethod.Delete))
            {
                TotpBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));

                if (TotpLockedSeconds is { } totpSeconds)
                {
                    return new HttpResponseMessage(HttpStatusCode.Locked)
                    {
                        Content = JsonContent.Create(
                            new Dictionary<string, object> { ["status"] = 423, ["code"] = IdentityErrorCodes.AccountLocked, ["retryAfterSeconds"] = totpSeconds },
                            options: AppJson.Options)
                    };
                }

                if (TotpFailure is { } totpFailure)
                {
                    return Problem(totpFailure);
                }

                return request.Method == HttpMethod.Delete
                    ? new HttpResponseMessage(HttpStatusCode.NoContent)
                    : Json(new RecoveryCodesResponse(RecoveryCodes));
            }

            // F-8.
            if (path.EndsWith("/profile", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                return Profile is null
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Profile, options: AppJson.Options) };
            }

            if (path.EndsWith("/profile", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            {
                ProfileUpdates.Add((await request.Content!.ReadFromJsonAsync<UpdateProfileRequest>(AppJson.Options, cancellationToken))!);
                return UpdateProfileFailure is { } failure ? Problem(failure) : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

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
