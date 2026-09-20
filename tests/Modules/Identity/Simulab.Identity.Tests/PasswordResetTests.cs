using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Api;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Jobs;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-7 AC1-AC8, AC10, AC12 through the real HTTP pipeline. Ported from Simulae's <c>ForgotPasswordHandlerTests</c>,
/// <c>ForgotPasswordEndpointTests</c>, <c>ResetPasswordHandlerTests</c> and <c>ResetPasswordEndpointTests</c>.
/// </summary>
public sealed class PasswordResetTests : IdentityApiTests
{
    private const string RequestRoute = "/api/v1/identity/password-reset-requests";
    private const string CheckRoute = "/api/v1/identity/password-reset-token-checks";
    private const string ResetRoute = "/api/v1/identity/password-resets";
    private const string NewPassword = "Revisar#2026!x";

    private async Task<string> RequestLinkAsync(HttpClient client, string email)
    {
        await RunJobsAsync();
        Emails.Clear();
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        return ResetLink.TokenOf(Emails.Last!.HtmlBody);
    }

    // Simulae: Deve_gerar_token_e_enviar_email_para_conta_existente, Deve_invalidar_tokens_anteriores_ao_solicitar_novo.
    [Fact]
    public async Task RequestLink_ActiveAccount_StoresOneHashedTokenForAnHourAndInvalidatesTheOlderOne()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var first = await RequestLinkAsync(client, email);
        Factory.Clock.Advance(PasswordResetPolicy.Cooldown);

        var second = await RequestLinkAsync(client, email);

        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        var tokens = await QueryAsync(context => context.PasswordResetTokens.OrderBy(token => token.CreatedAt).ToListAsync());
        tokens.Should().HaveCount(2);
        tokens.Should().NotContain(token => token.TokenHash == first || token.TokenHash == second, "only the hash is stored");
        tokens[0].IsConsumed.Should().BeTrue();
        tokens[1].IsConsumed.Should().BeFalse();
        tokens[1].ExpiresAt.Should().Be(Factory.Clock.GetUtcNow().Add(PasswordResetPolicy.Lifetime));
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(first), HttpStatusCode.BadRequest);
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(second), HttpStatusCode.NoContent);
    }

    // Simulae: Deve_retornar_mensagem_generica_para_email_inexistente, Deve_retornar_sucesso_generico_quando_email_vazio.
    [Theory]
    [InlineData("nao.existe@exemplo.com")]
    [InlineData("")]
    public async Task RequestLink_UnknownOrEmptyAddress_SameAnswerAndNothingStoredOrSent(string email)
    {
        var client = Client();

        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);

        await RunJobsAsync();
        Emails.Count.Should().Be(0);
        (await QueryAsync(context => context.PasswordResetTokens.CountAsync())).Should().Be(0);
    }

    [Fact]
    public async Task RequestLink_PerAccountLimits_AreSilent()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await RequestLinkAsync(client, email);

        // Inside the cooldown: same 202, nothing sent.
        await RunJobsAsync();
        Emails.Clear();
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(0);

        // Five in the hour: the sixth is refused silently too. The tokens are written directly, one a
        // minute apart: through HTTP the per-client cap (also five) would answer first in a test host,
        // where every request comes from the same client.
        var userId = (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).Id;
        for (var i = 1; i < PasswordResetPolicy.MaxEmailsPerWindow; i++)
        {
            Factory.Clock.Advance(PasswordResetPolicy.Cooldown);
            await QueryAsync(context =>
            {
                context.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = userId,
                    TokenHash = $"seeded-{i}",
                    ExpiresAt = Factory.Clock.GetUtcNow().Add(PasswordResetPolicy.Lifetime)
                });
                return context.SaveChangesAsync();
            });
        }

        Factory.Clock.Advance(PasswordResetPolicy.Cooldown);
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(0);

        // An hour later the account may ask again.
        Factory.Clock.Advance(PasswordResetPolicy.Window);
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
    }

    [Fact]
    public async Task RequestLink_AboveTheHourlyCapForOneClient_IsRefused()
    {
        var client = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetRequestsPerHour; i++)
        {
            await PostAsync(client, RequestRoute, new RequestPasswordResetRequest($"qualquer{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        var body = await PostAsync(client, RequestRoute, new RequestPasswordResetRequest("mais.um@exemplo.com"), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.PasswordResetRateLimited);
    }

    [Fact]
    public async Task Reset_AboveTheHourlyCapForOneClient_IsRefused()
    {
        var client = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetsPerHour; i++)
        {
            await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest($"token-{i}"), HttpStatusCode.BadRequest);
        }

        var body = await PostAsync(client, ResetRoute, new ResetPasswordRequest("token-x", NewPassword), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.PasswordResetRateLimited);
    }

    // Simulae: Deve_redefinir_senha_com_token_valido_1h, Deve_rejeitar_token_ja_consumido.
    [Fact]
    public async Task Reset_ValidToken_NewPasswordSignsInOldDoesNotAndTheLinkWorksOnce()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await RequestLinkAsync(client, email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull();
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        var again = await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, "Outra#Senha2026"), HttpStatusCode.BadRequest);
        CodeOf(again).Should().Be(IdentityErrorCodes.PasswordResetInvalid);
    }

    // Simulae: Deve_rejeitar_token_expirado_com_mensagem_generica, Deve_rejeitar_token_inexistente_com_mensagem_generica, Deve_rejeitar_quando_token_vazio.
    [Fact]
    public async Task CheckAndReset_UnknownOrExpiredToken_AnswerWithTheirOwnCodes()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await RequestLinkAsync(client, email);

        var unknown = await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest("not-a-token"), HttpStatusCode.BadRequest);
        var empty = await PostAsync(client, ResetRoute, new ResetPasswordRequest("", NewPassword), HttpStatusCode.BadRequest);
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(token), HttpStatusCode.NoContent);

        Factory.Clock.Advance(PasswordResetPolicy.Lifetime);
        var expiredCheck = await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(token), HttpStatusCode.Gone);
        var expiredReset = await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.Gone);

        CodeOf(unknown).Should().Be(IdentityErrorCodes.PasswordResetInvalid);
        CodeOf(empty).Should().Be(IdentityErrorCodes.PasswordResetInvalid);
        CodeOf(expiredCheck).Should().Be(IdentityErrorCodes.PasswordResetExpired);
        CodeOf(expiredReset).Should().Be(IdentityErrorCodes.PasswordResetExpired);
    }

    // Simulae: Deve_rejeitar_senha_fora_da_politica.
    [Theory]
    [InlineData("curta1!A", IdentityErrorCodes.PasswordResetTooWeak, HttpStatusCode.BadRequest)]
    [InlineData(SignUpForm.ValidPassword, IdentityErrorCodes.PasswordResetSameAsCurrent, HttpStatusCode.UnprocessableEntity)]
    public async Task Reset_RefusedPassword_ChangesNothingAndKeepsTheLink(string password, string code, HttpStatusCode status)
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await RequestLinkAsync(client, email);

        var body = await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, password), status);

        CodeOf(body).Should().Be(code);
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest(token), HttpStatusCode.NoContent);
    }

    // Simulae: Deve_resetar_lockout_apos_redefinicao_bem_sucedida (which cleared the count but not the lockout).
    [Fact]
    public async Task Reset_LockedOutAccount_ClearsTheLockoutAndSignsInAtOnce()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, email, "not-the-password");
        }

        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.AccountLocked);
        var token = await RequestLinkAsync(client, email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull();
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Reset_PendingAccount_ActivatesItAndConsumesItsVerificationLinks()
    {
        var client = Client();
        var request = SignUpForm.Valid();
        await client.PostAsJsonAsync("/api/v1/identity/registrations", request, AppJson.Options);
        var token = await RequestLinkAsync(client, request.Email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == request.Email));
        user.Status.Should().Be(AccountStatus.Active);
        user.EmailConfirmed.Should().BeTrue();
        user.EmailVerifiedAt.Should().Be(Factory.Clock.GetUtcNow());
        (await QueryAsync(context => context.EmailVerificationTokens.AllAsync(t => t.ConsumedAt != null))).Should().BeTrue();
        (await TokenClient.SignInAsync(client, request.Email, NewPassword)).AccessToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Reset_EndsEverySessionOfTheAccount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 3);
        var token = await RequestLinkAsync(client, email);

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        foreach (var session in sessions)
        {
            (await SignedInSessions.AnswerOfAsync(client, session.AccessToken!)).Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));
            (await TokenClient.RefreshAsync(client, session.RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
        }
    }

    [Fact]
    public async Task Reset_SessionThatRefreshed_EndsTooAndItsOlderAccessTokenIsRevoked()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var first = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var refreshed = await TokenClient.RefreshAsync(client, first.RefreshToken!);

        // Rotation itself retires the older access token (review finding, F-7).
        (await SignedInSessions.AnswerOfAsync(client, first.AccessToken!)).Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));

        var token = await RequestLinkAsync(client, email);
        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        (await SignedInSessions.AnswerOfAsync(client, refreshed.AccessToken!)).Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));
        (await TokenClient.RefreshAsync(client, refreshed.RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Refresh_SessionFromBeforeThePasswordChanged_IsRefusedEvenIfRevokeAllMissedIt()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        var oldStamp = user.SecurityStamp;
        var sessionJti = await SignedInSessions.SessionJtiAsync(client, session.AccessToken!);
        var token = await RequestLinkAsync(client, email);
        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        // Plays the race the review found: the session is back in Redis with the stamp it began with.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<Simulab.Identity.Application.Sessions.IRefreshSessionStore>();
            await sessions.CreateAsync(sessionJti, user.Id, oldStamp, Factory.Clock.GetUtcNow().AddDays(1));
        }

        (await TokenClient.RefreshAsync(client, session.RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Reset_TwoConcurrentResetsWithOneLink_OnlyOneSucceeds()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await RequestLinkAsync(client, email);

        var answers = await Task.WhenAll(
            client.PostAsJsonAsync(ResetRoute, new ResetPasswordRequest(token, NewPassword), AppJson.Options),
            Client().PostAsJsonAsync(ResetRoute, new ResetPasswordRequest(token, "Outra#Senha2026x"), AppJson.Options));

        answers.Count(answer => answer.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        answers.Count(answer => answer.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
    }

    /// <summary>
    /// F-7 BR1 through F-13: the request no longer touches the mail server at all, so a broken SMTP
    /// server cannot change the answer of the real-account path. The failed send lands on the worker,
    /// where the job survives and is tried again (F-13 BR5).
    /// </summary>
    [Fact]
    public async Task RequestLink_MailServerFails_AnswersTheSameAndKeepsTheEmailForARetry()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await RunJobsAsync();
        Emails.Clear();
        Emails.FailNext = true;

        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);

        // The answer is already given; nothing was sent yet, and the job is still waiting.
        Emails.Count.Should().Be(0);
        (await PendingJobCountAsync()).Should().Be(1);

        // The worker tries and fails: the email is not lost, it is due again after the backoff.
        await RunJobsAsync();
        Emails.Count.Should().Be(0);
        (await PendingJobCountAsync()).Should().Be(1);

        Factory.Clock.Advance(JobPolicy.BackoffAfter(1));
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        Emails.Last!.To.Should().Be(email);
    }

    [Fact]
    public async Task Reset_SendsOnePasswordChangedEmailWithTheInstantAndNoToken()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await RequestLinkAsync(client, email);
        await RunJobsAsync();
        Emails.Clear();

        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);

        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        var message = Emails.Last!;
        message.Subject.Should().StartWith("Your password was changed");
        message.HtmlBody.Should().Contain("https://localhost/forgot-password").And.Contain("UTC").And.NotContain("token=");
    }

    [Fact]
    public async Task Endpoints_ForTheLostPassword_AreAnonymous()
    {
        var client = Client();

        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest("x@exemplo.com"), HttpStatusCode.Accepted);
        await PostAsync(client, CheckRoute, new PasswordResetTokenCheckRequest("x"), HttpStatusCode.BadRequest);
        await PostAsync(client, ResetRoute, new ResetPasswordRequest("x", NewPassword), HttpStatusCode.BadRequest);
    }
}
